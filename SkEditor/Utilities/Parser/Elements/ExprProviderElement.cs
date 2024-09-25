using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using SkEditor.API;
using SkEditor.Parser;
using SkEditor.Parser.Elements;
using SkEditor.Views.Generators.Gui;

namespace SkEditor.Utilities.Parser.Elements;

/// <summary>
/// Represent an element that can have
/// expressions such as variables or options.
/// 
/// This is meant to centralize the logic
/// of parsing those expressions in a single place.
/// </summary>
public abstract partial class ExprProviderElement : Element
{
    public List<Variable> Variables { get; } = new();
    public List<Option> Options { get; } = new();
    public List<SkriptColor> Colors { get; } = new();
    
    public List<NameableReference> GetReferences()
    {
        var references = new List<NameableReference>();
        references.AddRange(Variables);
        //references.AddRange(Options);
        // references.AddRange(Colors); they cannot be refactored
        return references;
    }

    /// <summary>
    /// Parse a given input string to extract expressions.
    /// </summary>
    /// <param name="input">The input string to parse.</param>
    /// <param name="context">The parsing context.</param>
    /// <returns>The parsed input string.</returns>
    /// <remarks>
    /// This method should be called in the Load method
    /// of the derived class.
    /// </remarks>
    protected void ParseExpressions(string input, ParsingContext context)
    {
        if (context.Debug) 
            SkEditorAPI.Logs.Debug($"Parsing expressions for '{input}'");
        
        // Parse variables
        foreach (Match match in VariableRegex().Matches(input))
        {
            var name = match.Groups["name"].Value;
            var type = name[0] switch
            {
                '_' => VariableType.Local,
                '-' => VariableType.Memory,
                _ => VariableType.Global
            };
            Variables.Add(new Variable(name.TrimStart('_', '-'), type, RenameVariable, context.CurrentNode, match.Index, match.Length));
        }
        
        // Parse options
        foreach (Match match in OptionRegex().Matches(input))
        {
            var key = match.Groups["key"].Value;
            Options.Add(new Option(key.TrimStart('@'), context.CurrentNode, match.Index, match.Length));
            context.Tooltips.Add(new TooltipInformation(new TextBlock { Text = "Hello there: " + key }, 
                context.CurrentNode.Line, match.Index, match.Length));
        }
        
        // Parse colors
        ParseColors(ref input, context);

        if (context.Debug)
        {
            SkEditorAPI.Logs.Debug($"Parsed colors: {string.Join(", ", Colors.Select(c => c.Color + " as " + c.Type))}");
        }
    }

    public void RenameVariable((NameableReference, string) pair)
    {
        var (reference, newName) = pair;
        if (reference is not Variable variable)
            return;
        
        // TODO: Implement renaming of variables
    }
    
    /// <summary>
    /// Parse colors in the input string.
    /// </summary>
    /// <param name="input">The input string to parse.</param>
    private void ParseColors(ref string input, ParsingContext context)
    {
        // Parse hex colors
        foreach (Match match in HexColorRegex().Matches(input))
        {
            var hex = match.Groups["hex"].Value;
            var color = ColorTranslator.FromHtml($"#{hex}");
            Colors.Add(new SkriptColor(ColorType.Hex, color, context.CurrentNode, 
                match.Index, match.Length));
        }
        
        // Parse tag colors
        foreach (Match match in TagColorRegex().Matches(input))
        {
            var tag = match.Groups["tag"].Value;
            var color = GetColor(tag);
            Colors.Add(new SkriptColor(ColorType.Tag, color, context.CurrentNode, 
                match.Index, match.Length));
        }
        
        // Parse code colors
        foreach (Match match in CodeColorRegex().Matches(input))
        {
            var code = match.Value;
            var color = ColoredTextHandler.TextFormats[code[1].ToString()];
            if (color is not Avalonia.Media.Color avaloniaColor)
                continue;
            
            Colors.Add(new SkriptColor(ColorType.Code, 
                Color.FromArgb(avaloniaColor.A, avaloniaColor.R, avaloniaColor.G, avaloniaColor.B), 
                context.CurrentNode, 
                match.Index, match.Length));
        }
        
        // Sort colors by position
        Colors.Sort((a, b) => a.Position.CompareTo(b.Position));
        if (context.Debug)
        {
            foreach (var color in Colors)
            {
                SkEditorAPI.Logs.Debug($"    - Found color {color.Color} at {color.Position} size {color.Length}");
            }
        }
    }
    
    public static Color GetColor(string tag)
    {
        return tag switch
        {
            "black" => Color.Black,
            "dark_blue" => Color.DarkBlue,
            "dark_green" => Color.DarkGreen,
            "dark_aqua" => Color.DarkCyan,
            "dark_red" => Color.DarkRed,
            "dark_purple" => Color.DarkMagenta,
            "gold" => Color.Gold,
            "gray" => Color.Gray,
            "dark_gray" => Color.DarkGray,
            "blue" => Color.Blue,
            "green" => Color.Green,
            "aqua" => Color.Cyan,
            "red" => Color.Red,
            "light_purple" => Color.Magenta,
            "yellow" => Color.Yellow,
            "white" => Color.White,
            _ => Color.Black
        };
    }

    #region Data Structures

    /// <summary>
    /// Represent a variable in an expression.
    /// Example:
    ///     * {_hello} is a local variable named "hello".
    ///     * {hello} is a global variable named "hello".
    ///     * {-hello} is a memory variable named "hello".
    /// </summary>
    /// <param name="Name">The name of the variable.</param>
    /// <param name="Type">The type of the variable.</param>
    public record Variable(
        string Name,
        VariableType Type, 
        Action<(NameableReference, string)> RenameAction,
        Node Node, int Position, int Length) : NameableReference(Name, RenameAction, Node, Position, Length);
    
    public delegate void RenameVariableDelegate(string newName);
    
    /// <summary>
    /// Represent an option in an expression.
    /// Example:
    ///     * {@hello} is an option named "hello".
    /// </summary>
    /// <param name="Key">The key of the option.</param>
    public record Option(string Key,
        Node Node, int Position, int Length) : CodeReference(Node, Position, Length);
    
    /// <summary>
    /// The type of variable in an expression.
    /// </summary>
    public enum VariableType
    {
        Local,
        Global,
        Memory
    }
    
    /// <summary>
    /// Represent the type of color. This defines how the color
    /// was defined in the expression.
    /// </summary>
    public enum ColorType
    {
        Hex, // example: '<#FF0000>'
        Tag, // example: '<red>' or '<dark_red>'
        Code // example: '&4' or '&c'
    }
    
    /// <summary>
    /// Represent a color in an expression.
    /// </summary>
    /// <param name="Type">How the color was defined.</param>
    /// <param name="Color">The color value itself (what it represents).</param>
    /// <param name="position">The position of the color in the expression.</param>
    public record SkriptColor(ColorType Type, Color Color, 
        Node Node, int Position, int Length) : CodeReference(Node, Position, Length);
    
    #endregion

    [GeneratedRegex(@"\{(?<name>[_a-zA-Z-][_a-zA-Z0-9]*)\}")]
    private static partial Regex VariableRegex();
    [GeneratedRegex(@"\{(?<key>@[a-zA-Z0-9]+)\}")]
    private static partial Regex OptionRegex();
    [GeneratedRegex(@"<#(#)?(?<hex>[0-9A-Fa-f]{6})>")]
    private static partial Regex HexColorRegex();
    [GeneratedRegex(@"<(?<tag>[a-zA-Z_]+)>")]
    private static partial Regex TagColorRegex();
    [GeneratedRegex(@"&[0-9A-Fa-f]")]
    private static partial Regex CodeColorRegex();

    public override string Debug()
    {
        var variables = string.Join(", ", Variables.Select(v => v.Type switch
        {
            VariableType.Local => $"_{v.Name}",
            VariableType.Memory => $"-{v.Name}",
            _ => v.Name
        }));
        
        var options = string.Join(", ", Options.Select(o => $"@{o.Key}"));
        return $"Variables: {variables}, Options: {options}";
    }
}