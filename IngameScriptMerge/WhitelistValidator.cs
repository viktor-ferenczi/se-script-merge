using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IngameScriptMerge;

public class WhitelistValidator : CSharpSyntaxWalker
{
    public readonly List<string> Errors = [];
    private readonly SemanticModel semanticModel;
    private readonly HashSet<string> whitelistedNamespaces = [];
    private readonly HashSet<string> whitelistedTypes = [];
    private readonly HashSet<string> whitelistedMembers = [];

    public WhitelistValidator(CompiledScript compiledScript, string whitelistPath)
    {
        semanticModel = compiledScript.SemanticModel;
        RegisterWhitelist(whitelistPath);
    }

    private void RegisterWhitelist(string whitelistPath)
    {
        foreach (var line in File.ReadAllLines(whitelistPath))
        {
            var splitLine = line.Split(',');
            if (splitLine.Length != 2)
            {
                continue;
            }

            var name = splitLine[0].Trim();
            if (name.EndsWith(".*"))
            {
                var namespaceName = name.Substring(0, name.Length - 2);
                whitelistedNamespaces.Add(namespaceName);
            }
            else if (name.EndsWith("+*"))
            {
                var typeName = name.Substring(0, name.Length - 2);
                var type = Type.GetType(typeName);
                if (type == null)
                {
                    throw new Exception($"Could not find whitelisted type: {typeName}");
                }
                whitelistedMembers.AddRange(type.GetMembers().Select(m => m.ToString()));
            }
            else if (name.Contains("("))
            {
                var typeName = name.Substring(0, name.Length - 2);
                var type = Type.GetType(typeName);
                if (type == null)
                {
                    throw new Exception($"Could not find whitelisted type: {typeName}");
                }
                whitelistedMembers.Add(name);
            }
            else
            {
                var type = Type.GetType(name);
                if (type == null)
                {
                    throw new Exception($"Could not find whitelisted type: {name}");
                }
                whitelistedTypes.Add(type.FullName);
            }
        }
    }

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        var symbol = semanticModel.GetSymbolInfo(node).Symbol;
        if (symbol == null)
        {
            var memberGroup = semanticModel.GetMemberGroup(node);
            if (memberGroup == null)
            {
                return;
            }
            symbol = memberGroup.FirstOrDefault();
            if (symbol == null)
            {
                return;
            }
        }

        // TODO
        switch (symbol.Kind)
        {
            case SymbolKind.Field:
                break;
            case SymbolKind.Method:
                break;
            case SymbolKind.NamedType:
                break;
            case SymbolKind.Property:
                break;
            case SymbolKind.TypeParameter:
                break;
        }

        // TODO: USEFUL
        while (!symbol.IsDefinition)
        {
            symbol = symbol.OriginalDefinition;
        }

        base.VisitIdentifierName(node);
    }

    public override void VisitQualifiedName(QualifiedNameSyntax node)
    {
        base.VisitQualifiedName(node);
    }

    public override void VisitGenericName(GenericNameSyntax node)
    {
        base.VisitGenericName(node);
    }
}