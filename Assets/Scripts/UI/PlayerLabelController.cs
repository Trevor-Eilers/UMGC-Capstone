using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class PlayerLabelController : MonoBehaviour
    {
        public readonly PlayerLabelViewModel viewModel = new();

        private UIDocument _doc;
        private VisualElement _root;

        private Label[] _labels;

        void Start()
        {
            _doc = GetComponent<UIDocument>();
            
            viewModel.OnTextChanged += OnTextChanged;

            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            while (_root == null)
            {
                _root = _doc.rootVisualElement;
                yield return null;
            }
            
            _labels = new[]
            {
                _root.Q<Label>("P1Label"),
                _root.Q<Label>("P2Label"),
                _root.Q<Label>("P3Label"),
                _root.Q<Label>("P4Label"),
            };
        }

        private void OnTextChanged(List<FixedString64Bytes> players)
        {
            for (int i = 0; i < players.Count; i++)
            {
                _labels[i].text = players[i].ToString();
            }
        }

        private void OnDestroy()
        {
            viewModel.OnTextChanged -= OnTextChanged;
        }
    }
}
