using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using CodeGenHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EpDeviceManagement.Data.Generation;

#nullable enable
[Generator]
public class CsvGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var files = context.AdditionalTextsProvider
            .Where(a => a.Path.EndsWith(".csv"))
            .Select((a, c) => (Path.GetFileNameWithoutExtension(a.Path), a.GetText(c)!.Lines.First().ToString()));

        var types = context.SyntaxProvider
            .CreateSyntaxProvider(CouldBeGenerateFromCsvAttribute, GetMarkedClassOrNull)
            .Where(type => type != default);

        var compilationAndFiles = context
            .CompilationProvider
            .Combine(files.Collect())
            .Combine(types.Collect())
            .Flatten();

        context.RegisterSourceOutput(compilationAndFiles, Generate);
    }

    private bool CouldBeGenerateFromCsvAttribute(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        if (syntaxNode is not AttributeSyntax attribute)
        {
            return false;
        }

        var name = ExtractName(attribute.Name);

        return name is nameof(GenerateFromCsvAttribute) or "GenerateFromCsv";
    }

    private static string? ExtractName(NameSyntax? name)
    {
        return name switch
        {
            SimpleNameSyntax sns => sns.Identifier.Text,
            QualifiedNameSyntax qns => qns.Right.Identifier.Text,
            _ => null,
        };
    }

    private static (ITypeSymbol?, AttributeData) GetMarkedClassOrNull(
        GeneratorSyntaxContext context,
        CancellationToken token)
    {
        var attributeSyntax = (AttributeSyntax)context.Node;
        if (attributeSyntax.Parent?.Parent is not ClassDeclarationSyntax classDeclaration)
        {
            return default;
        }

        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is ITypeSymbol type
            && IsMarkedAsGenerateFromCsv(type, out var attribute))
        {
            return (type, attribute);
        }

        return default;

        //return (type is null || !IsMarkedAsGenerateFromCsv(type)) ? null : type;
    }

    private static bool IsMarkedAsGenerateFromCsv(ITypeSymbol type, out AttributeData? attribute)
    {
        attribute = type.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == nameof(GenerateFromCsvAttribute)
                                 && a.AttributeClass.ContainingNamespace is
                                 {
                                     Name:
                                     $"{nameof(EpDeviceManagement)}.{nameof(EpDeviceManagement.Data)}.{nameof(EpDeviceManagement.Data.Generation)}",
                                     ContainingNamespace.IsGlobalNamespace: true
                                 });
        return attribute != null;
    }

    private void Generate(
        SourceProductionContext productionContext,
        (
            Compilation compilation,
            ImmutableArray<(string filename, string content)> files,
            ImmutableArray<(ITypeSymbol? type, AttributeData attribute)> typeSymbols)
            compilationAndFiles)
    {
//#if DEBUG
//            if (!Debugger.IsAttached)
//            {
//                Debugger.Launch();
//            }
//#endif
        foreach (var (filename, firstLine) in compilationAndFiles.files)
        {
            var tuple = compilationAndFiles.typeSymbols.FirstOrDefault(tuple =>
                tuple.attribute?.ConstructorArguments[0].Value == filename);
            string typeName;
            Type? defaultType;
            IReadOnlyList<string> alreadyDefinedProperties;
            if (tuple != default)
            {
                var (type, attribute) = tuple;
                typeName = type!.Name;
                defaultType = (Type?) attribute.NamedArguments
                    .SingleOrDefault(pair => pair.Key == nameof(GenerateFromCsvAttribute.DefaultType))
                    .Value
                    .Value;
                alreadyDefinedProperties = type.GetMembers().OfType<IPropertySymbol>().Select(m => m.Name).ToList();
            }
            else
            {
                typeName = filename.Split('.')[0];
                defaultType = typeof(string);
                alreadyDefinedProperties = new List<string>();
            }
            defaultType ??= typeof(string);

            var propertyNames = firstLine.Split(',');
            var source = CodeBuilder
                .Create(compilationAndFiles.compilation.GlobalNamespace)
                .AddClass(typeName)
                .MakePublicClass();
            foreach (var property in propertyNames.Except(alreadyDefinedProperties))
            {
                source = source
                    .AddProperty(property, Accessibility.Public)
                    .SetType(defaultType.FullName)
                    .UseAutoProps(Accessibility.Public);
            }

            productionContext.AddSource(Path.ChangeExtension(Path.GetFileName(filename), ".g.cs"), source.Build());
        }
    }
}
#nullable restore