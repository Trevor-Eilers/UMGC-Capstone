using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    // Shows a full-screen How-to-Play reference when the player clicks the ?
    // button on the top bar. Rendered from its own UIDocument (disabled by
    // default) so it sorts above the HUD; while visible, the other
    // UIDocuments are hidden so the HUD can't bleed through.
    public class HelpOverlayController : MonoBehaviour
    {
        public static HelpOverlayController Instance { get; private set; }

        private UIDocument _doc;
        private VisualElement _overlay;
        private Button _closeBtn;
        private bool _built;
        private bool _visible;
        private readonly List<UIDocument> _hiddenWhileOpen = new();

        private void Awake()
        {
            Instance = this;
            _doc = GetComponent<UIDocument>();
            if (_doc == null)
            {
                Debug.LogError("HelpOverlayController: no UIDocument attached.");
                enabled = false;
                return;
            }
            // The document stays disabled until the first Toggle() so its
            // empty root doesn't compete with the HUD's UIDocuments before
            // the player ever opens help.
            _doc.enabled = false;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Toggle()
        {
            if (_visible) Hide();
            else Show();
        }

        public void Show()
        {
            if (_visible) return;
            if (_doc == null) return;

            _doc.enabled = true;
            if (!_built) Build();

            if (_overlay != null) _overlay.style.display = DisplayStyle.Flex;
            _visible = true;

            // Hide other UIDocuments (HUD, policy panel, etc.) so they don't
            // bleed through the overlay. Same trick EndCardController uses.
            _hiddenWhileOpen.Clear();
            foreach (var other in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                if (other == _doc) continue;
                var r = other.rootVisualElement;
                if (r != null && r.style.display != DisplayStyle.None)
                {
                    _hiddenWhileOpen.Add(other);
                    r.style.display = DisplayStyle.None;
                }
            }
        }

        public void Hide()
        {
            if (!_visible) return;
            if (_overlay != null) _overlay.style.display = DisplayStyle.None;
            _visible = false;

            // Restore the UIDocuments we hid on Show(). Guard on null in case
            // any were destroyed while help was open (e.g. scene unload).
            foreach (var other in _hiddenWhileOpen)
            {
                if (other != null && other.rootVisualElement != null)
                    other.rootVisualElement.style.display = DisplayStyle.Flex;
            }
            _hiddenWhileOpen.Clear();
        }

        private void Build()
        {
            var root = _doc.rootVisualElement;
            _overlay = root.Q<VisualElement>("HelpOverlay");
            _closeBtn = root.Q<Button>("HelpCloseBtn");
            if (_closeBtn != null) _closeBtn.clicked += Hide;

            PopulateSection(root.Q<VisualElement>("HelpPolicies"), HelpText.Policies);
            PopulateSection(root.Q<VisualElement>("HelpDistrict"), HelpText.District);
            PopulateSection(root.Q<VisualElement>("HelpCity"), HelpText.City);

            _built = true;
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
