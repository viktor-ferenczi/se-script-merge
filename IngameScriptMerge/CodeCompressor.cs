using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IngameScriptMerge;

record StringLiteral(SyntaxNode Node, string Shortened = null, int Count = 0)
{
    public readonly SyntaxNode Node = Node;
    public readonly string Shortened = Shortened;
    public int Count = Count;

    public bool ShouldShorten(string text)
    {
        // const string x=;
        return 17 + text.Length + Count < (2 + text.Length) * Count;
    }

    public override string ToString() => $"{Shortened} [{Count}]";
}

public class CodeCompressor : CSharpSyntaxRewriter
{
    private readonly SemanticModel semanticModel;
    private readonly SyntaxNode root;
    private readonly string fullText;

    private readonly Dictionary<string, string> nameMapping = new Dictionary<string, string>();
    private readonly Dictionary<SyntaxNode, string> nodeMapping = new Dictionary<SyntaxNode, string>();
    private readonly Dictionary<ISymbol, string> symbolMapping = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);

    private readonly Dictionary<string, StringLiteral> literals = new Dictionary<string, StringLiteral>();
    private bool literalsDefined;

    #region "Collecting declarations"

    private record Declaration(SyntaxNode Node, ISymbol Symbol)
    {
        public readonly ISymbol Symbol = Symbol;
        public readonly SyntaxNode Node = Node;
    }

    public CodeCompressor(CompiledScript compiledScript, bool aggressiveCompression) : base(true)
    {
        semanticModel = compiledScript.SemanticModel;
        root = compiledScript.SyntaxTree.GetRoot();
        fullText = root.ToFullString();

        var nameGenerator = CreateNameGenerator();

        BuildMapping(nameGenerator);

        if (aggressiveCompression)
        {
            CollectLiterals(nameGenerator);
        }

        nameMapping.DebugDump("nameMapping");
    }

    private NameGenerator CreateNameGenerator()
    {
        var forbidden = fullText.ToHashSet();
        forbidden.DebugDump("forbidden");

        var nameGenerator = new NameGenerator(forbidden);
        return nameGenerator;
    }

    private void CollectLiterals(NameGenerator nameGenerator)
    {
        var iterLiterals = root
            .DescendantNodes()
            .AsParallel()
#if DEBUG
            .WithDegreeOfParallelism(1)
#endif
            .AsUnordered()
            .Where(symbol => symbol.IsKind(SyntaxKind.StringLiteralExpression));

        var d = new Dictionary<string, StringLiteral>();
        foreach (var node in iterLiterals)
        {
            var text = node.GetText().ToString();
            if (text.Length < 3)
            {
                continue;
            }

            if (!d.TryGetValue(text, out var literal))
            {
                d[text] = literal = new StringLiteral(node);
            }

            literal.Count++;
        }

        foreach (var (text, literal) in d)
        {
            if (literal.ShouldShorten(text))
            {
                var shortened = nameGenerator.Next();
                if (shortened == null)
                {
                    break;
                }
                literals[text] = new StringLiteral(literal.Node, shortened, literal.Count);
            }
        }

        literals.DebugDump("literals");
    }

    private void BuildMapping(NameGenerator nameGenerator)
    {
        var iterDeclarations = root
            .DescendantNodes()
            .AsParallel()
#if DEBUG
            .WithDegreeOfParallelism(1)
#endif
            .AsUnordered()
            .Select(GetRelevantSymbol)
            .Where(symbol => symbol != null);

        var excluded = IterExcludedNames().ToHashSet();
        excluded.DebugDump("excluded");

        foreach (var declaration in iterDeclarations)
        {
            var name = declaration.Symbol.GetNameToShorten();
            if (name == null || name.Length < 2 || excluded.Contains(name))
            {
                continue;
            }

            var shortenedName = nameMapping
                .TryGetValue(name, out var existingMappedName)
                ? existingMappedName
                : nameGenerator.Next();

            if (shortenedName == null)
            {
                break;
            }

            nameMapping[name] = shortenedName;
            nodeMapping[declaration.Node] = shortenedName;
            symbolMapping[declaration.Symbol] = shortenedName;
        }
    }

    private Declaration GetRelevantSymbol(SyntaxNode node)
    {
        var symbol = semanticModel.GetDeclaredSymbol(node);
        if (symbol == null)
        {
            return null;
        }

        switch (node.Kind())
        {
            case SyntaxKind.InterfaceDeclaration:
            case SyntaxKind.ClassDeclaration:
            case SyntaxKind.StructDeclaration:
            case SyntaxKind.EnumDeclaration:
            case SyntaxKind.TypeParameter:
                return symbol.Name != "Program" &&
                       symbol.ContainingNamespace.Name.Length == 0
                    ? new Declaration(node, symbol)
                    : null;

            case SyntaxKind.ConstructorDeclaration:
            case SyntaxKind.DestructorDeclaration:
                return symbol.ContainingType.Name != "Program"
                    ? new Declaration(node, symbol)
                    : null;

            case SyntaxKind.MethodDeclaration:
                return IsValidMethod((MethodDeclarationSyntax) node, (IMethodSymbol) symbol, out var actualMethodSymbol)
                    ? new Declaration(node, actualMethodSymbol)
                    : null;

            case SyntaxKind.PropertyDeclaration:
                return IsValidProperty((PropertyDeclarationSyntax) node, (IPropertySymbol) symbol, out var actualPropertySymbol)
                    ? new Declaration(node, actualPropertySymbol)
                    : null;

            case SyntaxKind.FieldDeclaration:
            case SyntaxKind.EnumMemberDeclaration:
                return symbol.ContainingType == null ||
                       symbol.ContainingType.ContainingAssembly.Name == "Program"
                    ? new Declaration(node, symbol)
                    : null;

            case SyntaxKind.Parameter:
            case SyntaxKind.VariableDeclarator:
            case SyntaxKind.ForEachStatement:
                return new Declaration(node, symbol);

            default:
                return null;
        }
    }

    private bool IsValidMethod(MethodDeclarationSyntax node, IMethodSymbol symbol, out IMethodSymbol actualSymbol)
    {
        actualSymbol = symbol;

        var symbolNameSplit = symbol.Name.Split('.');
        var symbolNameWithoutInterface = symbolNameSplit[symbolNameSplit.Length - 1];

        var explicitInterfaceSpecifier = node.ChildNodes().OfType<ExplicitInterfaceSpecifierSyntax>().FirstOrDefault();
        if (explicitInterfaceSpecifier != null)
        {
            var interfaceName = explicitInterfaceSpecifier.Name.ToString().Split('<')[0];
            var explicitInterface = symbol.ContainingType.GetImplementedInterface<IMethodSymbol>(interfaceName, true);
            if (explicitInterface?.ContainingAssembly.Name != "Program")
            {
                return false;
            }
            actualSymbol = explicitInterface.GetMembers().First(m => m.Name == symbolNameWithoutInterface) as IMethodSymbol;
        }

        // Do not rename standard program methods
        if (symbol.ContainingType.Name == "Program" && symbol.Name is "Main" or "Save")
        {
            return false;
        }

        // Implementations of interface methods declared outside the program must not be renamed
        var baseTypeOrInterface = symbol.ContainingType.GetBaseOrInterfaceDeclaringMember<IMethodSymbol>(symbolNameWithoutInterface, true);
        if (baseTypeOrInterface != null && baseTypeOrInterface.ContainingAssembly.Name != "Program")
        {
            return false;
        }

        return true;
    }

    private bool IsValidProperty(PropertyDeclarationSyntax node, IPropertySymbol symbol, out IPropertySymbol actualSymbol)
    {
        actualSymbol = symbol;

        var symbolNameSplit = symbol.Name.Split('.');
        var symbolNameWithoutInterface = symbolNameSplit[symbolNameSplit.Length - 1];

        var explicitInterfaceSpecifier = node.ChildNodes().OfType<ExplicitInterfaceSpecifierSyntax>().FirstOrDefault();
        if (explicitInterfaceSpecifier != null)
        {
            var interfaceName = explicitInterfaceSpecifier.Name.ToString().Split('<')[0];
            var explicitInterface = symbol.ContainingType.GetImplementedInterface<IPropertySymbol>(interfaceName, true);
            if (explicitInterface?.ContainingAssembly.Name != "Program")
            {
                return false;
            }
            actualSymbol = explicitInterface.GetMembers().First(m => m.Name == symbolNameWithoutInterface) as IPropertySymbol;
        }

        // Implementations of interface methods declared outside the program must not be renamed
        var baseTypeOrInterface = symbol.ContainingType.GetBaseOrInterfaceDeclaringMember<IPropertySymbol>(symbolNameWithoutInterface, true);
        if (baseTypeOrInterface != null && baseTypeOrInterface.ContainingAssembly.Name != "Program")
        {
            return false;
        }

        return true;
    }

    private ParallelQuery<string> IterExcludedNames()
    {
        return root
            .ToFullString()
            .IterSplit('\n')
            .AsParallel()
#if DEBUG
            .WithDegreeOfParallelism(1)
#endif
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("//!"))
            .SelectMany(line => line
                .Substring(3)
                .TrimStart()
                .Split(',')
                .Select(name => name.Trim()));
    }

    #endregion

    #region "Rewriting declarations"

    public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        if (base.VisitInterfaceDeclaration(node) is not InterfaceDeclarationSyntax visited)
        {
            return null;
        }

        if (!nodeMapping.TryGetValue(node, out var shortenedName))
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

        if (!nodeMapping.TryGetValue(node, out var shortenedName))
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

        if (!nodeMapping.TryGetValue(node, out var shortenedName))
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

        if (!nodeMapping.TryGetValue(node, out var shortenedName))
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

        if (!nodeMapping.TryGetValue(node, out var shortenedName))
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

        if (!nodeMapping.TryGetValue(node, out var shortenedName))
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

        if (!nodeMapping.TryGetValue(node, out var shortenedName))
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

        if (!nodeMapping.TryGetValue(node, out var shortenedName))
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

        if (!nodeMapping.TryGetValue(node, out var shortenedName))
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

        if (!nodeMapping.TryGetValue(node, out var shortenedName))
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

        if (!nodeMapping.TryGetValue(node, out var shortenedName))
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

        if (!nodeMapping.TryGetValue(node, out var shortenedName))
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

        if (!symbolMapping.TryGetValue(symbol, out var shortenedName))
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

        // Must look up the symbol here, since it is NOT a declaration
        var symbolInfo = semanticModel.GetSymbolInfo(node);
        var symbol = symbolInfo.Symbol;
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

        if (!symbolMapping.TryGetValue(symbol, out var shortenedName))
        {
            return visited;
        }

        return SyntaxFactory
            .IdentifierName(shortenedName)
            .WithLeadingTrivia(visited.GetLeadingTrivia())
            .WithTrailingTrivia(visited.GetTrailingTrivia());
    }

    #endregion

    #region Shortening repeated literals

    public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        var visited = base.VisitLiteralExpression(node);

        if (visited == null || !visited.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return visited;
        }

        var text = node.GetText().ToString();
        if (!literals.TryGetValue(text, out var literal))
        {
            return visited;
        }

        return SyntaxFactory.IdentifierName(literal.Shortened);
    }

    public override SyntaxNode Visit(SyntaxNode node)
    {
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
        foreach (var (text, literal) in literals)
        {
            var literalExpressionSyntax = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(text.Substring(1, text.Length - 2)));

            var fieldDeclaration = SyntaxFactory
                .FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)))
                        .AddVariables(SyntaxFactory
                            .VariableDeclarator(SyntaxFactory.Identifier(literal.Shortened))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(literalExpressionSyntax))))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.ConstKeyword));

            classDeclarationSyntax = classDeclarationSyntax.AddMembers(fieldDeclaration);
        }
        return classDeclarationSyntax;
    }

    #endregion
}