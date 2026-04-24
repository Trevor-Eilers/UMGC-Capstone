using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    // Names the corner of the tooltip that sits at the anchor point.
    public enum TooltipCorner { TopLeft, TopRight, BottomLeft, BottomRight }

    public class Tooltip
    {
        private const float MaxWidth = 280f;

        private readonly VisualElement tooltip;
        private readonly Label label;
        private VisualElement _content;

        private Vector2 _pendingAnchor;
        private TooltipCorner _pendingCorner;

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

        public void SetContent(VisualElement content)
        {
            if (_content != null) tooltip.Remove(_content);
            tooltip.Remove(label);
            _content = content;
            tooltip.Add(_content);
        }

        public void Show(Vector2 position, Vector2 offset)
        {
            tooltip.style.left = position.x + offset.x;
            tooltip.style.top  = position.y + offset.y;
            tooltip.style.display = DisplayStyle.Flex;
            tooltip.BringToFront();
        }

        public void Show(Vector2 anchor, TooltipCorner corner)
        {
            _pendingAnchor = anchor;
            _pendingCorner = corner;
            tooltip.style.left = anchor.x;
            tooltip.style.top  = anchor.y;
            tooltip.style.display = DisplayStyle.Flex;
            tooltip.BringToFront();
            tooltip.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            tooltip.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            var w = tooltip.resolvedStyle.width;
            var h = tooltip.resolvedStyle.height;
            tooltip.style.left = _pendingAnchor.x - (_pendingCorner is TooltipCorner.TopRight or TooltipCorner.BottomRight ? w : 0);
            tooltip.style.top  = _pendingAnchor.y - (_pendingCorner is TooltipCorner.BottomLeft or TooltipCorner.BottomRight ? h : 0);
        }

        public void Hide()
        {
            tooltip.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            tooltip.style.display = DisplayStyle.None;
        }
    }
}
