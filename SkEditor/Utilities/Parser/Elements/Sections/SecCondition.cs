using System.Collections.Generic;
using System.Text.RegularExpressions;
using SkEditor.API;
using SkEditor.Utilities.Parser.Elements;

namespace SkEditor.Parser.Elements;

public class SecCondition : ExprProviderElement
{
    public static readonly ParserWarning InvalidConditionPlacement
        = new("invalid_condition_placement", "Invalid condition placement (must be after an if or else if)");
    
    public enum ConditionalType {
        Else, 
        ElseIf, 
        If
    }
    
    public static readonly Dictionary<string, (ConditionalType, bool)> ConditionalTypes = new()
    {
        {"else", (ConditionalType.Else, false)},
        {"else( parse)? if (.+)", (ConditionalType.ElseIf, true)},
        {"(parse )?if (.+)", (ConditionalType.If, true)},
        {"else( parse)? if (any|at least one( of)?)", (ConditionalType.ElseIf, false)},
        {"else( parse)? if( all)?", (ConditionalType.ElseIf, false)},
        {"(parse )?if (any|at least one( of)?)", (ConditionalType.If, false)},
        {"(parse )?if( all)?", (ConditionalType.If, false)},
    };
    
    public ConditionalType Type { get; private set; }
    public string? Condition { get; private set; }

    public override void Load(Node node, ParsingContext context) // imagine this is the hint (not selectable and all)
    {
        var key = node.Key;

        int i = -1;
        foreach (var (pattern, (type, hasInlineCondition)) in ConditionalTypes)
        {
            i++;
            if (Regex.IsMatch(key, pattern))
            {
                Type = type;
                if (hasInlineCondition)// we can get the last parsed group
                    Condition = node.Key.Split("if ")[1];
                break;
            }
        }

        if (Condition != null)
            ParseExpressions(node.Key, context);

        if (Type == ConditionalType.ElseIf || Type == ConditionalType.Else)
        {
            var previous = node.GetPreviousNode();
            if (previous == null || previous.Element is not SecCondition)
            {
                context.Warning(node, InvalidConditionPlacement);
            }
            else
            {
                var secCondition = (SecCondition) previous.Element;
                if (secCondition.Type == ConditionalType.Else)
                    context.Warning(node, InvalidConditionPlacement);
            }
            
            SkEditorAPI.Logs.Warning($"Previous: {previous?.Element?.Debug()} {previous?.Element?.GetType()}");
        }
    }

    public static bool Parse(Node node)
    {
        if (!node.IsSection)
            return false;
        
        var key = node.Key;
        foreach (var (pattern, type) in ConditionalTypes)
            if (Regex.IsMatch(key, pattern))
                return true;

        return false;
    }

    public override string Debug()
    {
        return $"Condition[{Type}]: {Condition} " + "{" + base.Debug() + "}";
    }
}