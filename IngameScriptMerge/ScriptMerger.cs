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

        var documents = solution.Projects
            .SelectMany(project => project.Documents)
            .ToList();

        documents.Sort((a, b) => String.Compare(a.FilePath, b.FilePath, StringComparison.Ordinal));

        var nodes = documents
            .AsParallel()
            .AsOrdered()
#if DEBUG
            .WithDegreeOfParallelism(1)
#endif
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
            .ToList();

        nodes.Sort(CompareNamespaceDeclarations);

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
            .AsParallel()
            .AsOrdered()
#if DEBUG
            .WithDegreeOfParallelism(1)
#endif
            .Select(ns => minifyWhitespace ? new WhitespaceRemover().Process(ns) : ns)
            .SelectMany(ns => ns.ChildNodes().Skip(1))
            .SkipWhile(child => child.IsKind(SyntaxKind.UsingDirective))
            .ToList();
        nodes.DebugDump("minified");

        nodes = nodes
            .SelectMany(new ProgramClassRemover().Process)
            .Where(node => node != null)
            .ToList();
        root.DebugDump("unpacked");

        var script = string.Join("\n", nodes
            .AsParallel()
            .AsOrdered()
#if DEBUG
            .WithDegreeOfParallelism(1)
#endif
            .Select(node => PostprocessCodeBlock(node.ToFullString())));

        return script;
    }

    private static string PostprocessCodeBlock(string text)
    {
        return string.Join("\n", text
            .Replace("\r\n", "\n")
            .IterSplit('\n')
            .Where(line => line.Trim() != "//!!")
            .ToList()
            .RemoveCommonIndentation());
    }

    private static int CompareNamespaceDeclarations(SyntaxNode a, SyntaxNode b)
    {
        if (!a.IsKind(SyntaxKind.NamespaceDeclaration) ||
            !b.IsKind(SyntaxKind.NamespaceDeclaration))
        {
            return 0;
        }

        var ae = a.IsExcludedNamespace();
        var be = b.IsExcludedNamespace();
        if (ae != be)
        {
            return ae ? -1 : 1;
        }

        return 0;
    }
}