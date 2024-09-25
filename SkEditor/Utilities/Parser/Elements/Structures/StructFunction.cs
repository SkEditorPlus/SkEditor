using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FluentAvalonia.UI.Controls;
using SkEditor.API;
using SkEditor.Utilities;
using Symbol = FluentIcons.Common.Symbol;
using SymbolIcon = FluentIcons.Avalonia.Fluent.SymbolIcon;
using SymbolIconSource = FluentIcons.Avalonia.Fluent.SymbolIconSource;

namespace SkEditor.Parser.Elements;

public partial class StructFunction : Element
{
    public Function InternalFunction { get; private set; }
    
    public override void Load(Node node, ParsingContext context)
    {
        var match = FunctionRegex().Match(node.Key);
        if (!match.Success)
        {
            context.Warning(node, UnknownElement);
            return;
        }

        var isLocal = match.Groups["local"].Success;
        var name = match.Groups["name"].Value;
        var returnType = match.Groups["returnType"].Value;
        var parameters = new List<FunctionParameter>();
        
        var rawParameters = match.Groups["parameters"].Value.Split(",");
        if (rawParameters.Length == 1 && string.IsNullOrWhiteSpace(rawParameters[0]))
        {
            rawParameters = [];
        }
        
        foreach (var rawParameter in rawParameters)
        {
            var parameterMatch = ParameterRegex().Match(rawParameter);
            if (!parameterMatch.Success)
            {
                context.Warning(node, UnknownElement);
                return;
            }
            
            var parameterName = parameterMatch.Groups["name"].Value;
            var parameterType = parameterMatch.Groups["type"].Value;
            var defaultValue = parameterMatch.Groups["defaultValue"].Value;
            parameters.Add(new FunctionParameter(parameterName, parameterType, defaultValue));
        }   

        InternalFunction = new Function(name, returnType, parameters, isLocal);
    }
    
    public static bool Parse(Node node)
    {
        return node is { IsTopLevel: true, IsSection: true } && (node.Key.StartsWith("local function") || node.Key.StartsWith("function"));
    }

    #region Data Structures

    /// <summary>
    /// Represent a function parameter.
    /// </summary>
    public record FunctionParameter(string Name, string Type, string? DefaultValue);
    
    /// <summary>
    /// Represent a function.
    /// </summary>
    public record Function(string Name, string? ReturnType, List<FunctionParameter> Parameters, bool IsLocal);

    #endregion
    
    #region Regex Patterns

    // function regex
    [GeneratedRegex(@"(?<local>local\s+)?function\s+(?<name>\w+)\((?<parameters>.*)\)(\s*(::|returns)\s*(?<returnType>\w+))?")]
    private static partial Regex FunctionRegex();
    
    // parameter regex
    [GeneratedRegex(@"(?<name>\w+):\s*(?<type>\w+)\s*(=\s*(?<defaultValue>[^)]+))?")]
    private static partial Regex ParameterRegex();
    
    #endregion
    
    public override string Debug()
    {
        return $"{(InternalFunction.IsLocal ? "local " : "")}function {InternalFunction.Name}({string.Join(", ", InternalFunction.Parameters)}) :: {InternalFunction.ReturnType}";
    }

    public override IconSource? IconSource => new SymbolIconSource() { Symbol = Symbol.Braces };
    
    public override string DisplayString => Translation.Get("CodeParserFilterTypeFunctions");
}