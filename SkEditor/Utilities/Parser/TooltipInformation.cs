using Avalonia.Controls;
using SkEditor.Utilities.InternalAPI;

namespace SkEditor.Utilities.Parser;

/// <summary>
/// Repersent an information about a tooltip, added
/// by an element to a <see cref="FileParser"/> instance.
/// </summary>
/// <param name="Tooltip">The tooltip control to display.</param>
/// <param name="StartingIndex">The starting index of the tooltip in the editor.</param>
/// <param name="Length">The length of the tooltip in the editor.</param>
public record TooltipInformation(Control Tooltip, int Line, int StartingIndex = 0, int Length = -1);