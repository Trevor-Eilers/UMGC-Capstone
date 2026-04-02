using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    [UxmlElement]
    public partial class IndicatorWidget : VisualElement
    {
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

        public IndicatorWidget()
        {
            var template = Resources.Load<VisualTreeAsset>("IndicatorWidget");
            template.CloneTree(this);
        }
    }
}