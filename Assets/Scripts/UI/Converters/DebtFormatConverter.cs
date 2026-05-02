// Data-binding converter for the Debt metric (range 0-80, cap at 60).
// Displays as "62 / 80" so players see the actual ceiling, not the misleading
// "/100" from the generic IndexFormat which clamps and obscures the danger zone.

using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public static class DebtFormatConverter
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Register()
        {
            var group = new ConverterGroup("DebtFormat");

            group.AddConverter((ref float value) =>
            {
                int rounded = Mathf.RoundToInt(Mathf.Clamp(value, 0f, 80f));
                return $"{rounded} / 80";
            });

            group.AddConverter((ref int value) =>
            {
                int clamped = Mathf.Clamp(value, 0, 80);
                return $"{clamped} / 80";
            });

            ConverterGroups.RegisterConverterGroup(group);
        }
    }
}
