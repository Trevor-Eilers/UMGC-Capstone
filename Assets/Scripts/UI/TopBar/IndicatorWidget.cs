using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    [UxmlElement]
    public partial class IndicatorWidget : VisualElement
    {
        private static readonly Color DeltaUp   = new(0.55f, 0.90f, 0.55f);
        private static readonly Color DeltaDown = new(0.95f, 0.45f, 0.45f);
        private static readonly Color DeltaFlat = new(0.55f, 0.55f, 0.55f);

        private VisualElement _barFill;

        private Label _titleLabel;
        private string _title;

        private Label _valueLabel;
        private string _value;

        private Label _deltaLabel;
        private string _delta;

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

        [UxmlAttribute]
        [CreateProperty]
        public string Delta
        {
            get => _delta;
            set
            {
                _delta = value;
                _deltaLabel ??= this.Q<Label>("DeltaLabel");
                if (_deltaLabel == null) return;
                _deltaLabel.text = value ?? string.Empty;

                // Colour the chip based on its arrow: ▲ green, ▼ red, empty hidden.
                if (string.IsNullOrEmpty(value))
                {
                    _deltaLabel.style.color = DeltaFlat;
                    _deltaLabel.style.visibility = Visibility.Hidden;
                }
                else if (value.StartsWith("\u25B2"))
                {
                    _deltaLabel.style.color = DeltaUp;
                    _deltaLabel.style.visibility = Visibility.Visible;
                }
                else if (value.StartsWith("\u25BC"))
                {
                    _deltaLabel.style.color = DeltaDown;
                    _deltaLabel.style.visibility = Visibility.Visible;
                }
                else
                {
                    _deltaLabel.style.color = DeltaFlat;
                    _deltaLabel.style.visibility = Visibility.Visible;
                }
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

            // Defer tooltip wiring until this widget has a panel, so we can
            // find the UIDocument root and anchor the popup there (tooltips
            // added to `this` clip to the widget's own bounds).
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                var panelRoot = panel?.visualTree;
                if (panelRoot == null) return;
                var controller = new TooltipController(panelRoot);

                RegisterCallback<MouseEnterEvent>(_ =>
                {
                    var rect = worldBound;
                    var anchor = panelRoot.WorldToLocal(new Vector2(rect.xMin, rect.yMax));
                    controller.Show(tooltip, anchor, new Vector2(0, 6));
                });
                RegisterCallback<MouseLeaveEvent>(_ => controller.Hide());
            });
        }
    }
}
