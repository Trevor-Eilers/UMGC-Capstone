using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class MainMenu : MonoBehaviour
    {
        private UIDocument _doc;
        private VisualElement _root;

        private TextField _nameField;
        private TextField _sessionField;
        private Button _joinButton;

        public ConnectionManager connectionManager;

        void Start()
        {
            _doc = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;

            _nameField = _root.Q<TextField>("DisplayNameField");
            _sessionField = _root.Q<TextField>("SessionNameField");
            _joinButton = _root.Q<Button>("JoinButton");

            _joinButton.RegisterCallback<ClickEvent>(OnButtonClick);
        }

        private void OnButtonClick(ClickEvent evt)
        {
            _ = connectionManager.CreateOrJoinSessionAsync(_nameField.text, _sessionField.text);
            Instantiate(Resources.Load<GameObject>("Lobby"));
        }
    }
}