using SkEditor.Utilities;
using System;
using System.Collections.Generic;
using SkEditor.Parser.Elements;

namespace SkEditor.API;

/// <summary>
/// Holds every registry used by the application.
/// </summary>
public static class Registries
{

    public static readonly Registry<ConnectionData> Connections = new();
    public static readonly Registry<IBottomIconElement> BottomIcons = new();
    public static readonly Registry<SidebarPanel> SidebarPanels = new();
    public static readonly Registry<WelcomeEntryData> WelcomeEntries = new();
    public static readonly Registry<MarginIconData> MarginIcons = new();
    public static readonly Registry<FileTypeData> FileTypes = new();

    
    public static readonly Registry<ParserElementData> ParserElements = new(); 
    public static readonly Registry<ParserWarning> ParserWarnings = new();
    
    public static void Unload(IAddon addon)
    {
        Connections.Unload(addon);
        BottomIcons.Unload(addon);
        SidebarPanels.Unload(addon);
        WelcomeEntries.Unload(addon);
        MarginIcons.Unload(addon);
        FileTypes.Unload(addon);
        
        ParserElements.Unload(addon);
        ParserWarnings.Unload(addon);
    }
}