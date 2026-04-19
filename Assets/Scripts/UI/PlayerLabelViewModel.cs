using System;
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
        

        public void Update()
        {
            _values.Clear();
            foreach (var player in GameManager.Instance.players)
            {
                player.TryGet(out NetworkObject networkObject, NetworkManager.Singleton);
                if (networkObject == null) return;
                var name = networkObject.GetComponent<Player>().playerName.Value;
                _values.Add(name);
            }
            
            OnTextChanged?.Invoke(_values);
        }
    }
}
