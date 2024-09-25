using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace SkEditor.Views.Settings;
public partial class HomePage : UserControl
{
    public StackPanel GetStackPanel() => ItemStackPanel;

    public HomePage()
    {
        InitializeComponent();

        GeneralItem.Command = new RelayCommand(() => SettingsWindow.NavigateToPage(typeof(GeneralPage)));
        PersonalizationItem.Command = new RelayCommand(() => SettingsWindow.NavigateToPage(typeof(PersonalizationPage)));
        ExperimentsItem.Command = new RelayCommand(() => SettingsWindow.NavigateToPage(typeof(ExperimentsPage)));
        AboutItem.Command = new RelayCommand(() => SettingsWindow.NavigateToPage(typeof(AboutPage)));
        AddonsItem.Command = new RelayCommand(() => SettingsWindow.NavigateToPage(typeof(AddonsPage)));
        ConnectionsItem.Command = new RelayCommand(() => SettingsWindow.NavigateToPage(typeof(ConnectionsPage)));
        FileTypesItem.Command = new RelayCommand(() => SettingsWindow.NavigateToPage(typeof(FileTypesPage)));
        ParserItem.Command = new RelayCommand(() => SettingsWindow.NavigateToPage(typeof(ParserPage)));
    }
}
