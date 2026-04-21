using System.Collections;
using System.Collections.Generic;
using Core;
using Network;
using UnityEngine.UIElements;

namespace UI.TopBar
{
    public class TopBarController : DistrictBoundPresenter<TopBarViewModel>
    {
        private Button _speed1Button;
        private Button _speed2Button;
        private Button _speed3Button;
        private Button _pauseButton;
        private Button _helpButton;
        private Button _activeButton;

        private void Start()
        {
            AcquireRoot();

            _speed1Button = root.Q<Button>("Speed1Btn");
            _speed2Button = root.Q<Button>("Speed2Btn");
            _speed3Button = root.Q<Button>("Speed3Btn");
            _pauseButton = root.Q<Button>("PauseBtn");
            _helpButton = root.Q<Button>("HelpButton");

            StartCoroutine(InitializeControlsWhenReady());
        }

        private IEnumerator InitializeControlsWhenReady()
        {
            while (ConnectionManager.Instance == null || ConnectionManager.Instance.Session == null)
                yield return null;

            var helpOverlay = GetComponent<HelpOverlayController>();
            if (_helpButton != null && helpOverlay != null)
                _helpButton.clicked += () => helpOverlay.Show();

            if (!ConnectionManager.Instance.Session.IsHost)
            {
                _speed1Button?.SetEnabled(false);
                _speed2Button?.SetEnabled(false);
                _speed3Button?.SetEnabled(false);
                _pauseButton?.SetEnabled(false);
                yield break;
            }

            _speed1Button.clicked += () => SetActive(_speed1Button);
            _speed2Button.clicked += () => SetActive(_speed2Button);
            _speed3Button.clicked += () => SetActive(_speed3Button);
            _pauseButton.clicked += () => SetActive(_pauseButton);

            if (_speed1Button != null)
                SetActive(_speed1Button);
        }

        protected override void OnViewModelBound(Player player)
        {
            AcquireRoot();
            viewModel.BindToPanel(root);

            viewModel.OnSpeedChangeRequested += speed => GameManager.Instance.RequestSetSpeedRpc(speed);
            viewModel.OnPauseChangeRequested += paused => GameManager.Instance.RequestSetPauseRpc(paused);
            viewModel.OnQuitRequested += () => GameManager.Instance.RequestQuitRpc(player.NetworkObjectId);

            StartCoroutine(SubscribeToGameStateWhenReady());
        }

        private IEnumerator SubscribeToGameStateWhenReady()
        {
            while (GameManager.Instance == null) yield return null;
            GameManager.Instance.GameState.OnValueChanged += (_, newVal) => viewModel.UpdateFromGameState(newVal);
            viewModel.UpdateFromGameState(GameManager.Instance.GameState.Value);
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
