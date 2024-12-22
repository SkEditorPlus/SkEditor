using AvaloniaEdit;
using AvaloniaEdit.Document;
using Serilog;
using SkEditor.API;
using SkEditor.Utilities.Files;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SkEditor.Utilities.Parser;
public partial class SkDocParser
{
    public record Function(int Line, string Name, string? ReturnType, List<FunctionParameter> Parameters, SkDocComment? Comment);
    public record FunctionParameter(string Name, string Type);
    public record SkDocComment(List<SkDocParameter> Parameters, string? Return, string? Description);
    public record SkDocParameter(string Name, string? Description);

    public static Dictionary<CodeParser, List<Function>> SkDocFunctions { get; } = [];

    private static readonly Regex _functionRegex = FunctionRegex();
    private static readonly Regex _commentBlockRegex = CommentBlockRegex();
    private static readonly Regex _commentParamRegex = SkDocParamRegex();
    private static readonly Regex _commentReturnsRegex = SkDocReturnsRegex();

    public static void Parse(CodeParser parser, List<CodeSection> sections)
    {
        SkDocFunctions.Clear();

        sections = sections.Where(x => x.Type == CodeSection.SectionType.Function).ToList();

        foreach (var section in sections)
        {
            int line = section.StartingLineIndex + 1;
            DocumentLine docLine = parser.Editor.Document.GetLineByNumber(line);
            string lineText = parser.Editor.Document.GetText(docLine);

            if (!_functionRegex.IsMatch(lineText)) continue;

            Match match = _functionRegex.Match(lineText);

            string name = match.Groups["name"].Value;
            string parameters = match.Groups["parameters"].Value;
            string returnType = match.Groups["returnType"].Value;
            List<FunctionParameter> parametersList = [];

            if (!string.IsNullOrEmpty(parameters))
            {
                string[] parametersArray = parameters.Split(',');

                foreach (var parameter in parametersArray)
                {
                    string[] parameterParts = parameter.Trim().Split(' ');
                    string parameterType = parameterParts[^1];
                    string parameterName = parameterParts[^2];
                    parametersList.Add(new(parameterName, parameterType));
                }
            }

            SkDocComment? comment = GetComment(parser, line);

            Function function = new(line, name, returnType, parametersList, comment);

            if (!SkDocFunctions.TryGetValue(parser, out List<Function>? value))
            {
                value = [];
                SkDocFunctions[parser] = value;
            }

            value.Add(function);
        }
    }

    private static SkDocComment? GetComment(CodeParser parser, int line)
    {
        if (line == 0) return null;

        SkDocComment? comment = null;
        DocumentLine? commentEndLine = parser.Editor.Document.GetLineByNumber(line).PreviousLine;
        if (commentEndLine == null) return null;
        string lineText = parser.Editor.Document.GetText(commentEndLine);

        if (!_commentBlockRegex.IsMatch(lineText)) return null;

        DocumentLine commentStartLine = commentEndLine;
        while (commentStartLine.LineNumber > 1)
        {
            commentStartLine = commentStartLine.PreviousLine;
            lineText = parser.Editor.Document.GetText(commentStartLine);

            if (_commentBlockRegex.IsMatch(lineText)) break;
        }

        List<SkDocParameter> parameters = [];
        string? returns = null;
        string? description = null;

        for (int i = commentStartLine.LineNumber; i <= commentEndLine.LineNumber; i++)
        {
            DocumentLine commentLine = parser.Editor.Document.GetLineByNumber(i);
            string commentLineText = parser.Editor.Document.GetText(commentLine);

            if (_commentParamRegex.IsMatch(commentLineText))
            {
                Match match = _commentParamRegex.Match(commentLineText);
                string paramName = match.Groups["name"].Value;
                string paramDescription = match.Groups["description"].Value;
                parameters.Add(new(paramName, paramDescription));
            }
            else if (_commentReturnsRegex.IsMatch(commentLineText))
            {
                Match match = _commentReturnsRegex.Match(commentLineText);
                returns = match.Groups["description"].Value;
            }
            else if (commentLineText.Trim().StartsWith('#') && !commentLineText.Trim().StartsWith("###"))
            {
                description ??= commentLineText.Trim('#').Trim();
            }
        }

        if (parameters.Count > 0 || returns != null || description != null)
        {
            comment = new(parameters, returns, description);
        }

        return comment;
    }

    public static Function? GetFunction(TextEditor editor, int line)
    {
        var file = SkEditorAPI.Files.GetOpenedEditors().FirstOrDefault(x => x.Editor == editor);
        if (file == null)
            return null;

        CodeParser parser = file.Parser;

        if (!SkDocFunctions.TryGetValue(parser, out var functions))
            return null;

        return functions.FirstOrDefault(f => f.Line == line);
    }

    public static Function? GetFunctionFromCall(TextEditor editor, SimpleSegment segment)
    {
        int line = editor.Document.GetLineByOffset(segment.Offset).LineNumber;
        DocumentLine documentLine = editor.Document.GetLineByNumber(line);
        string text = editor.Document.GetText(documentLine);

        var match = FunctionCallRegex().Match(text);
        if (!match.Success) return null;

        string name = match.Groups["name"].Value;
        if (!editor.Document.GetText(segment).Equals(name)) return null;

        var file = SkEditorAPI.Files.GetOpenedEditors().FirstOrDefault(x => x.Editor == editor);
        if (file == null)
            return null;

        CodeParser parser = file.Parser;

        if (!SkDocFunctions.TryGetValue(parser, out var functions))
            return null;

        return functions.FirstOrDefault(f => f.Name == name);
    }

    [GeneratedRegex(@"(^function|^local function) (?<name>\w+)\((?<parameters>[^)]*)\)(?: :: (?<returnType>\w+))?:")]
    private static partial Regex FunctionRegex();

    [GeneratedRegex(@"\b(?<name>\w+)\((?<parameters>[^)]*)\)")]
    private static partial Regex FunctionCallRegex();
    [GeneratedRegex(@"^###\s*$", RegexOptions.Compiled)]
    private static partial Regex CommentBlockRegex();
    [GeneratedRegex(@"#\s*@param\s+(?<name>\w+)\s+(?<description>.+)", RegexOptions.Compiled)]
    private static partial Regex SkDocParamRegex();
    [GeneratedRegex(@"#\s*@returns\s+(?<description>.+)", RegexOptions.Compiled)]
    private static partial Regex SkDocReturnsRegex();
}
