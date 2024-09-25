using System;
using SkEditor.Parser;

namespace SkEditor.Utilities.Parser;

/// <summary>
/// Represent a code reference that can be refactored,
/// such as an option or a variable.
/// </summary>
/// <param name="Name">The (current) name of the reference</param>
/// <param name="RenameAction">The action to call when renaming the reference</param>
/// <param name="Node">The node that this reference belongs to</param>
/// <param name="Position">The start position of the reference (within the node)</param>
/// <param name="Length">The length of the reference</param>
public record NameableReference(string Name, Action<(NameableReference, string)> RenameAction,
    Node Node, int Position, int Length) : CodeReference(Node, Position, Length);