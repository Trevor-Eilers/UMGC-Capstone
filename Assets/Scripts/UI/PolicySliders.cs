using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class PolicySliders : MonoBehaviour
    {
        private UIDocument _doc;
        private VisualElement _root;
        
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

            _root.Q<Slider>("TaxSlider").RegisterValueChangedCallback(OnSliderValueChanged);
            _root.Q<Slider>("EduSlider").RegisterValueChangedCallback(OnSliderValueChanged);
            _root.Q<Slider>("InfraSlider").RegisterValueChangedCallback(OnSliderValueChanged);
            _root.Q<Slider>("HousingSlider").RegisterValueChangedCallback(OnSliderValueChanged);
            _root.Q<Slider>("EnvSlider").RegisterValueChangedCallback(OnSliderValueChanged);
            _root.Q<Slider>("CitySlider").RegisterValueChangedCallback(OnSliderValueChanged);
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
        }
    }
}