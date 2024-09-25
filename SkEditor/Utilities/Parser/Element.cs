using FluentAvalonia.UI.Controls;
using SkEditor.API;
using SkEditor.Utilities.Parser;

namespace SkEditor.Parser;

/// <summary>
/// Represent an element that can be parsed
/// from a <see cref="Node"/> to a <see cref="Element"/>.
///
/// It must have a <b>static Parse</b> method that takes a <see cref="Node"/>
/// and returns a boolean indicating if the parsing was successful/is possible.
///
/// For any other "loading" logic, it should be done in the Load method.
/// </summary>
public abstract class Element
{
    public static readonly ParserWarning UnknownElement 
        = new ("unknown_element", "Can't understand this element.");
    
    /// <summary>
    /// Load the element's data from a <see cref="Node"/>.
    /// Type of node should be checked in the Parse method instead.
    /// </summary>
    /// <param name="node">The node to load the data from.</param>
    /// <param name="context">The parsing context.</param>
    public abstract void Load(Node node, ParsingContext context);
    
    /// <summary>
    /// Debug this element for logging purposes.
    /// </summary>
    /// <returns>The debug string, that should contains information about the element.</returns>
    public virtual string Debug() => GetType().Name;
    
    /// <summary>
    /// Create a display string for this section, mainly used
    /// in code folding.
    /// </summary>
    /// <returns>The display string for this section.</returns>
    public virtual string? SectionDisplay() => null;
    
    /// <summary>
    /// Get the icon source for this element.
    /// Thise will only be used when that element is considered as a structure
    /// (e.g. a function, an event, a command, etc.) to be displayed in the
    /// parser sidebar results.
    /// </summary>
    public virtual IconSource? IconSource => null;
    
    /// <summary>
    /// Get the display string for this element.
    /// </summary>
    public virtual string DisplayString => GetType().Name;
}