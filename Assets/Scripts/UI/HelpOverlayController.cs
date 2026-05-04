using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    // Shows a full-screen How-to-Play reference when the player clicks the ?
    // button on the top bar. Shares the Player's UIDocument; the overlay
    // element is hidden via display:none until Toggle() is called.
    public class HelpOverlayController : UIPresenterBase
    {
        private VisualElement _overlay;
        private Button _closeBtn;

        private void Start()
        {
            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            yield return WaitForRoot();
            Build();
            if (_overlay != null) _overlay.style.display = DisplayStyle.None;
        }

        public void Show()
        {
            AcquireRoot();
            if (root == null) return;
            if (_overlay != null) _overlay.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            if (_overlay != null) _overlay.style.display = DisplayStyle.None;
        }

        private void Build()
        {
            _overlay = root.Q<VisualElement>("HelpOverlay");
            if (_overlay == null) return;

            _closeBtn = root.Q<Button>("HelpCloseBtn");
            if (_closeBtn != null) _closeBtn.clicked += Hide;

            PopulateSection(root.Q<VisualElement>("HelpPolicies"), HelpText.Policies);
            PopulateSection(root.Q<VisualElement>("HelpDistrict"), HelpText.District);
            PopulateSection(root.Q<VisualElement>("HelpCity"), HelpText.City);
        }

        private static void PopulateSection(VisualElement container, HelpText.Entry[] entries)
        {
            if (container == null) return;
            container.Clear();

            foreach (var entry in entries)
            {
                var row = new VisualElement();
                row.style.marginBottom = 12;
                row.style.paddingTop = 10;
                row.style.paddingBottom = 10;
                row.style.paddingLeft = 14;
                row.style.paddingRight = 14;
                row.style.backgroundColor = new Color(0.073f, 0.073f, 0.105f, 1f);
                row.style.borderTopLeftRadius = 6;
                row.style.borderTopRightRadius = 6;
                row.style.borderBottomLeftRadius = 6;
                row.style.borderBottomRightRadius = 6;

                var title = new Label(entry.Title);
                title.style.color = new Color(0.90f, 0.90f, 0.90f, 1f);
                title.style.fontSize = 13;
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.marginBottom = 4;
                row.Add(title);

                var body = new Label(entry.Body);
                body.style.color = new Color(0.78f, 0.78f, 0.82f, 1f);
                body.style.fontSize = 11;
                body.style.whiteSpace = WhiteSpace.Normal;
                row.Add(body);

                container.Add(row);
            }
        }
    }
}
