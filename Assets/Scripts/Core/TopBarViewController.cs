using System.Collections;
using System.Collections.Generic;
using Network;
using Simulation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class TopBarViewController : MonoBehaviour
    {
        private TopBarViewModel _topBar;
        private readonly Dictionary<string, Button> _buttons = new();
        private Button _activeButton;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _buttons["Speed1Btn"] = root.Q<Button>("Speed1Btn");
            _buttons["Speed2Btn"] = root.Q<Button>("Speed2Btn");
            _buttons["Speed3Btn"] = root.Q<Button>("Speed3Btn");
            _buttons["PauseBtn"]  = root.Q<Button>("PauseBtn");

            StartCoroutine(ConfigureSpeedControlsWhenReady());
        }

        private IEnumerator ConfigureSpeedControlsWhenReady()
        {
            while (ConnectionManager.Instance == null || ConnectionManager.Instance.Session == null)
                yield return null;

            if (!ConnectionManager.Instance.Session.IsHost)
            {
                foreach (var button in _buttons.Values) button.SetEnabled(false);
                yield break;
            }

            foreach (var btn in _buttons.Values)
                btn.clicked += () => SetActive(btn);

            if (_buttons["Speed1Btn"] != null)
                SetActive(_buttons["Speed1Btn"]);
        }

        public void Initialize(Player player)
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _topBar = ScriptableObject.CreateInstance<TopBarViewModel>();
            root.dataSource = _topBar;
            _topBar.BindToPanel(root);

            _topBar.OnSpeedChangeRequested += speed => GameManager.Instance.RequestSetSpeedRpc(speed);
            _topBar.OnPauseChangeRequested += paused => GameManager.Instance.RequestSetPauseRpc(paused);
            _topBar.OnQuitRequested += () => GameManager.Instance.RequestQuitRpc(player.NetworkObjectId);

            player.districtNetRef.OnValueChanged += (_, _) =>
            {
                var district = player.District;
                if (district == null) return;
                district.state.OnValueChanged -= OnDistrictStateChanged;
                district.state.OnValueChanged += OnDistrictStateChanged;
            };

            StartCoroutine(SubscribeToGameStateWhenReady());
        }

        private IEnumerator SubscribeToGameStateWhenReady()
        {
            while (GameManager.Instance == null) yield return null;
            GameManager.Instance.GameState.OnValueChanged += (_, newVal) => _topBar.UpdateFromGameState(newVal);
            _topBar.UpdateFromGameState(GameManager.Instance.GameState.Value);
        }

        private void OnDistrictStateChanged(DistrictState _, DistrictState newState)
            => _topBar.UpdateFromDistrictState(newState);

        private void OnDestroy()
        {
            if (_topBar != null) Destroy(_topBar);
        }

        private void SetActive(Button btn)
        {
            if (_activeButton == btn) return;
            _activeButton?.RemoveFromClassList("speed-btn-active");
            _activeButton = btn;
            _activeButton.AddToClassList("speed-btn-active");
        }
    }
}
