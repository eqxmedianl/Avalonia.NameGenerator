### VB `SourceGenerator` for Typed Avalonia `x:Name` References 

This is a [VB `SourceGenerator`](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/) built for generating strongly-typed references to controls with `x:Name` (or just `Name`) attributes declared in XAML (or, in `.axaml`). The source generator will look for the `xaml` (or `axaml`) file with the same name as your partial C# class that is a subclass of `Avalonia.INamed` and parses the XAML markup, finds all XAML tags with `x:Name` attributes and generates the C# code.

### Getting Started

In order to get started, just add the following package reference to the `.vbproj`-file:

```
<PackageReference Include="XamlNameReferenceGenerator.VisualBasic" Version="1.6.2" />
```

Or, if you are using [submodules](https://git-scm.com/docs/git-submodule), you can reference the generator as such:

```xml
<ItemGroup>
    <!-- Remember to ensure XAML files are included via <AdditionalFiles>,
         otherwise C# source generator won't see XAML files. -->
    <AdditionalFiles Include="**\*.xaml"/>
    <ProjectReference Include="..\Avalonia.NameGenerator.VisualBasic\Avalonia.NameGenerator.VisualBasic.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
</ItemGroup>
```

### Usage

After installing the NuGet package, declare your view class as `Partial`. Typed VB references to Avalonia controls declared in XAML files will be generated for classes referenced by the `x:Class` directive in XAML files. For example, for the following XAML markup:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="Sample.App.SignUpView">
    <TextBox x:Name="UserNameTextBox" x:FieldModifier="public" />
</Window>
```

A new VB partial class named `SignUpView` with a single `Public` property named `UserNameTextBox` of type `TextBox` will be generated in the `Sample.App` namespace. We won't see the generated file, but we'll be able to access the generated property as shown below:

```vb
Imports Avalonia.Controls;

Namespace App

    Public Partial Class SignUpView : Inherits Window

        Sub New()

            ' This method is generated. Call it before accessing any
            ' of the generated properties. The 'UserNameTextBox'
            ' property is also generated.
            InitializeComponent()
            UserNameTextBox.Text = "Joseph"

        End Sub

    End Class

End Namespace
```

### Advanced Usage

> Never keep a method named `InitializeComponent` in your code-behind view class if you are using the generator with `AvaloniaNameGeneratorBehavior` set to `InitializeComponent` (this is the default value). The private `InitializeComponent` method declared in your code-behind class hides the `InitializeComponent` method generated by `Avalonia.NameGenerator.VisualBasic`, see [Issue 69](https://github.com/AvaloniaUI/Avalonia.NameGenerator/issues/69). If you wish to use your own `InitializeComponent` method (not the generated one), set `AvaloniaNameGeneratorBehavior` to `OnlyProperties`.

The `x:Name` generator can be configured via MsBuild properties that you can put into your VB project file (`.vbproj`). Using such options, you can configure the generator behavior, the default field modifier, namespace and path filters. The generator supports the following options:

- `AvaloniaNameGeneratorBehavior`  
    Possible values: `OnlyProperties`, `InitializeComponent`  
    Default value: `InitializeComponent`  
    Determines if the generator should generate get-only properties, or the `InitializeComponent` method.

- `AvaloniaNameGeneratorDefaultFieldModifier`  
    Possible values: `Friend`, `Public`, `Private`, `Protected`  
    Default value: `Friend`  
    The default field modifier that should be used when there is no `x:FieldModifier` directive specified.

- `AvaloniaNameGeneratorFilterByPath`  
    Posssible format: `glob_pattern`, `glob_pattern;glob_pattern`  
    Default value: `*`  
    The generator will process only XAML files with paths matching the specified glob pattern(s).  
    Example: `*/Views/*View.xaml`, `*View.axaml;*Control.axaml`

- `AvaloniaNameGeneratorFilterByNamespace`  
    Posssible format: `glob_pattern`, `glob_pattern;glob_pattern`  
    Default value: `*`  
    The generator will process only XAML files with base classes' namespaces matching the specified glob pattern(s).  
    Example: `MyApp.Presentation.*`, `MyApp.Presentation.Views;MyApp.Presentation.Controls`

- `AvaloniaNameGeneratorViewFileNamingStrategy`  
    Possible values: `ClassName`, `NamespaceAndClassName`  
    Default value: `NamespaceAndClassName`  
    Determines how the automatically generated view files should be [named](https://github.com/AvaloniaUI/Avalonia.NameGenerator/issues/92).

The default values are given by:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <AvaloniaNameGeneratorBehavior>InitializeComponent</AvaloniaNameGeneratorBehavior>
        <AvaloniaNameGeneratorDefaultFieldModifier>internal</AvaloniaNameGeneratorDefaultFieldModifier>
        <AvaloniaNameGeneratorFilterByPath>*</AvaloniaNameGeneratorFilterByPath>
        <AvaloniaNameGeneratorFilterByNamespace>*</AvaloniaNameGeneratorFilterByNamespace>
        <AvaloniaNameGeneratorViewFileNamingStrategy>NamespaceAndClassName</AvaloniaNameGeneratorViewFileNamingStrategy>
    </PropertyGroup>
    <!-- ... -->
</Project>
```
