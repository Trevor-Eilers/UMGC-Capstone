// Data-binding converter group for displaying numeric values truncated to one decimal place.

using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public static class OneDecimalConverter
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Register()
        {
            var group = new ConverterGroup("OneDecimal");

            group.AddConverter((ref float value) =>
            {
                float truncated = Mathf.Floor(value * 10f) / 10f;
                return truncated.ToString("F1");
            });

            group.AddConverter((ref int value) =>
            {
                return value.ToString();
            });

            ConverterGroups.RegisterConverterGroup(group);
        }
    }
}
