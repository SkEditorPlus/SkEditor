using System.Collections.Generic;
using System.Linq;
using FluentAvalonia.UI.Controls;
using SkEditor.API;
using SkEditor.Parser;
using SkEditor.Parser.Elements;
using SkEditor.Utilities;
using Symbol = FluentIcons.Common.Symbol;
using SymbolIconSource = FluentIcons.Avalonia.Fluent.SymbolIconSource;

namespace SkEditor.Parser.Elements;

public class StructOptions : Element
{   
    public static readonly ParserWarning MultipleOptionsSections 
        = new("multiple_options_sections", "Only one options section is allowed.");
    public static readonly ParserWarning OptionNodeNotSimple 
        = new("option_node_not_simple", "Option node is not simple ('key: value').");
    
    public readonly List<OptionDefinition> Options = new();
    
    public override void Load(Node node, ParsingContext context)
    {
        var anotherOptions = context.ParsedNodes.FirstOrDefault(x => x.Element is StructOptions);
        if (anotherOptions != null && anotherOptions != node)
        {
            SkEditorAPI.Logs.Debug("Found multiple options sections.");
            context.Warning(node, MultipleOptionsSections);
        }
        
        foreach (var optionNode in node as SectionNode)
        {
            if (optionNode.IsSimple)
            {
                var simpleNode = optionNode as SimpleNode;
                var option = new OptionDefinition(simpleNode.Key, simpleNode.Value);
                
                Options.Add(option);
                simpleNode.Element = option;
            }
            else
            {
                context.Warning(optionNode, OptionNodeNotSimple);
            }
        }
    }

    public static bool Parse(Node node)
    {
        return node is { 
            IsSection: true, 
            Key: "options", 
            IsTopLevel: true 
        };
    }

    public class OptionDefinition(string key, string value) : Element
    {
        public string Key { get; private set; } = key;
        public string Value { get; private set; } = value;

        public override void Load(Node node, ParsingContext context)
        {
            throw new System.NotImplementedException("Cannot load option definition."); 
        }
    }

    public override string Debug()
    {
        return "Options[" + Options.Count + "]:" +
               string.Join("\n  - ", Options.Select(x => x.Key + ": " + x.Value));
    }

    public override IconSource? IconSource => new SymbolIconSource() { Symbol = Symbol.BracesVariable };
    
    public override string DisplayString => Translation.Get("CodeParserFilterTypeOptions");
}