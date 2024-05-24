using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace IngameScriptMerge;

public static class MergeExtensions
{
    public static void Visit(this CSharpSyntaxWalker sv, IEnumerable<SyntaxNode> nodes)
    {
        foreach (var node in nodes)
        {
            sv.Visit(node);
        }
    }

    public static bool IsExcludedNamespace(this SyntaxNode node)
    {
        return node.IsKind(SyntaxKind.NamespaceDeclaration) &&
               node.ToFullString().Contains("//!!");
    }

    public static int MeasureIndentation(this string line) => line.Length - line.TrimStart().Length;

    public static IEnumerable<string> IterSplit(this string text, char separator)
    {
        var start = 0;
        var end = text.IndexOf(separator);
        while (end >= 0)
        {
            yield return text.Substring(start, end - start);

            start = end + 1;
            if (start >= text.Length)
            {
                yield return "";
                yield break;
            }

            end = text.IndexOf(separator, start);
        }

        yield return text.Substring(start);
    }

    public static IEnumerable<string> RemoveCommonIndentation(this ICollection<string> lines)
    {
        var indentation = lines.Any()
            ? lines
                .Where(line => line.TrimStart().Length != 0)
                .Select(MeasureIndentation).Min()
            : 0;

        foreach (var line in lines)
        {
            yield return indentation < line.Length ? line.Substring(indentation) : "";
        }
    }

    public static IEnumerable<string> TrimAndRemoveEmptyLines(this IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.Length != 0)
            {
                yield return trimmedLine;
            }
        }
    }

    public static string Shorten(this string t, int maxLength = 100)
    {
        t = t.Replace("\r\n", "\n");
        var s = t.Split('\n');
        var r = string.Join(@"\n", s);
        var l = maxLength - 3;
        var h = l / 2;
        var c = l - h;
        return r.Length <= l ? r : r.Substring(0, h) + "..." + r.Substring(r.Length - c, c);
    }

    public static bool IsProgramClassDeclaration(this SyntaxNode node)
    {
        return node != null &&
               node.IsKind(SyntaxKind.ClassDeclaration) &&
               node.ChildTokens().Any(
                   token => token.IsKind(SyntaxKind.IdentifierToken) &&
                            token.Text.Trim() == "Program"
               );
    }

    public static bool IsAllowedInIdentifier(this char c)
    {
        // https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names
        return CharUnicodeInfo.GetUnicodeCategory(c) switch
        {
            UnicodeCategory.UppercaseLetter => true, // Lu
            UnicodeCategory.LowercaseLetter => true, // Ll
            UnicodeCategory.TitlecaseLetter => true, // Lt
            UnicodeCategory.ModifierLetter => true, // Lm
            UnicodeCategory.OtherLetter => true, // Lo
            UnicodeCategory.LetterNumber => true, // Nl
            _ => false,
        };
    }

    public static string GetNameToShorten(this ISymbol symbol)
    {
        switch (symbol)
        {
            case IMethodSymbol methodSymbol:
                switch (methodSymbol.MethodKind)
                {
                    case MethodKind.Ordinary:
                    case MethodKind.LocalFunction:
                    case MethodKind.DeclareMethod:
                    case MethodKind.FunctionPointerSignature:
                    case MethodKind.ExplicitInterfaceImplementation:
                        break;

                    case MethodKind.StaticConstructor:
                    case MethodKind.Constructor:
                    case MethodKind.Destructor:
                        return symbol.ContainingType.Name;

                    default:
                        return null;
                }
                break;
        }

        return symbol.Name;
    }

    public static void RecreateDebugDir()
    {
#if DEBUG
        if (Directory.Exists("debug"))
        {
            Directory.Delete("debug", recursive: true);
        }
        Directory.CreateDirectory("debug");
#endif
    }

#if DEBUG
    private static int debugSequenceNumber = 1;
#endif

    public static void DebugDump<T>(this IEnumerable<T> data, string name)
    {
#if DEBUG
        var sortedData = data.ToList();
        sortedData.Sort();

        var path = $@"debug\{debugSequenceNumber++}-{name}.md";
        var iterLines = sortedData.Select(v => $"- `{v.ToString().Replace("\n", "\\n")}`");

        File.WriteAllLines(path, iterLines);
#endif
    }

    public static void DebugDump<T>(this IDictionary<string, T> data, string name)
    {
#if DEBUG
        var sortedKeys = data.Keys.ToList();
        sortedKeys.Sort();

        var path = $@"debug\{debugSequenceNumber++}-{name}.md";
        var iterLines = sortedKeys.Select(key => $"- `{key}` => `{data[key].ToString().Replace("\f", "\\f").Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n")}`");

        File.WriteAllLines(path, iterLines);
#endif
    }

    public static void DebugDump(this SyntaxNode node, string name)
    {
#if DEBUG
        var sb = new StringBuilder();
        SyntaxDumper.Dump(sb, node, SyntaxWalkerDepth.StructuredTrivia);

        File.WriteAllText($@"debug\{debugSequenceNumber++}-{name}.md", sb.ToString(), Encoding.UTF8);
#endif
    }

    public static void DebugDump(this IEnumerable<SyntaxNode> nodes, string name)
    {
#if DEBUG
        var sb = new StringBuilder();
        SyntaxDumper.Dump(sb, nodes, SyntaxWalkerDepth.StructuredTrivia);

        File.WriteAllText($@"debug\{debugSequenceNumber++}-{name}.md", sb.ToString(), Encoding.UTF8);
#endif
    }

    public static ITypeSymbol GetBaseOrInterfaceDeclaringMember<T>(this INamedTypeSymbol type, string name, bool ignoreThis = false) where T : ISymbol
    {
        if (!ignoreThis && type.MemberNames.Contains(name))
        {
            return type;
        }

        var t = type.BaseType?.GetBaseOrInterfaceDeclaringMember<T>(name);
        if (t != null)
        {
            return t;
        }

        return type
            .Interfaces
            .Select(i => i.GetBaseOrInterfaceDeclaringMember<T>(name))
            .FirstOrDefault(i => i != null);
    }

    public static ITypeSymbol GetBaseClass<T>(this INamedTypeSymbol type, string name, bool ignoreThis = false) where T : ISymbol
    {
        if (!ignoreThis && type.Name == name)
        {
            return type;
        }

        return type.BaseType?.GetBaseClass<T>(name);
    }

    public static ITypeSymbol GetImplementedInterface<T>(this INamedTypeSymbol type, string name, bool ignoreThis = false) where T : ISymbol
    {
        if (!ignoreThis && type.Name == name)
        {
            return type;
        }

        return type
            .Interfaces
            .Select(i => i.GetImplementedInterface<T>(name))
            .FirstOrDefault(v => v != null);
    }

    public static bool RequiresWhitespace(this char c)
    {
        if (c == '_')
        {
            return true;
        }

        // https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names
        return CharUnicodeInfo.GetUnicodeCategory(c) switch
        {
            UnicodeCategory.UppercaseLetter => true, // Lu
            UnicodeCategory.LowercaseLetter => true, // Ll
            UnicodeCategory.TitlecaseLetter => true, // Lt
            UnicodeCategory.ModifierLetter => true, // Lm
            UnicodeCategory.OtherLetter => true, // Lo
            UnicodeCategory.LetterNumber => true, // Nl
            UnicodeCategory.OtherNumber => true,
            UnicodeCategory.DecimalDigitNumber => true,
            _ => false,
        };
    }
}