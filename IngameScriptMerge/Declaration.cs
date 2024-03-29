using Microsoft.CodeAnalysis;

namespace IngameScriptMerge;

public record Declaration(SyntaxNode Node, ISymbol Symbol)
{
    public readonly SyntaxNode Node = Node;
    public readonly ISymbol Symbol = Symbol;
}
