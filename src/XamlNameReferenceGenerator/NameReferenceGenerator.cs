﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using XamlNameReferenceGenerator.Parsers;

namespace XamlNameReferenceGenerator
{
    [Generator]
    public class NameReferenceGenerator : ISourceGenerator
    {
        private const string AttributeName = "XamlNameReferenceGenerator.GenerateTypedNameReferencesAttribute";
        private const string AttributeFile = "GenerateTypedNameReferencesAttribute";
        private const string AttributeCode = @"// <auto-generated />

using System;

namespace XamlNameReferenceGenerator
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    sealed class GenerateTypedNameReferencesAttribute : Attribute
    {
        public GenerateTypedNameReferencesAttribute() { }

        public string[] AdditionalNamespaces { get; set; } = null;
    }
}
";
        private const string DebugPath = @"C:\Users\prizr\Documents\GitHub\XamlNameReferenceGenerator\debug.txt";
        private static readonly NameReferenceDebugger Debugger = new NameReferenceDebugger(DebugPath);
        private static readonly SymbolDisplayFormat SymbolDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters |
                             SymbolDisplayGenericsOptions.IncludeTypeConstraints |
                             SymbolDisplayGenericsOptions.IncludeVariance);

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new NameReferenceSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource(AttributeFile, SourceText.From(AttributeCode, Encoding.UTF8));
            if (!(context.SyntaxReceiver is NameReferenceSyntaxReceiver receiver))
                return;

            var compilation = (CSharpCompilation) context.Compilation;
            var xamlParser = new XamlXRawNameReferenceXamlParser();
            var symbols = UnpackAnnotatedTypes(compilation, receiver);
            foreach (var (typeSymbol, additionalNamespaces) in symbols)
            {
                var relevantXamlFile = context.AdditionalFiles
                    .First(text =>
                        text.Path.EndsWith($"{typeSymbol.Name}.xaml") ||
                        text.Path.EndsWith($"{typeSymbol.Name}.axaml"));

                var sourceCode = Debugger.Debug(
                    () => GenerateSourceCode(xamlParser, typeSymbol, relevantXamlFile, additionalNamespaces));
                context.AddSource($"{typeSymbol.Name}.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
            }
        }

        private static string GenerateSourceCode(
            INameReferenceXamlParser xamlParser,
            INamedTypeSymbol classSymbol,
            AdditionalText xamlFile,
            IList<string> additionalNamespaces)
        {
            var className = classSymbol.Name;
            var nameSpace = classSymbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat);
            var namespaces = additionalNamespaces.Select(name => $"using {name};");
            var namedControls = xamlParser
                .GetNamedControls(xamlFile.GetText()!.ToString())
                .Select(info => "        " +
                                $"public {info.TypeName} {info.Name} => " +
                                $"this.FindControl<{info.TypeName}>(\"{info.Name}\");");
            return $@"// <auto-generated />

using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
{string.Join("\n", namespaces)}

namespace {nameSpace}
{{
    public partial class {className}
    {{
{string.Join("\n", namedControls)}   
    }}
}}
";
        }

        private static IReadOnlyList<(INamedTypeSymbol Type, IList<string> Namespaces)> UnpackAnnotatedTypes(
            CSharpCompilation existingCompilation,
            NameReferenceSyntaxReceiver nameReferenceSyntaxReceiver)
        {
            var options = (CSharpParseOptions)existingCompilation.SyntaxTrees[0].Options;
            var compilation = existingCompilation.AddSyntaxTrees(
                CSharpSyntaxTree.ParseText(
                    SourceText.From(AttributeCode, Encoding.UTF8),
                    options));

            var attributeSymbol = compilation.GetTypeByMetadataName(AttributeName);
            var symbols = new List<(INamedTypeSymbol Type, IList<string> Namespaces)>();
            foreach (var candidateClass in nameReferenceSyntaxReceiver.CandidateClasses)
            {
                var model = compilation.GetSemanticModel(candidateClass.SyntaxTree);
                var typeSymbol = (INamedTypeSymbol) model.GetDeclaredSymbol(candidateClass);
                var relevantAttribute = typeSymbol!
                    .GetAttributes()
                    .FirstOrDefault(attr => attr.AttributeClass!.Equals(attributeSymbol, SymbolEqualityComparer.Default));

                if (relevantAttribute != null)
                {
                    var additionalNamespaces = new List<string>();
                    if (relevantAttribute.NamedArguments.Any(kvp => kvp.Key == "AdditionalNamespaces"))
                    {
                        additionalNamespaces = relevantAttribute
                            .NamedArguments
                            .First(kvp => kvp.Key == "AdditionalNamespaces")
                            .Value.Values
                            .Where(constant => !constant.IsNull)
                            .Select(constant => constant.Value!.ToString())
                            .ToList();
                    }
                    
                    symbols.Add((typeSymbol, additionalNamespaces));
                }
            }

            return symbols;
        }
    }
}