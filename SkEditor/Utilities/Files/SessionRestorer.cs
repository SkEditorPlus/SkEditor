using Newtonsoft.Json.Linq;
using Serilog;
using SkEditor.API;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using SkEditor.Utilities.Syntax;

namespace SkEditor.Utilities.Files;
public static class SessionRestorer
{
    private static readonly string SessionFolder = Path.Combine(Path.GetTempPath(), "SkEditor", "Session");

    public static async Task SaveSession()
    {
        if (Directory.Exists(SessionFolder)) Directory.Delete(SessionFolder, true);
        Directory.CreateDirectory(SessionFolder);

        var openedFiles = SkEditorAPI.Files.GetOpenedFiles();
        var index = 0;
        foreach (var openedFile in openedFiles)
        {
            if (!openedFile.IsEditor || string.IsNullOrEmpty(openedFile.Editor.Text))
            {
                continue;
            }

            var jsonData = BuildSavingData(openedFile);
            var compressed = await Compress(jsonData);
            var path = Path.Combine(SessionFolder, $"file_{index}.skeditor");
            await File.WriteAllTextAsync(path, compressed);
            index++;
        }
    }

    public static async Task<bool> RestoreSession()
    {
        if (!Directory.Exists(SessionFolder))
            return false;

        var files = Directory.GetFiles(SessionFolder);
        if (files.Length == 0)
            return false;

        foreach (var file in files)
        {
            var compressed = await File.ReadAllTextAsync(file);
            var jsonData = await Decompress(compressed);
            if (string.IsNullOrEmpty(jsonData)) continue;

            var data = BuildOpeningData(jsonData);
            await SkEditorAPI.Files.AddEditorTab(data.Content, data.Path);

            if (data.HasUnsavedChanges)
            {
                var openedFile = SkEditorAPI.Files.GetOpenedFileByPath(data.Path);
                if (openedFile != null)
                {
                    openedFile.IsSaved = false;
                }
            }
        }

        return true;
    }

    #region Compressing/Decompressing

    private static async Task<string> Compress(string data)
    {
        var byteArray = Encoding.UTF8.GetBytes(data);
        using var ms = new MemoryStream();
        await using (var sw = new GZipStream(ms, CompressionMode.Compress))
            sw.Write(byteArray, 0, byteArray.Length);
        return Convert.ToBase64String(ms.ToArray());
    }

    private static async Task<string> Decompress(string data)
    {
        string result = string.Empty;
        try
        {
            var byteArray = Convert.FromBase64String(data);
            using var ms = new MemoryStream(byteArray);
            await using var sr = new GZipStream(ms, CompressionMode.Decompress);
            using var reader = new StreamReader(sr);
            result = await reader.ReadToEndAsync();
        }
        catch (FormatException e)
        {
            Log.Warning(e, "Error while decompressing data");
        }
        return result;
    }

    #endregion

    #region Serialization/Deserialization

    private static string BuildSavingData(OpenedFile openedFile)
    {
        var obj = new JObject
        {
            ["Path"] = openedFile.Path,
            ["Content"] = openedFile.Editor.Text,
            ["HasUnsavedChanges"] = !openedFile.IsSaved
        };

        return obj.ToString();
    }

    private static (string Content, string? Path, bool HasUnsavedChanges) BuildOpeningData(string data)
    {
        var obj = JObject.Parse(data);

        var path = obj["Path"]?.Value<string>();
        var content = obj["Content"]?.Value<string>();
        var hasUnsavedChanges = obj["HasUnsavedChanges"]?.Value<bool>() ?? false;

        if (string.IsNullOrEmpty(content) && !string.IsNullOrEmpty(path))
        {
            content = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }

        return (content, path, hasUnsavedChanges);
    }

    #endregion
}