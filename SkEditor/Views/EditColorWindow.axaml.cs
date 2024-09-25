using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Windowing;
using SkEditor.Utilities.InternalAPI;
using SkEditor.Utilities.Parser.Elements;
using SkEditor.Views.Generators.Gui;

namespace SkEditor.Views;

public partial class EditColorWindow : AppWindow
{
    public FileParser Parser { get; set; }
    public ExprProviderElement.SkriptColor Color { get; set; }
    
    public EditColorWindow(FileParser parser, ExprProviderElement.SkriptColor color)
    {
        InitializeComponent();
        
        Parser = parser;
        Color = color;
        
        Picker.Color = new Color(Color.Color.A, Color.Color.R, Color.Color.G, Color.Color.B);
        Picker.IsAlphaVisible = false;
        Picker.Palette = new MinecraftColorPalette();
        Picker.ColorChanged += (sender, args) => UpdateColorData(args);

        ColorTypeComboBox.SelectedIndex = (int) color.Type;
        
        AssignCommands();
    }

    public void AssignCommands()
    {
        ChangeButton.Command = new RelayCommand(() =>
        {
            var color = Picker.Color;
            var tag = (ColorTypeComboBox.SelectedItem as ComboBoxItem)!.Tag as string;
            var toReplace = "error: unable to parse color tag";
            
            if (tag == "hex")
            {
                toReplace = $"<##{color.R:X2}{color.G:X2}{color.B:X2}>";
            } else if (tag is "minecraft" or "tag")
            {
                var code = ColoredTextHandler.TextFormats.FirstOrDefault(x => x.Value is Color c && c == color).Key;
                if (tag == "minecraft")
                {
                    toReplace = "&" + code;
                }
                else
                {
                    var colorTag = ColoredTextHandler.TagFormats.FirstOrDefault(x => 
                        x.Value is Color c && c == color).Key;
                    
                    if (colorTag != null)
                    {
                        toReplace = "<" + colorTag + ">";
                    } else {
                        toReplace = "<" + code + ">";
                    }
                }
            }
            
            Parser.ReplaceReference(Color, toReplace);
            Close();
        });
        CancelButton.Command = new RelayCommand(Close);
    }
    
    public void UpdateColorData(ColorChangedEventArgs args)
    {
        var color = args.NewColor;
        bool isCustom = false;
        foreach (var pair in ColoredTextHandler.TextFormats)
        {
            if (pair.Value is Color c && c == color)
            {
                isCustom = false;
                break;
            }
            isCustom = true;
        }

        foreach (var child in ColorTypeComboBox.Items)
        {
            var item = (ComboBoxItem) child;
            var tag = (item.Tag as string)!;
            
            if (tag is "minecraft" or "tag" && isCustom)
                item.IsEnabled = false;
            else 
                item.IsEnabled = true;
        }

        if (isCustom) ColorTypeComboBox.SelectedIndex = 0;
    }

    public class MinecraftColorPalette : IColorPalette
    {
        public Color GetColor(int colorIndex, int shadeIndex)
        {
            var i = 0;
            foreach (var c in ColoredTextHandler.TextFormats)
            {
                if (i == colorIndex)
                    return (Color) c.Value;
                i++;
            }
            
            return Colors.Brown;
        }

        public int ColorCount => 16;
        public int ShadeCount => 1;
    }
}