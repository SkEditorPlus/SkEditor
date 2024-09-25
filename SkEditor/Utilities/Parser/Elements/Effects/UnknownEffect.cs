using SkEditor.Parser;
using SkEditor.Parser.Elements;

namespace SkEditor.Utilities.Parser.Elements.Effects;

public class UnknownEffect : ExprProviderElement
{
    public override void Load(Node node, ParsingContext context)
    {
        ParseExpressions(node.Key, context);
    }
    
    public static bool Parse(Node node)
    {
        return node.IsEffect;
    }
}