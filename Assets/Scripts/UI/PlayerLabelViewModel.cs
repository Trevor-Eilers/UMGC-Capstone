using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace UI
{
    public class PlayerLabelViewModel
    {
        public event Action<List<FixedString64Bytes>> OnTextChanged;
        
        private readonly List<FixedString64Bytes> _values = new();


        public PlayerLabelViewModel()
        {
            for (int i = 0; i < 4; i++)
            {
                 _values.Add(new FixedString64Bytes());
            }
        }
        

        public IEnumerator Update()
        {
            _values.Clear();

            foreach (var player in GameManager.Instance.players)
            {
                NetworkObject networkObject = null;
                while (!player.TryGet(out networkObject, NetworkManager.Singleton))
                    yield return null;

                var playerComp = networkObject.GetComponent<Player>();
                if (playerComp.playerName.Value.IsEmpty)
                    yield return new WaitUntil(() => !playerComp.playerName.Value.IsEmpty);

                _values.Add(playerComp.playerName.Value);
            }

            OnTextChanged?.Invoke(_values);
        }
    }
}
