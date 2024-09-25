using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SkEditor.Parser;

namespace SkEditor.Controls.Sidebar;

public partial class ParserFilterViewModel : ObservableObject
{
    [ObservableProperty] private string _searchText = "";
    
    [ObservableProperty] private int _selectedFilterIndex = 0;
    [ObservableProperty] private ObservableCollection<Element> _availableSections = [];
    
    private ObservableCollection<Node> _allSections = [];

    public ObservableCollection<Node> AllSections
    {
        get => _allSections;
        set
        {
            SetProperty(ref _allSections, value);
            FilterSections();
        }
    }

    private void FilterSections()
    {
        FilteredSections.Clear();
        if (SelectedFilterIndex == 0)
        {
            foreach (var section in AllSections)
                if (string.IsNullOrEmpty(SearchText) || section.Key.ToLower().Contains(SearchText.ToLower())) 
                    FilteredSections.Add(section);
        }
        else
        {
            foreach (var section in AllSections)
            {
                if ((string.IsNullOrEmpty(SearchText) || section.Key.ToLower().Contains(SearchText.ToLower())) ||
                    section.GetType() == AvailableSections[SelectedFilterIndex].GetType())
                {
                    FilteredSections.Add(section);
                }
            }
        }
    }

    [ObservableProperty] private ObservableCollection<Node> _filteredSections = [];
}