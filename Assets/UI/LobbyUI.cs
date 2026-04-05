using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class LobbyUI : MonoBehaviour
    {
        public event Action OnStartClicked;
        public event Action OnLeaveClicked;

        [SerializeField] private UIDocument doc;
        private VisualElement _root;
        private ListView _playerList;
        private Button _startButton;
        private Button _leaveButton;
        private Label _waitingLabel;

        private List<string> _playerNames = new();

        void Awake()
        {
            doc = GetComponent<UIDocument>();
            _root = doc.rootVisualElement;

            _playerList = _root.Q<ListView>("PlayerList");
            _startButton = _root.Q<Button>("StartButton");
            _leaveButton = _root.Q<Button>("LeaveButton");
            _waitingLabel = _root.Q<Label>("WaitingLabel");
            
            _waitingLabel.text = "Connecting...";

            _startButton.RegisterCallback<ClickEvent>(_ => OnStartClicked?.Invoke());
            _leaveButton.RegisterCallback<ClickEvent>(_ => OnLeaveClicked?.Invoke());

            _playerList.makeItem = () => new Label();
            _playerList.bindItem = (element, index) =>
                ((Label)element).text = _playerNames[index];
            _playerList.itemsSource = _playerNames;
            _playerList.fixedItemHeight = 32f;
            _playerList.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
        }

        public void SetVisible(bool visible)
        {
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        public void SetStartButtonVisible(bool visible)
        {
            _startButton.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            _waitingLabel.style.display = visible
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        public void SetLeaveButtonVisible(bool visible)
        {
            _leaveButton.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
        
        public void SetPlayerList(List<string> names)
        {
            _playerNames.Clear();
            _playerNames.AddRange(names);
            _playerList.Rebuild();
        }
        
        public void SetConnected(bool connected)
        {
            _waitingLabel.text = connected ? "Waiting for host to start..." : "Connecting...";
        }
    }
}