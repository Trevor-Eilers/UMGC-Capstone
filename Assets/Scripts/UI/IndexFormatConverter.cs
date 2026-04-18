// Data-binding converter group for 0-100 index metrics.
// Displays values as "50 / 100" so players see they're on a bounded scale.

using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public static class IndexFormatConverter
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Register()
        {
            var group = new ConverterGroup("IndexFormat");

            group.AddConverter((ref float value) =>
            {
                int rounded = Mathf.RoundToInt(Mathf.Clamp(value, 0f, 100f));
                return $"{rounded} / 100";
            });

            group.AddConverter((ref int value) =>
            {
                int clamped = Mathf.Clamp(value, 0, 100);
                return $"{clamped} / 100";
            });

            ConverterGroups.RegisterConverterGroup(group);
        }
    }
}
