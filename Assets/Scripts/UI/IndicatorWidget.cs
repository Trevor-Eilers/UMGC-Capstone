using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    [UxmlElement]
    public partial class IndicatorWidget : VisualElement
    {
        private VisualElement _barFill;

        private Label _titleLabel;
        private string _title;
        
        private Label _valueLabel;
        private string _value;

        [UxmlAttribute]
        [CreateProperty]
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                _titleLabel ??= this.Q<Label>("Label");
                if (_titleLabel != null) _titleLabel.text = value;
            }
        }

        [UxmlAttribute]
        [CreateProperty]
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                _valueLabel ??= this.Q<Label>("IndicatorValue");
                if (_valueLabel != null) _valueLabel.text = value;
            }
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
