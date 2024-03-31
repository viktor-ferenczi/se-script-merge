using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IngameScriptMerge;

public class CodeCompressor : CSharpSyntaxRewriter
{
    private readonly SemanticModel semanticModel;
    private readonly Mappings mappings;
    private bool literalsDefined;

    #region "Collecting declarations"

    public CodeCompressor(CompiledScript compiledScript, bool aggressiveCompression) : base(true)
    {
        semanticModel = compiledScript.SemanticModel;

        var root = compiledScript.SyntaxTree.GetRoot();
        var fullText = root.ToFullString();

        var forbidden = fullText.ToHashSet();
        forbidden.DebugDump("forbidden");

        var nameGenerator = new NameGenerator(forbidden);

        mappings = new Mappings(root, semanticModel, fullText, nameGenerator, aggressiveCompression);

        mappings.Visit(root);
        mappings.FinalizeLiterals();

        mappings.NameMapping.DebugDump("nameMapping");
        mappings.Literals.DebugDump("literals");
    }

    #endregion

    #region "Rewriting declarations"

    public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        if (base.VisitInterfaceDeclaration(node) is not InterfaceDeclarationSyntax visited)
        {
            return null;
        }

        if (!mappings.NodeMapping.TryGetValue(node, out var shortenedName))
        {
            return visited;
        }

        var identifier = visited.Identifier;
        var newIdentifier = SyntaxFactory
            .Identifier(shortenedName)
            .WithLeadingTrivia(identifier.LeadingTrivia)
            .WithTrailingTrivia(identifier.TrailingTrivia);

        return visited.WithIdentifier(newIdentifier);
    }

    public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (base.VisitClassDeclaration(node) is not ClassDeclarationSyntax visited)
        {
            return null;
        }

        if (!mappings.NodeMapping.TryGetValue(node, out var shortenedName))
        {
            return visited;
        }

        var identifier = visited.Identifier;
        var newIdentifier = SyntaxFactory
            .Identifier(shortenedName)
            .WithLeadingTrivia(identifier.LeadingTrivia)
            .WithTrailingTrivia(identifier.TrailingTrivia);

        return visited.WithIdentifier(newIdentifier);
    }

    public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
    {
        if (base.VisitStructDeclaration(node) is not StructDeclarationSyntax visited)
        {
            return null;
        }

        if (!mappings.NodeMapping.TryGetValue(node, out var shortenedName))
        {
            return visited;
        }

        var identifier = visited.Identifier;
        var newIdentifier = SyntaxFactory
            .Identifier(shortenedName)
            .WithLeadingTrivia(identifier.LeadingTrivia)
            .WithTrailingTrivia(identifier.TrailingTrivia);

        return visited.WithIdentifier(newIdentifier);
    }

    public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        if (base.VisitConstructorDeclaration(node) is not ConstructorDeclarationSyntax visited)
        {
            return null;
        }

        if (!mappings.NodeMapping.TryGetValue(node, out var shortenedName))
        {
            return visited;
        }

        var identifier = visited.Identifier;
        var newIdentifier = SyntaxFactory
            .Identifier(shortenedName)
            .WithLeadingTrivia(identifier.LeadingTrivia)
            .WithTrailingTrivia(identifier.TrailingTrivia);

        return visited.WithIdentifier(newIdentifier);
    }

    public override SyntaxNode VisitDestructorDeclaration(DestructorDeclarationSyntax node)
    {
        if (base.VisitDestructorDeclaration(node) is not DestructorDeclarationSyntax visited)
        {
            return null;
        }

        if (!mappings.NodeMapping.TryGetValue(node, out var shortenedName))
        {
            return visited;
        }

        var identifier = visited.Identifier;
        var newIdentifier = SyntaxFactory
            .Identifier(shortenedName)
            .WithLeadingTrivia(identifier.LeadingTrivia)
            .WithTrailingTrivia(identifier.TrailingTrivia);

        return visited.WithIdentifier(newIdentifier);
    }

    public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (base.VisitMethodDeclaration(node) is not MethodDeclarationSyntax visited)
        {
            return null;
        }

        if (!mappings.NodeMapping.TryGetValue(node, out var shortenedName))
        {
            return visited;
        }

        var identifier = visited.Identifier;
        var newIdentifier = SyntaxFactory
            .Identifier(shortenedName)
            .WithLeadingTrivia(identifier.LeadingTrivia)
            .WithTrailingTrivia(identifier.TrailingTrivia);

        return visited.WithIdentifier(newIdentifier);
    }

    public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        if (base.VisitEnumDeclaration(node) is not EnumDeclarationSyntax visited)
        {
            return null;
        }

        if (!mappings.NodeMapping.TryGetValue(node, out var shortenedName))
        {
            return visited;
        }

        var identifier = visited.Identifier;
        var newIdentifier = SyntaxFactory
            .Identifier(shortenedName)
            .WithLeadingTrivia(identifier.LeadingTrivia)
            .WithTrailingTrivia(identifier.TrailingTrivia);

        return visited.WithIdentifier(newIdentifier);
    }

    public override SyntaxNode VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
    {
        if (base.VisitEnumMemberDeclaration(node) is not EnumMemberDeclarationSyntax visited)
        {
            return null;
        }

        if (!mappings.NodeMapping.TryGetValue(node, out var shortenedName))
        {
            return visited;
        }

        var identifier = visited.Identifier;
        var newIdentifier = SyntaxFactory
            .Identifier(shortenedName)
            .WithLeadingTrivia(identifier.LeadingTrivia)
            .WithTrailingTrivia(identifier.TrailingTrivia);

        return visited.WithIdentifier(newIdentifier);
    }

    public override SyntaxNode VisitTypeParameter(TypeParameterSyntax node)
    {
        if (base.VisitTypeParameter(node) is not TypeParameterSyntax visited)
        {
            return null;
        }

        if (!mappings.NodeMapping.TryGetValue(node, out var shortenedName))
        {
            return visited;
        }

        var identifier = visited.Identifier;
        var newIdentifier = SyntaxFactory
            .Identifier(shortenedName)
            .WithLeadingTrivia(identifier.LeadingTrivia)
            .WithTrailingTrivia(identifier.TrailingTrivia);

        return visited.WithIdentifier(newIdentifier);
    }

    public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        if (base.VisitPropertyDeclaration(node) is not PropertyDeclarationSyntax visited)
        {
            return null;
        }

        if (!mappings.NodeMapping.TryGetValue(node, out var shortenedName))
        {
            return visited;
        }

        var identifier = visited.Identifier;
        var newIdentifier = SyntaxFactory
            .Identifier(shortenedName)
            .WithLeadingTrivia(identifier.LeadingTrivia)
            .WithTrailingTrivia(identifier.TrailingTrivia);

        return visited.WithIdentifier(newIdentifier);
    }

    public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
    {
        if (base.VisitVariableDeclarator(node) is not VariableDeclaratorSyntax visited)
        {
            return null;
        }

        if (!mappings.NodeMapping.TryGetValue(node, out var shortenedName))
        {
            return visited;
        }

        var identifier = node.Identifier;
        var newIdentifier = SyntaxFactory
            .Identifier(shortenedName)
            .WithLeadingTrivia(identifier.LeadingTrivia)
            .WithTrailingTrivia(identifier.TrailingTrivia);

        return visited.WithIdentifier(newIdentifier);
    }

    public override SyntaxNode VisitForEachStatement(ForEachStatementSyntax node)
    {
        if (base.VisitForEachStatement(node) is not ForEachStatementSyntax visited)
        {
            return null;
        }

        if (!mappings.NodeMapping.TryGetValue(node, out var shortenedName))
        {
            return visited;
        }

        var identifier = visited.ChildTokens().FirstOrDefault(t => t.IsKind(SyntaxKind.IdentifierToken));
        if (identifier == default)
        {
            return visited;
        }

        var newIdentifier = SyntaxFactory
            .Identifier(shortenedName)
            .WithLeadingTrivia(identifier.LeadingTrivia)
            .WithTrailingTrivia(identifier.TrailingTrivia);

        return visited.ReplaceToken(identifier, newIdentifier);
    }

    public override SyntaxNode VisitParameter(ParameterSyntax node)
    {
        if (base.VisitParameter(node) is not ParameterSyntax visited)
        {
            return null;
        }

        var symbol = semanticModel.GetDeclaredSymbol(node);
        if (symbol == null)
        {
            return visited;
        }

        if (!mappings.SymbolMapping.TryGetValue(symbol, out var shortenedName))
        {
            return visited;
        }

        var identifier = node.Identifier;
        var newIdentifier = SyntaxFactory
            .Identifier(shortenedName)
            .WithLeadingTrivia(identifier.LeadingTrivia)
            .WithTrailingTrivia(identifier.TrailingTrivia);

        return visited.WithIdentifier(newIdentifier);
    }

    #endregion

    #region Rewriting usages

    public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (base.VisitIdentifierName(node) is not IdentifierNameSyntax visited)
        {
            return null;
        }

        if (visited.IsVar)
        {
            return visited;
        }

        // Must look up the symbol here, since it is NOT a declaration
        var symbol = semanticModel.GetSymbolInfo(node).Symbol;
        if (symbol == null)
        {
            var memberGroup = semanticModel.GetMemberGroup(node);
            if (memberGroup == null)
            {
                return visited;
            }
            symbol = memberGroup.FirstOrDefault();
            if (symbol == null)
            {
                return visited;
            }
        }

        // Resolve specialized methods to their original, generic definition
        while (!symbol.IsDefinition)
        {
            symbol = symbol.OriginalDefinition;
        }

        if (!mappings.SymbolMapping.TryGetValue(symbol, out var shortenedName))
        {
            return visited;
        }

        return SyntaxFactory
            .IdentifierName(shortenedName)
            .WithLeadingTrivia(visited.GetLeadingTrivia())
            .WithTrailingTrivia(visited.GetTrailingTrivia());
    }

    public override SyntaxNode VisitGenericName(GenericNameSyntax node)
    {
        if (base.VisitGenericName(node) is not GenericNameSyntax visited)
        {
            return null;
        }

        if (visited.IsVar)
        {
            return visited;
        }

        // Must look up the symbol here, since it is NOT a declaration
        var symbol = semanticModel.GetSymbolInfo(node).Symbol;
        if (symbol == null)
        {
            var memberGroup = semanticModel.GetMemberGroup(node);
            if (memberGroup == null)
            {
                return visited;
            }
            symbol = memberGroup.FirstOrDefault();
            if (symbol == null)
            {
                return visited;
            }
        }

        // Resolve specialized methods to their original, generic definition
        while (!symbol.IsDefinition)
        {
            symbol = symbol.OriginalDefinition;
        }

        if (!mappings.SymbolMapping.TryGetValue(symbol, out var shortenedName))
        {
            return visited;
        }

        return SyntaxFactory
            .GenericName(shortenedName)
            .WithTypeArgumentList(visited.TypeArgumentList)
            .WithLeadingTrivia(visited.GetLeadingTrivia())
            .WithTrailingTrivia(visited.GetTrailingTrivia());
    }

    #endregion

    #region Shortening repeated string literals

    public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        var visited = base.VisitLiteralExpression(node);

        if (visited == null || !visited.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return visited;
        }

        var text = node.GetText().ToString().Trim();
        if (!mappings.Literals.TryGetValue(text, out var literal))
        {
            return visited;
        }

        return SyntaxFactory.IdentifierName(literal.Shortened);
    }

    public override SyntaxNode Visit(SyntaxNode node)
    {
        if (node.IsExcludedNamespace())
        {
            return node;
        }

        var visited = base.Visit(node);

        if (visited is ClassDeclarationSyntax classDeclarationSyntax)
        {
            if (!literalsDefined && classDeclarationSyntax.IsProgramClassDeclaration())
            {
                classDeclarationSyntax = AddLiteralStringDefinitions(classDeclarationSyntax);
                literalsDefined = true;
            }
            return classDeclarationSyntax; //.WithModifiers([]);
        }

        // NOTE: Access modifiers cannot always be removed without causing an inconsistent member access error.

        /*
        if (visited is InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            return interfaceDeclarationSyntax.WithModifiers([]);
        }

        if (visited is StructDeclarationSyntax structDeclarationSyntax)
        {
            return structDeclarationSyntax.WithModifiers([]);
        }
        */

        return visited;
    }

    private ClassDeclarationSyntax AddLiteralStringDefinitions(ClassDeclarationSyntax classDeclarationSyntax)
    {
        foreach (var (text, literal) in mappings.Literals)
        {
            var literalToken = CreateLiteralTokenFromQuotedEscapedText(text);
            var literalExpressionSyntax = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, literalToken);

            var fieldDeclaration = SyntaxFactory
                .FieldDeclaration(SyntaxFactory
                    .VariableDeclaration(SyntaxFactory
                        .PredefinedType(SyntaxFactory
                            .Token(SyntaxKind.StringKeyword)))
                    .AddVariables(SyntaxFactory
                        .VariableDeclarator(SyntaxFactory
                            .Identifier(literal.Shortened))
                        .WithInitializer(SyntaxFactory
                            .EqualsValueClause(literalExpressionSyntax))))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.ConstKeyword));

            classDeclarationSyntax = classDeclarationSyntax.AddMembers(fieldDeclaration);
        }
        return classDeclarationSyntax;
    }

    private static SyntaxToken CreateLiteralTokenFromQuotedEscapedText(string text)
    {
        // Unquote and unescape
        var parsedSyntaxTree = CSharpSyntaxTree.ParseText($"var dummy = {text};");
        var root = parsedSyntaxTree.GetRoot() as CompilationUnitSyntax;
        var literalExpression = root?.DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .First();

        var unescapedText = (string) literalExpression?.Token.Value ?? "???";
        var literalToken = SyntaxFactory.Literal(text, unescapedText);
        return literalToken;
    }

    #endregion
}