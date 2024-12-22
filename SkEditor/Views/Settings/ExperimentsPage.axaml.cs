using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using SkEditor.API;
using System.Collections.Generic;
using System.Linq;

namespace SkEditor.Views.Settings;
public partial class ExperimentsPage : UserControl
{
    private readonly List<Experiment> experiments =
    [
        new Experiment("Auto Completion", "Early prototype of auto completion, not very helpful.", "EnableAutoCompletionExperiment", "MagicWandIcon"),
        new Experiment("Projects", "Adds a sidebar for managing projects.", "EnableProjectsExperiment", "Folder"),
        new Experiment("Hex Preview", "Preview hex colors in the editor.", "EnableHexPreview", "ColorIcon"),
        new Experiment("Code Parser", "Parse code for informations. Doesn't contain error checking, see Analyzer addon instead. Requires Projects experiment.", "EnableCodeParser", "SearchIcon", Dependency: "EnableProjectsExperiment"),
        new Experiment("Real-Time Code Parser", "Automatically parses your code with every change you make. Requires Code Parser experiment.", "EnableRealtimeCodeParser", "SearchIcon", Dependency: "EnableCodeParser"),
        new Experiment("Folding", "Folding code blocks. Requires Code Parser experiment.", "EnableFolding", "FoldingIcon", Dependency: "EnableCodeParser"),
        //new Experiment("Better pairing", "Experimental better version of auto pairing.", "EnableBetterPairing", "AutoPairingIcon"),
        new Experiment("Session restoring", "Automatically saves your files and reopens it next time you start the app.", "EnableSessionRestoring", "SessionRestoringIcon"),
        new Experiment("SkDoc", "Function documentation on hover.", "EnableSkDoc", "SkDocIcon"),
    ];

    public ExperimentsPage()
    {
        InitializeComponent();
        Title.BackButton.Command = new RelayCommand(() => SettingsWindow.NavigateToPage(typeof(HomePage)));

        AddExperiments();
    }

    private void AddExperiments()
    {
        foreach (var experiment in experiments)
        {
            Application.Current.TryGetResource(experiment.Icon, Avalonia.Styling.ThemeVariant.Default, out object icon);

            ToggleSwitch toggleSwitch = new()
            {
                IsChecked = SkEditorAPI.Core.GetAppConfig().GetOptionValue<bool>(experiment.Option),
            };

            toggleSwitch.IsCheckedChanged += (sender, e) => Switch(experiment, toggleSwitch);

            SettingsExpander expander = new()
            {
                Header = experiment.Name,
                Description = experiment.Description,
                Footer = toggleSwitch,
                IconSource = icon as IconSource,
            };

            ExperimentsStackPanel.Children.Add(expander);
        }
    }

    private void Switch(Experiment experiment, ToggleSwitch toggleSwitch)
    {
        if (toggleSwitch.IsChecked.Value && experiment.Dependency is not null)
        {
            var dependency = experiments.Find(e => e.Option == experiment.Dependency);
            if (dependency is not null)
            {
                var dependencySwitch = ExperimentsStackPanel.Children.OfType<SettingsExpander>().FirstOrDefault(e => e.Header.ToString() == dependency.Name)?.Footer as ToggleSwitch;
                if (dependencySwitch is not null)
                {
                    dependencySwitch.IsChecked = true;
                }
            }
        }

        SkEditorAPI.Core.GetAppConfig().SetOptionValue(experiment.Option, toggleSwitch.IsChecked.Value);
        SkEditorAPI.Core.GetAppConfig().Save();
    }
}

public record Experiment(string Name, string Description, string Option, string Icon = "Settings", string? Dependency = null);