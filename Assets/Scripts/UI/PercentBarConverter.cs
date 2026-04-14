// Data-binding converter group for driving progress-bar widths from view-model values.
//
// Call PercentBarConverter.Register() once at startup (e.g. from a [RuntimeInitializeOnLoadMethod]).
//
// Usage in UIBuilder:
//   1. Select the bar-fill VisualElement (e.g. CityRepBar).
//   2. Add a DataBinding on property "style.width".
//   3. Set data-source-path to the view-model property (e.g. "CityReputation").
//   4. Set source-to-ui-converters to "PercentBar".
//
// Compatible binding paths:
//   TopBarViewModel:      CityReputation (0-100), SharedInfraQuality (0-100),
//                      MetroInflowPercent (0-100, normalized from signed flow)
//   DistrictViewModel: Gdp, Happiness, Infrastructure, Sustainability, Efficiency,
//                      Debt (0-80, will clamp to 100)

using UnityEngine;
using UnityEngine.UIElements;

public static class PercentBarConverter
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Register()
    {
        var group = new ConverterGroup("PercentBar");

        // int (0–100) → StyleLength percentage  (TopBarViewModel properties)
        group.AddConverter((ref int value) =>
        {
            return new StyleLength(Length.Percent(Mathf.Clamp(value, 0, 100)));
        });

        // float (0–100) → StyleLength percentage  (DistrictViewModel properties)
        group.AddConverter((ref float value) =>
        {
            return new StyleLength(Length.Percent(Mathf.Clamp(value, 0f, 100f)));
        });
        
        ConverterGroups.RegisterConverterGroup(group);
    }
}
