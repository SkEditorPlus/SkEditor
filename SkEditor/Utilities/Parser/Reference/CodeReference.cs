using SkEditor.Parser;

namespace SkEditor.Utilities.Parser;

/// <summary>
/// Represent a reference to a node in the code,
/// with linked position and length. This is for instance
/// a variable, option or color.
/// </summary>
/// <param name="Node">The node that this reference belongs to</param>
/// <param name="Position">The start position of the reference (within the node)</param>
/// <param name="Length">The length of the reference</param>
public record CodeReference(Node Node, int Position, int Length);