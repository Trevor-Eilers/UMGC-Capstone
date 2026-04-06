using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    [UxmlElement]
    public partial class PolicyBarWidget : VisualElement
    {
        private PolicyValues _values;
        
        [UxmlAttribute]
        public float TaxValue
        {
            get => this.Q<Slider>("TaxSlider").value;
            set => this.Q<Slider>("TaxSlider").value = value;
        }

        public PolicyBarWidget()
        {
            var template = Resources.Load<VisualTreeAsset>("PolicyBarWidget");
            template.CloneTree(this);
            
        }

        void OnSliderValueChanged(ChangeEvent<float> evt)
        {
            
        }
    }
}