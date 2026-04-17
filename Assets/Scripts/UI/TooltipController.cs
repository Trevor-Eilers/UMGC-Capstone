using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class TooltipController
    {
        private readonly VisualElement tooltip;
        private readonly Label label;

        public TooltipController(VisualElement root)
        {
            tooltip = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    backgroundColor = new Color(0, 0, 0, 0.85f),
                    paddingLeft = 6,
                    paddingRight = 6,
                    paddingTop = 4,
                    paddingBottom = 4,
                    display = DisplayStyle.None
                }
            };

            label = new Label
            {
                style =
                {
                    color = Color.white
                }
            };

            tooltip.Add(label);
            root.Add(tooltip);
        }

        public void Show(string text, Vector2 position, Vector2 offset)
        {
            label.text = text;
            tooltip.style.left = position.x + offset.x;
            tooltip.style.top = position.y + offset.y;
            tooltip.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            tooltip.style.display = DisplayStyle.None;
        }
    }
}