using System;
using System.Linq;
using SkEditor.API;

namespace SkEditor.Utilities.Parser;

public static class ParserRegistryExtensions
{

    public static void RegisterWarnings(this Registry<ParserElementData> registry, SkEditorSelfAddon selfAddon)
    {
        foreach (var type in registry.GetValues().Select(x => x.Type))
            RegisterWarning(type, selfAddon);
    }
    
    private static void RegisterWarning(Type type, SkEditorSelfAddon selfAddon)
    {
        foreach (var field in type.GetFields())
        {
            if (field.FieldType == typeof(ParserWarning))
            {
                var warning = (ParserWarning) field.GetValue(null);
                Registries.ParserWarnings.Register(new RegistryKey(selfAddon, warning.Identifier), warning);
            }
        }
    }
    
}