// Data-binding converter group for per-tick delta chips.
// Formats a signed float as "▲ +1.2" / "▼ −0.8" / "" (empty for near-zero).
// Pair with USS classes .delta--up / .delta--down / .delta--flat for colouring.

using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public static class DeltaFormatConverter
    {
        private const float EPSILON = 0.05f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Register()
        {
            var group = new ConverterGroup("DeltaFormat");

            group.AddConverter((ref float value) => Format(value));
            group.AddConverter((ref int value) => Format(value));

            ConverterGroups.RegisterConverterGroup(group);
        }

        private static string Format(float delta)
        {
            if (Mathf.Abs(delta) < EPSILON) return string.Empty;
            string arrow = delta > 0f ? "\u25B2" : "\u25BC"; // ▲ / ▼
            string sign = delta > 0f ? "+" : "\u2212";       // + / − (proper minus)
            float abs = Mathf.Abs(delta);
            return $"{arrow} {sign}{abs:0.0}";
        }
    }
}
