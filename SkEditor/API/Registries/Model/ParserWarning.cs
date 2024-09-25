namespace SkEditor.API;

/// <summary>
/// Represent a specific warning the parser can generate
///that is given by a specific element. 
/// </summary>
public class ParserWarning(string identifier, string message)
{
    
    /// <summary>
    /// The unique identifier of the warning.
    /// </summary>
    public string Identifier { get; } = identifier;
    
    /// <summary>
    /// The displayed message of the warning.
    /// </summary>
    public string Message { get; } = message;
    
}