using System.Text.RegularExpressions;
using SkEditor.API;
using SkEditor.Parser;
using SkEditor.Parser.Elements;

namespace SkEditor.Utilities.Parser.Elements.Effects;

public partial class EffDelay : ExprProviderElement
{
    public static readonly ParserWarning DelayFunctionWarning
        = new ("delay_function", "Cannot delay a function that returns a value.");
    
    public string Delay { get; private set; }
    
    public override void Load(Node node, ParsingContext context)
    {
        var match = DelayEffectRegex().Match(node.Key);
        if (!match.Success)
        {
            context.Warning(node, UnknownElement);
            return;
        }

        Delay = match.Groups["delay"].Value;
        
        // Check if we're in a function that returns something
        var structure = node.GetStructureNode();
        SkEditorAPI.Logs.Debug("Structure: " + structure.Key);
        if (structure.Element is StructFunction { InternalFunction.ReturnType: not null })
            context.Warning(node, DelayFunctionWarning);
    }

    public static bool Parse(Node node)
    {
        return node.IsEffect && (node.Key.StartsWith("wait") || node.Key.StartsWith("halt")); 
    }
    
    [GeneratedRegex("(wait|halt)(\\s+for)?\\s+(?<delay>.+)")]
    private static partial Regex DelayEffectRegex();
}