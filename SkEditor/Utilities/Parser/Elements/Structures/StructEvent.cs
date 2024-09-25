using FluentAvalonia.UI.Controls;
using SkEditor.Parser;
using SkEditor.Parser.Elements;
using SkEditor.Utilities;
using Symbol = FluentIcons.Common.Symbol;
using SymbolIconSource = FluentIcons.Avalonia.Fluent.SymbolIconSource;

namespace SkEditor.Parser.Elements;

public class StructEvent : Element
{
    public override void Load(Node node, ParsingContext context)
    {
        /*var section = (SectionNode) node;
        ElementParser.ParseNodes(section.Children, context);*/
    }

    public static bool Parse(Node node)
    {
        return node is { IsSection: true, Indent: 0 };
    }

    public override IconSource? IconSource => new SymbolIconSource() { Symbol = Symbol.Timer };
    
    public override string DisplayString => Translation.Get("CodeParserFilterTypeEvents");
}