using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using SkEditor.Utilities.Parser;
using SkEditor.Utilities.Styling;

namespace SkEditor.Controls;

public partial class FunctionTooltip : UserControl, INotifyPropertyChanged
{
    public FunctionTooltip(SkDocParser.Function skDocFunction)
    {
        InitializeComponent();
        DataContext = this;

        FunctionContentPanel.Children.Add(BuildFunctionContent(skDocFunction));

        var comment = skDocFunction.Comment;
        if (comment == null)
        {
            return;
        }
        BuildFromComment(comment);
    }

    private void BuildFromComment(SkDocParser.SkDocComment? comment)
    {
        Separator separator = new()
        {
            Margin = new Thickness(0, 5, 0, 5)
        };
        TooltipPanel.Children.Add(separator);

        if (comment.Description != null)
        {
            SelectableTextBlock descriptionTextBlock = CreateSelectableTextBlock(comment.Description, "#adbcc3", 12);
            descriptionTextBlock.MaxWidth = 300;
            descriptionTextBlock.TextWrapping = TextWrapping.Wrap;
            descriptionTextBlock.SelectionBrush = ThemeEditor.CurrentTheme.SelectionColor;
            descriptionTextBlock.ContextFlyout = null;
            TooltipPanel.Children.Add(descriptionTextBlock);
        }

        if (comment.Parameters.Count > 0)
        {
            StackPanel paramsPanel = new()
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0),
                Spacing = 10
            };

            TextBlock textBlock = CreateTextBlock("Params:", "#7d7d7d", 12);
            paramsPanel.Children.Add(textBlock);

            StackPanel parametersPanel = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 3
            };

            foreach (var parameter in comment.Parameters)
            {
                StackPanel parameterPanel = new()
                {
                    Orientation = Orientation.Horizontal
                };

                SelectableTextBlock parameterName = CreateSelectableTextBlock(parameter.Name, "#fdc272", 12, fontStyle: FontStyle.Italic);
                SelectableTextBlock parameterDescription = CreateSelectableTextBlock(" - " + parameter.Description, "#adbcc3", 12);

                parameterPanel.Children.Add(parameterName);
                parameterPanel.Children.Add(parameterDescription);
                parametersPanel.Children.Add(parameterPanel);
            }

            paramsPanel.Children.Add(parametersPanel);
            TooltipPanel.Children.Add(paramsPanel);
        }

        if (comment.Return != null)
        {
            StackPanel returnsPanel = new()
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0),
                Spacing = 10
            };

            TextBlock textBlock = CreateTextBlock("Returns:", "#7d7d7d", 12);
            SelectableTextBlock selectableTextBlock = new()
            {
                FontFamily = new FontFamily("JetBrains Mono"),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#adbcc3")),
                Text = comment.Return,
                SelectionBrush = ThemeEditor.CurrentTheme.SelectionColor,
                ContextFlyout = null
            };

            returnsPanel.Children.Add(textBlock);
            returnsPanel.Children.Add(selectableTextBlock);
            TooltipPanel.Children.Add(returnsPanel);
        }
    }

    private static TextBlock CreateTextBlock(string text, string color, double fontSize = 13)
    {
        return new TextBlock
        {
            FontFamily = new FontFamily("JetBrains Mono"),
            FontSize = fontSize,
            Foreground = new SolidColorBrush(Color.Parse(color)),
            FontWeight = FontWeight.Bold,
            MaxWidth = 300,
            Text = text
        };
    }

    private static SelectableTextBlock CreateSelectableTextBlock(string text, string color, double fontSize = 12,
        FontWeight fontWeight = FontWeight.Normal, FontStyle fontStyle = FontStyle.Normal)
    {
        return new SelectableTextBlock
        {
            FontFamily = new FontFamily("JetBrains Mono"),
            FontSize = fontSize,
            Foreground = new SolidColorBrush(Color.Parse(color)),
            FontWeight = fontWeight,
            FontStyle = fontStyle,
            MaxWidth = 300,
            Text = text,
            SelectionBrush = ThemeEditor.CurrentTheme.SelectionColor,
            ContextFlyout = null
        };
    }

    public static StackPanel BuildFunctionContent(SkDocParser.Function skDocFunction)
    {
        var functionContentPanel = new StackPanel { Orientation = Orientation.Vertical };

        var headerStackPanel = new StackPanel { Orientation = Orientation.Horizontal };
        headerStackPanel.Children.Add(CreateTextBlock("function", "#ff6542"));
        headerStackPanel.Children.Add(CreateTextBlock(" " + skDocFunction.Name, "#90dbf5"));
        headerStackPanel.Children.Add(CreateTextBlock("(", "#cdcaca"));
        functionContentPanel.Children.Add(headerStackPanel);

        int maxParamNameLength = skDocFunction.Parameters.Max(p => p.Name.Length);
        foreach (var parameter in skDocFunction.Parameters)
        {
            var parameterStackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            string alignedParameterName = parameter.Name;
            alignedParameterName = alignedParameterName.PadRight(maxParamNameLength + 1);
            parameterStackPanel.Children.Add(CreateTextBlock("    " + alignedParameterName, "#d9ad69"));
            parameterStackPanel.Children.Add(CreateTextBlock(parameter.Type, "#54d7a9"));
            functionContentPanel.Children.Add(parameterStackPanel);
        }

        var footerStackPanel = new StackPanel { Orientation = Orientation.Horizontal };
        footerStackPanel.Children.Add(CreateTextBlock(")", "#cdcaca"));
        if (!string.IsNullOrEmpty(skDocFunction.ReturnType))
        {
            footerStackPanel.Children.Add(CreateTextBlock(" ::", "#cdcaca"));
            footerStackPanel.Children.Add(CreateTextBlock(" " + skDocFunction.ReturnType, "#54d7a9"));
        }
        functionContentPanel.Children.Add(footerStackPanel);

        return functionContentPanel;
    }
}