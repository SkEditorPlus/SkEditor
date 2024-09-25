using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using System.Collections.Generic;

namespace SkEditor.Views.Generators.Gui;

/// <summary>
/// Class to handle colored text in the GUI.
/// </summary>
public static class ColoredTextHandler
{
    public static readonly Dictionary<string, object> TextFormats = new()
    {
        { "0", Color.FromRgb(0, 0, 0) },
        { "1", Color.FromRgb(0, 0, 170) },
        { "2", Color.FromRgb(0, 170, 0) },
        { "3", Color.FromRgb(0, 170, 170) },
        { "4", Color.FromRgb(170, 0, 0) },
        { "5", Color.FromRgb(170, 0, 170) },
        { "6", Color.FromRgb(255, 170, 0) },
        { "7", Color.FromRgb(170, 170, 170) },
        { "8", Color.FromRgb(85, 85, 85) },
        { "9", Color.FromRgb(85, 85, 255) },
        { "a", Color.FromRgb(85, 255, 85) },
        { "b", Color.FromRgb(85, 255, 255) },
        { "c", Color.FromRgb(255, 85, 85) },
        { "d", Color.FromRgb(255, 85, 255) },
        { "e", Color.FromRgb(255, 255, 85) },
        { "f", Color.FromRgb(255, 255, 255) },
        { "l", FontWeight.Bold },
        { "o", FontStyle.Italic },
        { "n", TextDecorations.Underline },
        { "m", TextDecorations.Strikethrough },
        { "r", Color.FromRgb(255, 255, 255) }
    };
    
    public static readonly Dictionary<string, object> TagFormats = new()
    {
        { "black", Color.FromRgb(0, 0, 0) },
        { "dark_blue", Color.FromRgb(0, 0, 170) },
        { "dark_green", Color.FromRgb(0, 170, 0) },
        { "dark_aqua", Color.FromRgb(0, 170, 170) },
        { "dark_red", Color.FromRgb(170, 0, 0) },
        { "dark_purple", Color.FromRgb(170, 0, 170) },
        { "gold", Color.FromRgb(255, 170, 0) },
        { "gray", Color.FromRgb(170, 170, 170) },
        { "dark_gray", Color.FromRgb(85, 85, 85) },
        { "blue", Color.FromRgb(85, 85, 255) },
        { "green", Color.FromRgb(85, 255, 85) },
        { "aqua", Color.FromRgb(85, 255, 255) },
        { "red", Color.FromRgb(255, 85, 85) },
        { "light_purple", Color.FromRgb(255, 85, 255) },
        { "yellow", Color.FromRgb(255, 255, 85) },
        { "white", Color.FromRgb(255, 255, 255) },
        { "bold", FontWeight.Bold },
        { "italic", FontStyle.Italic },
        { "underline", TextDecorations.Underline },
        { "strikethrough", TextDecorations.Strikethrough },
        { "reset", Color.FromRgb(255, 255, 255) }
    };
    
    private struct FormattedText
    {
        public string Text { get; set; }
        public Color Color { get; set; }
        public FontWeight FontWeight { get; set; }
        public FontStyle FontStyle { get; set; }
        public List<TextDecoration> TextDecorations { get; set; }
    }

    public static void SetupBox(TextBox textBox)
    {
        var flyout = new Flyout { Placement = PlacementMode.BottomEdgeAlignedLeft };
        FlyoutBase.SetAttachedFlyout(textBox, flyout);

        textBox.TextChanged += (source, args) =>
        {
            var text = textBox.Text;
            List<FormattedText> formattedTexts = ParseFormattedText(text);
            var panel = new StackPanel() { Orientation = Orientation.Horizontal };
            foreach (var formattedText in formattedTexts)
            {
                var block = new TextBlock
                {
                    Text = formattedText.Text,
                    Foreground = new SolidColorBrush(formattedText.Color),
                    FontWeight = formattedText.FontWeight,
                    FontStyle = formattedText.FontStyle,
                    TextDecorations = new TextDecorationCollection(formattedText.TextDecorations),
                    FontFamily = GetMinecraftFont()
                };

                panel.Children.Add(block);
            }

            flyout.Content = panel;
            FlyoutBase.ShowAttachedFlyout(textBox);
        };
    }

    private static List<FormattedText> ParseFormattedText(string text)
    {
        List<FormattedText> formattedTexts = new();
        FormattedText currentFormattedText = new()
        {
            Text = "",
            Color = (Color)TextFormats["5"],
            FontWeight = FontWeight.Normal,
            FontStyle = FontStyle.Italic,
            TextDecorations = new List<TextDecoration>()
        };

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '&' && i + 1 < text.Length)
            {
                char code = text[i + 1];
                if (TextFormats.ContainsKey(code.ToString()))
                {
                    // If the current formatted text has any text, add it to the list
                    if (currentFormattedText.Text.Length > 0)
                    {
                        formattedTexts.Add(currentFormattedText);
                    }

                    // Start a new formatted text with the new format
                    currentFormattedText = currentFormattedText with
                    {
                        Text = "",
                        TextDecorations = [.. currentFormattedText.TextDecorations]
                    };

                    switch (TextFormats[code.ToString()])
                    {
                        case Color color:
                            currentFormattedText.Color = color;
                            // Reset style and weight when a color code is encountered
                            currentFormattedText.FontWeight = FontWeight.Normal;
                            currentFormattedText.FontStyle = FontStyle.Normal;
                            currentFormattedText.TextDecorations.Clear();
                            break;
                        case FontWeight fontWeight:
                            currentFormattedText.FontWeight = fontWeight;
                            break;
                        case FontStyle fontStyle:
                            currentFormattedText.FontStyle = fontStyle;
                            break;
                        case TextDecoration textDecoration:
                            currentFormattedText.TextDecorations.Add(textDecoration);
                            break;
                    }

                    // Reset all styles to default when '&r' code is encountered
                    if (code == 'r')
                    {
                        currentFormattedText.Color = (Color)TextFormats["f"];
                        currentFormattedText.FontWeight = FontWeight.Normal;
                        currentFormattedText.FontStyle = FontStyle.Normal;
                        currentFormattedText.TextDecorations.Clear();
                    }

                    // Skip the next character as it is the format code
                    i++;
                }
                else
                {
                    // If the code is not recognized, treat it as normal text
                    currentFormattedText.Text += text[i];
                }
            }
            else
            {
                // If the character is not a format code, add it to the current text
                currentFormattedText.Text += text[i];
            }
        }

        // Add the last formatted text to the list
        if (currentFormattedText.Text.Length > 0)
        {
            formattedTexts.Add(currentFormattedText);
        }

        return formattedTexts;
    }

    private static FontFamily _cachedFont = null!;

    private static FontFamily GetMinecraftFont()
    {
        if (_cachedFont != null!)
            return _cachedFont;

        Application.Current.TryGetResource("MinecraftFont", ThemeVariant.Default, out var font);
        _cachedFont = (FontFamily)font;
        return _cachedFont;
    }

}