using Avalonia.NameGenerator.VisualBasic.Domain;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.NameGenerator.VisualBasic.Generator;

internal class AvaloniaNameGenerator : INameGenerator
{
    private readonly ViewFileNamingStrategy _naming;
    private readonly IGlobPattern _pathPattern;
    private readonly IGlobPattern _namespacePattern;
    private readonly IViewResolver _classes;
    private readonly INameResolver _names;
    private readonly ICodeGenerator _code;

    public AvaloniaNameGenerator(
        ViewFileNamingStrategy naming,
        IGlobPattern pathPattern,
        IGlobPattern namespacePattern,
        IViewResolver classes,
        INameResolver names,
        ICodeGenerator code)
    {
        _naming = naming;
        _pathPattern = pathPattern;
        _namespacePattern = namespacePattern;
        _classes = classes;
        _names = names;
        _code = code;
    }

    public IReadOnlyList<GeneratedPartialClass> GenerateNameReferences(IEnumerable<AdditionalText> additionalFiles)
    {
        var resolveViews =
            from file in additionalFiles.ToList()
            where (file.Path.EndsWith(".xaml") ||
                   file.Path.EndsWith(".paml") ||
                   file.Path.EndsWith(".axaml")) &&
                  _pathPattern.Matches(file.Path)
            let xaml = file.GetText()!.ToString()
            let view = _classes.ResolveView(xaml)
            where view != null && _namespacePattern.Matches(view.Namespace)
            select view;

        var query =
            from view in resolveViews
            let names = _names.ResolveNames(view.Xaml)
            let code = _code.GenerateCode(view.ClassName, view.Namespace, view.XamlType, names)
            let fileName = ResolveViewFileName(view, _naming)
            select new GeneratedPartialClass(fileName, code);

        return query.ToList();
    }

    private static string ResolveViewFileName(ResolvedView view, ViewFileNamingStrategy strategy) => strategy switch
    {
        ViewFileNamingStrategy.ClassName => $"{view.ClassName}.g.vb",
        ViewFileNamingStrategy.NamespaceAndClassName => $"{view.Namespace}.{view.ClassName}.g.vb",
        _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown naming strategy!")
    };
}