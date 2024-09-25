using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Windowing;
using SkEditor.Utilities;
using SkEditor.Utilities.InternalAPI;
using SkEditor.Utilities.Parser;

namespace SkEditor.Views;

public partial class SymbolRefactorWindow : AppWindow
{
    public NameableReference Reference { get; }
    public SymbolRefactorWindow(NameableReference reference)
    {
        InitializeComponent();
        Reference = reference;
        Focusable = true;

        RenameText.Text = Translation.Get("RefactorWindowRefactorBoxName",
            Reference.Name);
        NameBox.Text = Reference.Name;
        
        RefactorButton.Command = new RelayCommand(Refactor);

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) Close();
        };
    }

    private void Refactor()
    {
        Reference.RenameAction((Reference, NameBox.Text));
        Close();

        AddonLoader.GetCoreAddon().ParserPanel.Panel.ParseCurrentFile();
    }
}