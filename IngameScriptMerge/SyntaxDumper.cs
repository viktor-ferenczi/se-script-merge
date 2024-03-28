using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace IngameScriptMerge;

public class SyntaxDumper(StringBuilder stringBuilder, int startIndentation = 0, SyntaxWalkerDepth depth = SyntaxWalkerDepth.Node) : CSharpSyntaxWalker(depth)
{
    public static void Dump(StringBuilder stringBuilder, SyntaxNode node, SyntaxWalkerDepth depth = SyntaxWalkerDepth.Node)
    {
        new SyntaxDumper(stringBuilder, 0, depth).Visit(node);
    }

    public static void Dump(StringBuilder stringBuilder, IEnumerable<SyntaxNode> nodes, SyntaxWalkerDepth depth = SyntaxWalkerDepth.Node)
    {
        new SyntaxDumper(stringBuilder, 0, depth).Visit(nodes);
    }

    public override void Visit(SyntaxNode node)
    {
        var indentation = string.Concat(Enumerable.Repeat("  ", startIndentation));
        stringBuilder.AppendLine($"{indentation}- Node<{node?.GetType().Name}> [{node?.Kind().ToString()}] `{node?.ToFullString().Shorten()}`");

        startIndentation++;
        base.Visit(node);
        startIndentation--;
    }

    public override void VisitToken(SyntaxToken token)
    {
        var indentation = string.Concat(Enumerable.Repeat("  ", startIndentation));

        stringBuilder.AppendLine($"{indentation}- Token<{token.GetType().Name}> [{token.Kind().ToString()}] `{token.ToFullString().Shorten()}`");

        if (token.HasLeadingTrivia)
        {
            stringBuilder.AppendLine($"{indentation}  - LeadingTrivia");
            foreach (var trivia in token.LeadingTrivia)
            {
                stringBuilder.AppendLine($"{indentation}    - Trivia<{trivia.GetType().Name}> [{trivia.Kind().ToString()}] `{trivia.ToFullString().Shorten()}`");
            }
        }

        if (token.HasTrailingTrivia)
        {
            stringBuilder.AppendLine($"{indentation}  - TrailingTrivia");
            foreach (var trivia in token.TrailingTrivia)
            {
                stringBuilder.AppendLine($"{indentation}    - Trivia<{trivia.GetType().Name}> [{trivia.Kind().ToString()}] `{trivia.ToFullString().Shorten()}`");
            }
        }

        startIndentation++;
        base.VisitToken(token);
        startIndentation--;
    }
}