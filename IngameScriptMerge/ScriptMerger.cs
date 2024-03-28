using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace IngameScriptMerge;

public class ScriptMerger(string solutionPath, string @namespace, bool minifyWhitespace, bool shortenNames, bool aggressiveCompression, bool releaseMode = false)
{
    private readonly string[] namespaceNames = @namespace.Split(',');

    public async Task<string> Merge()
    {
        var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(solutionPath);

        var nodes = solution.Projects
            .SelectMany(project => project.Documents)
            .Select(document => document.GetSyntaxTreeAsync().Result)
            .Where(tree => tree != null)
            .SelectMany(tree => tree
                .GetRootAsync().Result
                .DescendantNodes()
                .Where(node =>
                    node.IsKind(SyntaxKind.NamespaceDeclaration) &&
                    node.ChildNodes().Any(child =>
                        child.IsKind(SyntaxKind.IdentifierName) &&
                        namespaceNames.Contains(child.TryGetInferredMemberName()))))
            .SelectMany(ns => ns.ChildNodes().Skip(1))
            .ToList();

        var compiledScript = new CompiledScript(nodes);
        var root = await compiledScript.SyntaxTree.GetRootAsync();
        root.DebugDump("original");

        if (shortenNames)
        {
            root = new CodeCompressor(compiledScript, aggressiveCompression).Visit(root);
            root.DebugDump("shortened");
        }

        nodes = root
            .ChildNodes()
            .SkipWhile(child => child.IsKind(SyntaxKind.UsingDirective))
            .ToList();

        nodes = nodes.SelectMany(new ProgramClassRemover().Process).ToList();
        root.DebugDump("unpacked");

        if (minifyWhitespace)
        {
            nodes = nodes.Select(new WhitespaceRemover().Process).ToList();
            root.DebugDump("minified");
        }

        var scriptLines = new List<string>();
        foreach (var node in nodes.Where(node => node != null))
        {
            var iterLines = node
                .ToFullString()
                .Replace("\r\n", "\n")
                .IterSplit('\n')
                .FilterLines(releaseMode);

            scriptLines.AddRange(
                minifyWhitespace
                    ? iterLines.TrimAndRemoveEmptyLines()
                    : iterLines.ToList().RemoveCommonIndentation());
        }

        var script = string.Join("\n", scriptLines);
        return script;
    }
}