using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.UIElements;

namespace UI
{
    public class PlayerLabelController : UIPresenterBase
    {
        public readonly PlayerLabelViewModel viewModel = new();

        private Label[] _labels;

        void Start()
        {
            viewModel.OnTextChanged += OnTextChanged;
            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            yield return WaitForRoot();

            _labels = new[]
            {
                root.Q<Label>("P1Label"),
                root.Q<Label>("P2Label"),
                root.Q<Label>("P3Label"),
                root.Q<Label>("P4Label"),
            };
        }

        private void OnTextChanged(List<FixedString64Bytes> players)
        {
            if (_labels == null) return;

            for (int i = 0; i < players.Count; i++)
            {
                if (_labels[i] == null) continue;
                _labels[i].text = players[i].ToString();
            }
        }

        private void OnDestroy()
        {
            viewModel.OnTextChanged -= OnTextChanged;
        }
    }
}
