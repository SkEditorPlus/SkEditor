using System;

namespace SkEditor.API;

/// <summary>
/// Represent a parser element data, with an element type
/// and its priority to be parsed (0 being the highest priority).
/// </summary>
public class ParserElementData
{
    /// <summary>
    /// The type of the element.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// The priority of the element.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Creates a new instance of <see cref="ParserElementData"/>.
    /// </summary>
    /// <param name="type">The type of the element.</param>
    /// <param name="priority">The priority of the element.</param>
    public ParserElementData(Type type, int priority)
    {
        if (priority < 0) throw new ArgumentOutOfRangeException(nameof(priority), "Priority cannot be negative.");

        Type = type ?? throw new ArgumentNullException(nameof(type), "Type cannot be null.");
        Priority = priority;
    }
}