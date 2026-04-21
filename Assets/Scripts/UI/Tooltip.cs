using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class Tooltip
    {
        private const float MaxWidth = 280f;

        private readonly VisualElement tooltip;
        private readonly Label label;

        public Tooltip(VisualElement root)
        {
            tooltip = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    backgroundColor = new Color(0.06f, 0.06f, 0.09f, 0.96f),
                    borderTopLeftRadius = 6,
                    borderTopRightRadius = 6,
                    borderBottomLeftRadius = 6,
                    borderBottomRightRadius = 6,
                    borderTopColor = new Color(0.30f, 0.30f, 0.36f, 1f),
                    borderRightColor = new Color(0.30f, 0.30f, 0.36f, 1f),
                    borderBottomColor = new Color(0.30f, 0.30f, 0.36f, 1f),
                    borderLeftColor = new Color(0.30f, 0.30f, 0.36f, 1f),
                    borderTopWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 6,
                    paddingBottom = 6,
                    maxWidth = MaxWidth,
                    display = DisplayStyle.None
                }
            };

            label = new Label
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    color = new Color(0.92f, 0.92f, 0.92f, 1f),
                    fontSize = 11,
                    whiteSpace = WhiteSpace.Normal,
                    maxWidth = MaxWidth - 20
                }
            };

            tooltip.Add(label);
            root.Add(tooltip);
            tooltip.BringToFront();
        }

        public void Show(string text, Vector2 position, Vector2 offset)
        {
            if (string.IsNullOrEmpty(text))
            {
                Hide();
                return;
            }
            label.text = text;
            tooltip.style.left = position.x + offset.x;
            tooltip.style.top = position.y + offset.y;
            tooltip.style.display = DisplayStyle.Flex;
            tooltip.BringToFront();
        }

        public void Hide()
        {
            tooltip.style.display = DisplayStyle.None;
        }
    }
}
