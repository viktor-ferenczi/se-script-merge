﻿using Microsoft.CodeAnalysis;
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

    public override SyntaxToken VisitToken(SyntaxToken token)
    {
        var visited = base.VisitToken(token).WithoutTrivia();

        var fullString = visited.ToFullString();
        if (fullString.Length == 0)
        {
            return visited;
        }

        if (requiresWhitespace && fullString[0].RequiresWhitespace())
        {
            visited = visited.WithLeadingTrivia(SyntaxFactory.Whitespace(" "));
        }

        requiresWhitespace = fullString[fullString.Length - 1].RequiresWhitespace();

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