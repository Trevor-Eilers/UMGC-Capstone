using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class PolicySliders : UIPresenterBase
    {
        public event Action<PolicyValues> OnPolicyChanged;

        private bool _dirty;

        [SerializeField, ReadOnly]
        private float taxRate = SimulationConstants.TAX_RATE_DEFAULT;

        [SerializeField, ReadOnly]
        private float education = SimulationConstants.SLIDERS_DEFAULT;

        [SerializeField, ReadOnly]
        private float infrastructure = SimulationConstants.SLIDERS_DEFAULT;

        [SerializeField, ReadOnly]
        private float housing = SimulationConstants.SLIDERS_DEFAULT;

        [SerializeField, ReadOnly]
        private float environment = SimulationConstants.SLIDERS_DEFAULT;

        [SerializeField, ReadOnly]
        private float cityContribution = SimulationConstants.CITY_CONTRIB_DEFAULT;

        void Start()
        {
            AcquireRoot();

            var taxSlider     = root.Q<Slider>("TaxSlider");
            var eduSlider     = root.Q<Slider>("EduSlider");
            var infraSlider   = root.Q<Slider>("InfraSlider");
            var housingSlider = root.Q<Slider>("HousingSlider");
            var envSlider     = root.Q<Slider>("EnvSlider");
            var citySlider    = root.Q<Slider>("CitySlider");

            taxSlider.RegisterValueChangedCallback(OnSliderValueChanged);
            eduSlider.RegisterValueChangedCallback(OnSliderValueChanged);
            infraSlider.RegisterValueChangedCallback(OnSliderValueChanged);
            housingSlider.RegisterValueChangedCallback(OnSliderValueChanged);
            envSlider.RegisterValueChangedCallback(OnSliderValueChanged);
            citySlider.RegisterValueChangedCallback(OnSliderValueChanged);

            taxSlider.value     = taxRate;
            eduSlider.value     = education;
            infraSlider.value   = infrastructure;
            housingSlider.value = housing;
            envSlider.value     = environment;
            citySlider.value    = cityContribution;

            // Tooltip hover — each row has a small "i" icon next to its
            // label. We hover the icon (not the whole row) so that sliding
            // the slider doesn't pop a tooltip over the handle.
            var tooltip = new Tooltip(root);
            foreach (var iconName in new[] { "TaxInfo", "EduInfo", "InfraInfo", "HousingInfo", "EnvInfo", "CityInfo" })
            {
                var icon = root.Q<VisualElement>(iconName);
                if (icon == null || string.IsNullOrEmpty(icon.tooltip)) continue;
                var capturedIcon = icon;
                icon.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    // Pop the tooltip to the LEFT of the icon so it floats
                    // over the map rather than covering the sliders below.
                    var anchor = new Vector2(capturedIcon.worldBound.xMin, capturedIcon.worldBound.y);
                    var pos = root.WorldToLocal(anchor);
                    tooltip.Show(capturedIcon.tooltip, pos, new Vector2(-290, 0));
                });
                icon.RegisterCallback<MouseLeaveEvent>(_ => tooltip.Hide());
            }

            _dirty = true;
        }

        private void OnSliderValueChanged(ChangeEvent<float> evt)
        {
            string slider = (evt.target as VisualElement)?.name;

            switch (slider)
            {
                case "TaxSlider":     taxRate          = evt.newValue; break;
                case "EduSlider":     education        = evt.newValue; break;
                case "InfraSlider":   infrastructure   = evt.newValue; break;
                case "HousingSlider": housing          = evt.newValue; break;
                case "EnvSlider":     environment      = evt.newValue; break;
                case "CitySlider":    cityContribution = evt.newValue; break;
                default: Debug.LogWarning($"Unknown element: {slider}"); break;
            }

            _dirty = true;
        }

        private void LateUpdate()
        {
            if (!_dirty) return;
            _dirty = false;

            OnPolicyChanged?.Invoke(new PolicyValues
            {
                taxRate          = taxRate,
                education        = education,
                infrastructure   = infrastructure,
                housing          = housing,
                environment      = environment,
                cityContribution = cityContribution
            });
        }
    }
}
