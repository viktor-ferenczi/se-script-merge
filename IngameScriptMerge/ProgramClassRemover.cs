using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace IngameScriptMerge;

public class ProgramClassRemover : CSharpSyntaxRewriter
{
    private readonly List<SyntaxNode> programNodes = [];

    public IEnumerable<SyntaxNode> Process(SyntaxNode node)
    {
        yield return Visit(node);

        foreach (var programNode in programNodes)
        {
            yield return programNode;
        }

        programNodes.Clear();
    }

    public override SyntaxNode Visit(SyntaxNode node)
    {
        var visited = base.Visit(node);
        if (visited == null)
        {
            return null;
        }

        if (visited.IsProgramClassDeclaration())
        {
            programNodes.AddRange(visited.ChildNodes().Where(child => !child.IsKind(SyntaxKind.BaseList)));
            return default;
        }

        return visited;
    }
}