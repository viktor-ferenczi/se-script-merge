using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IngameScriptMerge;

public class Mappings : CSharpSyntaxWalker
{
    private readonly SyntaxNode root;
    private readonly SemanticModel semanticModel;
    private readonly string fullText;
    private readonly NameGenerator nameGenerator;
    private readonly bool collectLiterals;
    private readonly HashSet<string> excluded;

    public readonly Dictionary<string, string> NameMapping = new Dictionary<string, string>();
    public readonly Dictionary<SyntaxNode, string> NodeMapping = new Dictionary<SyntaxNode, string>();
    public readonly Dictionary<ISymbol, string> SymbolMapping = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);
    public readonly Dictionary<string, StringLiteral> Literals = new Dictionary<string, StringLiteral>();

    public Mappings(SyntaxNode root, SemanticModel semanticModel, string fullText, NameGenerator nameGenerator, bool collectLiterals)
    {
        this.root = root;
        this.semanticModel = semanticModel;
        this.fullText = fullText;
        this.nameGenerator = nameGenerator;
        this.collectLiterals = collectLiterals;

        excluded = IterExcludedNames().ToHashSet();
        excluded.DebugDump("excluded");
    }

    private ParallelQuery<string> IterExcludedNames()
    {
        return root
            .ToFullString()
            .IterSplit('\n')
            .AsParallel()
            .AsUnordered()
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

    #region Literals

    public override void VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        if (collectLiterals && node.IsKind(SyntaxKind.StringLiteralExpression))
        {
            var text = node.GetText().ToString().Trim();
            if (text.StartsWith("\""))
            {
                if (!Literals.TryGetValue(text, out var literal))
                {
                    Literals[text] = literal = new StringLiteral();
                }
                literal.Count++;
            }
        }
        base.VisitLiteralExpression(node);
    }

    public void FinalizeLiterals()
    {
        if (!collectLiterals)
        {
            return;
        }

        foreach (var text in Literals.Keys.ToList())
        {
            var literal = Literals[text];
            if (!literal.ShouldShorten(text))
            {
                Literals.Remove(text);
                continue;
            }

            var name = nameGenerator.Next();
            if (name == null)
            {
                Literals.Remove(text);
                continue;
            }

            literal.Shortened = name;
        }
    }

    #endregion

    #region Declarations

    public override void Visit(SyntaxNode node)
    {
        if (node.IsExcludedNamespace())
        {
            return;
        }

        var declaration = GetDeclaration(node);
        if (declaration != null)
        {
            StoreDeclaration(declaration);
        }

        base.Visit(node);
    }

    private Declaration GetDeclaration(SyntaxNode node)
    {
        var symbol = semanticModel.GetDeclaredSymbol(node);
        if (symbol == null)
        {
            return null;
        }

        Declaration declaration = null;
        switch (node.Kind())
        {
            case SyntaxKind.InterfaceDeclaration:
            case SyntaxKind.ClassDeclaration:
            case SyntaxKind.StructDeclaration:
            case SyntaxKind.EnumDeclaration:
            case SyntaxKind.TypeParameter:
                if (symbol.Name != "Program")
                {
                    declaration = new Declaration(node, symbol);
                }
                break;

            case SyntaxKind.ConstructorDeclaration:
            case SyntaxKind.DestructorDeclaration:
                if (symbol.ContainingType.Name != "Program")
                {
                    declaration = new Declaration(node, symbol);
                }
                break;

            case SyntaxKind.MethodDeclaration:
                if (IsValidMethod((MethodDeclarationSyntax) node, (IMethodSymbol) symbol, out var actualMethodSymbol))
                {
                    declaration = new Declaration(node, actualMethodSymbol);
                }
                break;

            case SyntaxKind.PropertyDeclaration:
                if (IsValidProperty((PropertyDeclarationSyntax) node, (IPropertySymbol) symbol, out var actualPropertySymbol))
                {
                    declaration = new Declaration(node, actualPropertySymbol);
                }
                break;

            case SyntaxKind.FieldDeclaration:
            case SyntaxKind.EnumMemberDeclaration:
            case SyntaxKind.Parameter:
            case SyntaxKind.VariableDeclarator:
            case SyntaxKind.ForEachStatement:
                declaration = new Declaration(node, symbol);
                break;
        }

        if (declaration == null)
        {
            return null;
        }

        if (IsExcludedDeclaration(declaration))
        {
            return null;
        }

        return declaration;
    }

    private bool IsExcludedDeclaration(Declaration declaration)
    {
        var start = declaration.Node.SpanStart;
        if (start < 0 || start >= fullText.Length)
        {
            return false;
        }

        var newLine = fullText.IndexOf('\n', start);
        if (newLine <= start)
        {
            return false;
        }

        var line = fullText.Substring(start, newLine - start);
        if (!line.Contains("//!"))
        {
            return false;
        }

        return true;
    }

    private void StoreDeclaration(Declaration declaration)
    {
        var name = declaration.Symbol.GetNameToShorten();
        if (name == null || name.Length < 2 || excluded.Contains(name))
        {
            return;
        }

        var shortenedName = NameMapping
            .TryGetValue(name, out var existingMappedName)
            ? existingMappedName
            : nameGenerator.Next();

        if (shortenedName == null)
        {
            return;
        }

        NameMapping[name] = shortenedName;
        NodeMapping[declaration.Node] = shortenedName;
        SymbolMapping[declaration.Symbol] = shortenedName;
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

    #endregion
}