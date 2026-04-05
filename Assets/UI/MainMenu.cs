using System;
using Unity.Services.Authentication;
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
        private Button _createButton;

        public ConnectionManager connectionManager;
        public LobbyManager lobbyManager;
        
        void Start()
        {
            _doc = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;

            _nameField = _root.Q<TextField>("DisplayNameField");
            _sessionField = _root.Q<TextField>("SessionNameField");
            _joinButton = _root.Q<Button>("JoinButton");
            _createButton = _root.Q<Button>("CreateButton");

            _joinButton.RegisterCallback<ClickEvent>(OnJoinButtonClick);
            _createButton.RegisterCallback<ClickEvent>(OnCreateButtonClick);
        }

        private async void OnJoinButtonClick(ClickEvent evt)
        {
            try
            {
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    var success = await connectionManager.Authenticate(_nameField.text);
                    if (!success) return;
                }
                
                await lobbyManager.JoinLobbyByName(_sessionField.text, _nameField.text);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private async void OnCreateButtonClick(ClickEvent evt)
        {
            try
            {
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    var success = await connectionManager.Authenticate(_nameField.text);
                    if (!success) return;
                }
                
                await lobbyManager.CreateLobby(_sessionField.text, _nameField.text);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}