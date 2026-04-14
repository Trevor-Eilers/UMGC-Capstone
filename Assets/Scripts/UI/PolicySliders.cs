using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class PolicySliders : MonoBehaviour
    {
        public event Action<PolicyValues> OnPolicyChanged;

        private UIDocument _doc;
        private VisualElement _root;
        private bool _dirty;

        public float taxRate = SimulationConstants.TAX_RATE_DEFAULT;
        public float education = SimulationConstants.SLIDERS_DEFAULT;
        public float infrastructure = SimulationConstants.SLIDERS_DEFAULT;
        public float housing = SimulationConstants.SLIDERS_DEFAULT;
        public float environment = SimulationConstants.SLIDERS_DEFAULT;
        public float cityContribution = SimulationConstants.CITY_CONTRIB_DEFAULT;

        void Start()
        {
            _doc = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;

            var taxSlider = _root.Q<Slider>("TaxSlider");
            var eduSlider = _root.Q<Slider>("EduSlider");
            var infraSlider = _root.Q<Slider>("InfraSlider");
            var housingSlider = _root.Q<Slider>("HousingSlider");
            var envSlider = _root.Q<Slider>("EnvSlider");
            var citySlider = _root.Q<Slider>("CitySlider");

            taxSlider.value = taxRate;
            eduSlider.value = education;
            infraSlider.value = infrastructure;
            housingSlider.value = housing;
            envSlider.value = environment;
            citySlider.value = cityContribution;
            
            taxSlider.RegisterValueChangedCallback(OnSliderValueChanged);
            eduSlider.RegisterValueChangedCallback(OnSliderValueChanged);
            infraSlider.RegisterValueChangedCallback(OnSliderValueChanged);
            housingSlider.RegisterValueChangedCallback(OnSliderValueChanged);
            envSlider.RegisterValueChangedCallback(OnSliderValueChanged);
            citySlider.RegisterValueChangedCallback(OnSliderValueChanged);
        }

        private void OnSliderValueChanged(ChangeEvent<float> evt)
        {
            string slider = (evt.target as VisualElement)?.name;

            switch (slider)
            {
                case "TaxSlider": taxRate = evt.newValue; break;
                case "EduSlider": education = evt.newValue; break;
                case "InfraSlider": infrastructure = evt.newValue; break;
                case "HousingSlider": housing = evt.newValue; break;
                case "EnvSlider": environment = evt.newValue; break;
                case "CitySlider": cityContribution = evt.newValue; break;
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
                taxRate = taxRate,
                education = education,
                infrastructure = infrastructure,
                housing = housing,
                environment = environment,
                cityContribution = cityContribution
            });
        }
    }
}