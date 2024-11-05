using System.Collections.Generic;
using System.Linq;
using Avalonia.NameGenerator.VisualBasic.Domain;
using XamlX.TypeSystem;

namespace Avalonia.NameGenerator.VisualBasic.Generator;

internal class OnlyPropertiesCodeGenerator : ICodeGenerator
{
    public string GenerateCode(string className, string nameSpace, IXamlType xamlType, IEnumerable<ResolvedName> names)
    {
        var namedControls = names
            .Select(info => "        " +
                            $"{info.FieldModifier} {info.TypeName} {info.Name} => " +
                            $"this.FindNameScope()?.Find<{info.TypeName}>(\"{info.Name}\");")
            .ToList();
        var lines = string.Join("\n", namedControls);
        return $@"// <auto-generated />

using Avalonia.Controls;

namespace {nameSpace}
{{
    partial class {className}
    {{
{lines}
    }}
}}
";
    }
}