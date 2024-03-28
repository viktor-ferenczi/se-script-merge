using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Gui;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScriptMerge;

public class CompiledScript
{
    private static readonly string[] ScriptAccessibleNamespaces =
    [
        "Sandbox.Game.EntityComponents",
        "Sandbox.ModAPI.Ingame",
        "Sandbox.ModAPI.Interfaces",
        "SpaceEngineers.Game.ModAPI.Ingame",
        "System",
        "System.Collections",
        "System.Collections.Generic",
        "System.Collections.Immutable",
        "System.Linq",
        "System.Text",
        "VRage",
        "VRage.Collections",
        "VRage.Game",
        "VRage.Game.Components",
        "VRage.Game.GUI.TextPanel",
        "VRage.Game.ModAPI.Ingame",
        "VRage.Game.ModAPI.Ingame.Utilities",
        "VRage.Game.ObjectBuilders.Definitions",
        "VRageMath",
    ];

    private static readonly Type[] TypesForAssemblyReferences =
    [
        typeof(object), // System
        typeof(ImmutableArray), // System.Collections.Immutable
        typeof(Enumerable), // System.Core
        typeof(XmlReader), // System.Xml
        typeof(IMyMedicalRoom), // SpaceEngineers.Game
        typeof(MyObjectBuilder_AirVent), // SpaceEngineers.ObjectBuilders
        typeof(IMyTerminalBlock), // Sandbox.Common
        typeof(TerminalActionExtensions), // Sandbox.Game
        typeof(MyTexts.MyLanguageDescription), // VRage
        typeof(IMyEntity), // VRage.Game
        typeof(MyTuple), // VRage.Library
        typeof(Vector3), // VRage.Math
    ];

    public readonly SyntaxTree SyntaxTree;
    public readonly SemanticModel SemanticModel;

    public CompiledScript(ICollection<SyntaxNode> nodes)
    {
        var compilationUnit = SyntaxFactory
            .CompilationUnit()
            .AddUsings(ScriptAccessibleNamespaces.Select(ns => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns))).ToArray())
            .AddMembers(nodes.OfType<MemberDeclarationSyntax>().ToArray());

        SyntaxTree = SyntaxFactory.SyntaxTree(compilationUnit);

        var references = TypesForAssemblyReferences
            .Select(t => MetadataReference.CreateFromFile(t.Assembly.Location))
            .ToList();

        var compilation = CSharpCompilation.Create(
            assemblyName: "Program",
            syntaxTrees: new[] { SyntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        SemanticModel = compilation.GetSemanticModel(SyntaxTree);
    }
}