// Data-binding converter group for budget-unit metrics (revenue, reserve, debt).
// Formats as "$1,984" with thousands separator, or "$2.0K" / "$12K" for compactness.

using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public static class CurrencyFormatConverter
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Register()
        {
            var group = new ConverterGroup("CurrencyFormat");

            group.AddConverter((ref float value) => Format(value));
            group.AddConverter((ref int value) => Format(value));

            ConverterGroups.RegisterConverterGroup(group);
        }

        private static string Format(float amount)
        {
            bool negative = amount < 0f;
            float abs = Mathf.Abs(amount);
            string body;

            if (abs >= 10000f)
            {
                float thousands = abs / 1000f;
                body = $"${thousands:0.0}K";
            }
            else
            {
                int whole = Mathf.RoundToInt(abs);
                body = $"${whole:N0}";
            }

            return negative ? $"-{body}" : body;
        }
    }
}
