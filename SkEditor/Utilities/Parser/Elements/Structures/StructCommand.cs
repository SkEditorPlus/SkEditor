using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAvalonia.UI.Controls;
using SkEditor.API;
using SkEditor.Utilities;
using Symbol = FluentIcons.Common.Symbol;
using SymbolIconSource = FluentIcons.Avalonia.Fluent.SymbolIconSource;

namespace SkEditor.Parser.Elements;

public partial class StructCommand : Element
{
    public static readonly ParserWarning CommandAlreadyExists 
        = new("command_already_exists", "Another command with the same name already exists.");
    
    public string Name { get; set; }
    
    public List<string>? Aliases { get; set; }
    public string? Permission { get; set; }
    public string? PermissionMessage { get; set; }
    public string? Description { get; set; }
    public string? Prefix { get; set; }
    
    public string? Cooldown { get; set; }
    public string? CooldownMessage { get; set; }
    public string? CooldownBypass { get; set; }
    public string? CooldownStorage { get; set; }
    
    public string? Usage { get; set; }
    
    public static readonly Dictionary<string, string> Entries = new()
    {
        { "aliases", nameof(Aliases) },
        { "permission", nameof(Permission) },
        { "permission message", nameof(PermissionMessage) },
        { "description", nameof(Description) },
        { "prefix", nameof(Prefix) },
        { "cooldown", nameof(Cooldown) },
        { "cooldown message", nameof(CooldownMessage) },
        { "cooldown bypass", nameof(CooldownBypass) },
        { "cooldown storage", nameof(CooldownStorage) },
        { "usage", nameof(Usage) }
    };
    
    public override void Load(Node node, ParsingContext context)
    {
        var section = (SectionNode) node;

        // Parse the name
        var match = CommandRegex().Match(node.Key);
        if (!match.Success)
        {
            context.Warning(node, UnknownElement);
            return;
        }
        
        if (context.ParsedNodes.Any(n => n.Element is StructCommand c && c.Name == match.Groups[1].Value))
            context.Warning(node, CommandAlreadyExists);
        
        Name = match.Groups[1].Value;
        
        // Parse simple entries
        foreach (var entry in Entries) {
            var value = section.GetSimpleChild(entry.Key);
            if (value != null) {
                GetType().GetProperty(entry.Value)?.SetValue(this, value.Value);
                node.Element = new CommandEntry(entry.Key, value.Value, this);
            }
        }
        
        // Parse trigger
        var triggerNode = section.GetSectionChild("trigger");
        if (triggerNode != null) {
            node.Element = new CommandTrigger(this);
            ElementParser.ParseNode(triggerNode, context);
        }
    }

    public override string? SectionDisplay()
    {
        return "Command '" + Name + "'";
    }

    public static bool Parse(Node node)
    {
        return node is SectionNode
            && node.Key.StartsWith("command")
            && node.IsTopLevel;
    }

    [GeneratedRegex("(?i)^command\\s+/?(\\S+)\\s*(\\s+(.+))?$", RegexOptions.None, "fr-FR")]
    private static partial Regex CommandRegex();

    public class CommandEntry(string key, string value, StructCommand command) : Element
    {
        public string Key { get; set; } = key;
        public string Value { get; set; } = value;
        public StructCommand Command { get; set; } = command;
        
        public override void Load(Node node, ParsingContext context)
        {
            throw new NotImplementedException();
        }

        public override string? SectionDisplay()
        {
            return $"Entry '{Key}'";
        }
    }
    
    public class CommandTrigger(StructCommand command) : Element
    {
        public StructCommand Command { get; set; } = command;

        public override void Load(Node node, ParsingContext context)
        {
            throw new NotImplementedException();
        }
    }

    public override IconSource? IconSource => new SymbolIconSource() { Symbol = Symbol.Code };

    public override string DisplayString => Translation.Get("CodeParserFilterTypeCommands");
}