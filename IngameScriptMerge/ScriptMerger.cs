using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace IngameScriptMerge;

public class MergedScript(string text, List<string> errors)
{
    public readonly string Text = text;
    public readonly List<string> Errors = errors;
}

public class ScriptMerger(string solutionPath, string @namespace, bool minifyWhitespace = false, bool shortenNames = false, bool aggressiveCompression = false, bool releaseMode = false, string whitelistPath = null, string terminalPath = null)
{
    private readonly string[] namespaceNames = @namespace.Split(',');

    public async Task<MergedScript> Merge()
    {
        var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(solutionPath);

        // Sort documents by path for repeatable execution
        var documents = solution.Projects
            .SelectMany(project => project.Documents)
            .ToList();
        documents.Sort((a, b) => String.Compare(a.FilePath, b.FilePath, StringComparison.Ordinal));

        // Relevant namespaces from the original code (keep document order)
        var namespaces = documents
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

        // Move any namespaces excluded from minification to the top (header, configuration)
        namespaces.Sort(CompareNamespaceDeclarations);
        namespaces.DebugDump("original");

        // Compile the script with standard using statements
        var compiledScript = new CompiledScript(namespaces);
        var root = await compiledScript.SyntaxTree.GetRootAsync();
        root.DebugDump("compiled");

        if (whitelistPath != null)
        {
            var whitelistValidator = new WhitelistValidator(compiledScript, whitelistPath);
            whitelistValidator.Visit(root);
            if (whitelistValidator.Errors.Any())
            {
                return new MergedScript(null, whitelistValidator.Errors);
            }
        }

        if (terminalPath != null)
        {
            var terminalValidator = new TerminalValidator(compiledScript, terminalPath);
            terminalValidator.Visit(root);
            if (terminalValidator.Errors.Any())
            {
                return new MergedScript(null, terminalValidator.Errors);
            }
        }

        // Shorten type and variable names in the script based on type information
        if (shortenNames)
        {
            root = new CodeCompressor(compiledScript, aggressiveCompression).Visit(root);
            root.DebugDump("shortened");
        }

        // Collect the namespaces containing the rewritten code (do not unpack them here)
        namespaces = root
            .ChildNodes()
            .SkipWhile(child => child.IsKind(SyntaxKind.UsingDirective))
            .ToList();
        root.DebugDump("non-minified");

        // Remove unnecessary whitespace (keep order)
        if (minifyWhitespace)
        {
            namespaces = namespaces
                .AsParallel()
                .AsOrdered()
#if DEBUG
                .WithDegreeOfParallelism(1)
#endif
                // Must use separate WhitespaceRemover instances due to parallelism
                .Select(ns => new WhitespaceRemover().Process(ns))
                .ToList();
            namespaces.DebugDump("minified");
        }

        // Unpack code from namespaces, remove the Program class, post-process code, join all code
        var script = string.Join("\n", namespaces
            .AsParallel()
            .AsOrdered()
#if DEBUG
            .WithDegreeOfParallelism(1)
#endif
            .SelectMany(ns => ns
                // Skip the IdentifierName of the namespace, process the nodes inside the namespace
                .ChildNodes().Skip(1)
                // Must use separate ProgramClassRemover instances due to parallelism
                .SelectMany(new ProgramClassRemover().Process)
                .Where(node => node != null)
                .Select(node => PostprocessCodeBlock(node.ToFullString())))
        );

        return new MergedScript(script, null);
    }

    private string PostprocessCodeBlock(string text)
    {
        return string.Join("\n", text
            .Replace("\r\n", "\n")
            .IterSplit('\n')
            .Where(line => line.Trim() != "//!!")
            .FilterLines(releaseMode)
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