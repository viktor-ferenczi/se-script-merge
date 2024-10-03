using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace IngameScriptMerge;

public class WhitespaceRemover() : CSharpSyntaxRewriter(true)
{
    private bool requiresWhitespace;

    public SyntaxNode Process(SyntaxNode node)
    {
        requiresWhitespace = false;

        return Visit(node);
    }

    public override SyntaxNode Visit(SyntaxNode node)
    {
        if (node.IsExcludedNamespace())
        {
            return node;
        }

        return base.Visit(node);
    }

    public override SyntaxToken VisitToken(SyntaxToken token)
    {
        var visited = base.VisitToken(token).WithoutTrivia();

        visited = CopyIfElseEndDirectives(token, visited);

        var fullString = visited.ToFullString();
        if (fullString.Length == 0)
        {
            return visited;
        }

        if (requiresWhitespace && fullString[0].RequiresWhitespace())
        {
            visited = visited.WithLeadingTrivia(visited.LeadingTrivia.Insert(0, SyntaxFactory.Whitespace(" ")));
        }

        requiresWhitespace = fullString[fullString.Length - 1].RequiresWhitespace();

        return visited;
    }

    private static SyntaxToken CopyIfElseEndDirectives(SyntaxToken token, SyntaxToken visited)
    {
        foreach (var trivia in token.LeadingTrivia)
        {
            switch (trivia.Kind())
            {
                case SyntaxKind.IfDirectiveTrivia:
                case SyntaxKind.ElseDirectiveTrivia:
                case SyntaxKind.EndIfDirectiveTrivia:
                    visited = visited.WithLeadingTrivia(visited.LeadingTrivia.Add(trivia));
                    break;
            }
        }

        if (visited.HasLeadingTrivia)
        {
            visited = visited.WithLeadingTrivia(visited.LeadingTrivia.Insert(0, SyntaxFactory.EndOfLine("\n")));
        }

        foreach (var trivia in token.TrailingTrivia)
        {
            switch (trivia.Kind())
            {
                case SyntaxKind.IfDirectiveTrivia:
                case SyntaxKind.ElseDirectiveTrivia:
                case SyntaxKind.EndIfDirectiveTrivia:
                    visited = visited.WithTrailingTrivia(visited.TrailingTrivia.Add(trivia));
                    break;
            }
        }

        return visited;
    }

    public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
    {
        if (
            trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
            trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
            trivia.IsKind(SyntaxKind.DocumentationCommentExteriorTrivia) ||
            trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
            trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
        {
            return SyntaxFactory.Whitespace(" ");
        }

        return base.VisitTrivia(trivia);
    }
}