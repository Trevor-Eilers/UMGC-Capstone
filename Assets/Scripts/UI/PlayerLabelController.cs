using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class PlayerLabelController : UIPresenterBase
    {
        public readonly PlayerLabelViewModel viewModel = new();

        private Label[] _labels;
        private PlayerCardViewModel[] _cardVms;
        private VisualElement[] _cardWidgets;

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
            
            _cardVms     = new PlayerCardViewModel[_labels.Length];
            _cardWidgets = new VisualElement[_labels.Length];
            var template = Resources.Load<VisualTreeAsset>("PlayerCardWidget");
            
            for (int i = 0; i < _labels.Length; i++)
            {
                int slot = i;

                _cardWidgets[slot] = template.CloneTree();
                _cardVms[slot] = ScriptableObject.CreateInstance<PlayerCardViewModel>();
                _cardWidgets[slot].dataSource = _cardVms[slot];
            }
            
            while (GameManager.Instance == null 
                   || GameManager.Instance.players.Count < 4) yield return null;
            yield return StartCoroutine(viewModel.Update());
            yield return StartCoroutine(BindPlayersToCards());
            
            var panelRoot = root.panel?.visualTree;

            // Per-slot: which corner of the label to anchor to, and which corner of the tooltip sits there.
            // Slot 0 = P1 top-left, 1 = P2 top-right, 2 = P3 bottom-left, 3 = P4 bottom-right.
            var tooltipConfigs = new (Func<Rect, Vector2> anchor, TooltipCorner corner)[]
            {
                (r => new Vector2(r.xMax, r.yMax), TooltipCorner.TopLeft),     // top-left label → tooltip bottom-right
                (r => new Vector2(r.xMin, r.yMax), TooltipCorner.TopRight),    // top-right label → tooltip bottom-left
                (r => new Vector2(r.xMax, r.yMin), TooltipCorner.BottomLeft),  // bottom-left label → tooltip top-right
                (r => new Vector2(r.xMin, r.yMin), TooltipCorner.BottomRight), // bottom-right label → tooltip top-left
            };

            for (int i = 0; i < _labels.Length; i++)
            {
                int slot = i;

                var tooltip = new Tooltip(panelRoot);
                tooltip.SetContent(_cardWidgets[slot]);

                _labels[slot].RegisterCallback<MouseEnterEvent>(_ =>
                {
                    var rect   = _labels[slot].worldBound;
                    var anchor = panelRoot.WorldToLocal(tooltipConfigs[slot].anchor(rect));
                    tooltip.Show(anchor, tooltipConfigs[slot].corner);
                });
                _labels[slot].RegisterCallback<MouseLeaveEvent>(_ => tooltip.Hide());
            }
        }

        private IEnumerator BindPlayersToCards()
        {
            int slot = 0;
            foreach (var playerRef in GameManager.Instance.players)
            {
                if (slot >= _labels.Length) yield break;

                playerRef.TryGet(out NetworkObject networkObject, NetworkManager.Singleton);
                if (networkObject == null)
                    yield return new WaitUntil(() =>
                    {
                        playerRef.TryGet(out networkObject, NetworkManager.Singleton);
                        return networkObject != null;
                    });

                var player = networkObject.GetComponent<Player>();
                BindSlotToPlayer(slot, player);
                slot++;
            }
        }

        private void BindSlotToPlayer(int slot, Player player)
        {
            _cardVms[slot].PlayerName = player.playerName.Value.ToString();

            player.districtNetRef.OnValueChanged += (_, _) =>
            {
                var district = player.District;
                if (district == null) return;
                SubscribeAndPrime(slot, district);
            };

            // districtNetRef may already be set on the client by the time we bind
            if (player.District != null)
                SubscribeAndPrime(slot, player.District);
        }

        private void SubscribeAndPrime(int slot, District district)
        {
            district.state.OnValueChanged += (_, newState) =>
                _cardVms[slot].UpdateFromDistrictState(newState);
            _cardVms[slot].UpdateFromDistrictState(district.state.Value);
        }

        private void OnTextChanged(List<FixedString64Bytes> players)
        {
            if (_labels == null) return;

            for (int i = 0; i < players.Count && i < _labels.Length; i++)
            {
                if (_labels[i] == null) continue;
                _labels[i].text = players[i].ToString();
            }
        }

        private void OnDestroy()
        {
            viewModel.OnTextChanged -= OnTextChanged;

            if (_cardVms == null) return;
            foreach (var vm in _cardVms)
                if (vm != null) Destroy(vm);
        }
    }
}
