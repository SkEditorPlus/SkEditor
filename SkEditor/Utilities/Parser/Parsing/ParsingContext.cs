using System.Collections.Generic;
using SkEditor.API;
using SkEditor.Parser;
using SkEditor.Utilities.Parser;

namespace SkEditor.Parser;

public class ParsingContext
{
    public bool Debug { get; set; }
    public Node CurrentNode { get; set; }

    public List<(Node, ParserWarning)> Warnings { get; } = new();
    
    public List<Node> ParsedNodes { get; } = new();
    
    public Dictionary<string, object> Data { get; } = new();
    
    public List<TooltipInformation> Tooltips { get; } = new();
    
    public void Warning(Node node, ParserWarning warning)
    {
        Warnings.Add((node, warning));
    }

}