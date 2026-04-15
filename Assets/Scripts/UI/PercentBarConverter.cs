// Converter group for calculating progress-bar width from view-model values.

using UnityEngine;
using UnityEngine.UIElements;

public static class PercentBarConverter
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Register()
    {
        var group = new ConverterGroup("PercentBar");
        
        
        group.AddConverter((ref int value) =>
        {
            return new StyleLength(Length.Percent(Mathf.Clamp(value, 0, 100)));
        });


        group.AddConverter((ref float value) =>
        {
            return new StyleLength(Length.Percent(Mathf.Clamp(value, 0f, 100f)));
        });
        
        
        ConverterGroups.RegisterConverterGroup(group);
    }
}
