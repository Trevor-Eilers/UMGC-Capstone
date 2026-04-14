using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    [UxmlElement]
    public partial class IndicatorWidget : VisualElement
    {
        private VisualElement _barFill;
        
        [UxmlAttribute]
        public string Title
        {
            get => this.Q<Label>("Label").text;
            set => this.Q<Label>("Label").text = value;
        }

        [UxmlAttribute]
        public string Value
        {
            get => this.Q<Label>("IndicatorValue").text;
            set => this.Q<Label>("IndicatorValue").text = value;
        }

        public void SetFill(float percent, Color color)
        {
            if (_barFill == null)
                _barFill = this.Q<VisualElement>("BarFill");
            if (_barFill == null) return;

            _barFill.style.width = Length.Percent(Mathf.Clamp(percent, 0f, 100f));
            _barFill.style.backgroundColor = color;
        }

        public IndicatorWidget()
        {
            var template = Resources.Load<VisualTreeAsset>("IndicatorWidget");
            template.CloneTree(this);
        }
    }
}
