// Data-binding converter group for population values.
// DistrictState.population is stored in thousands (150.0 = 150k residents).
// Displays as "150k" or "1.2M" for readability.

using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public static class PopulationFormatConverter
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Register()
        {
            var group = new ConverterGroup("PopulationFormat");

            group.AddConverter((ref float value) => Format(value));
            group.AddConverter((ref int value) => Format(value));

            ConverterGroups.RegisterConverterGroup(group);
        }

        private static string Format(float thousands)
        {
            if (thousands >= 1000f)
            {
                float millions = thousands / 1000f;
                return $"{millions:0.0}M";
            }
            int k = Mathf.RoundToInt(thousands);
            return $"{k}k";
        }
    }
}
