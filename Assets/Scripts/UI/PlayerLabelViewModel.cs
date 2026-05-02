using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Core;
using Unity.Collections;
using Unity.Netcode;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class PlayerLabelViewModel : ScriptableObject, INotifyBindablePropertyChanged
    {
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        [SerializeField] private string _p1Name;
        [SerializeField] private string _p2Name;
        [SerializeField] private string _p3Name;
        [SerializeField] private string _p4Name;

        [CreateProperty] public string P1Name { get => _p1Name; set => SetProperty(ref _p1Name, value); }
        [CreateProperty] public string P2Name { get => _p2Name; set => SetProperty(ref _p2Name, value); }
        [CreateProperty] public string P3Name { get => _p3Name; set => SetProperty(ref _p3Name, value); }
        [CreateProperty] public string P4Name { get => _p4Name; set => SetProperty(ref _p4Name, value); }

        private readonly Dictionary<Player, NetworkVariable<FixedString64Bytes>.OnValueChangedDelegate> _nameHandlers = new();

        public void Bind()
        {
            GameManager.Instance.players.OnListChanged += OnPlayersChanged;
            Refresh();
        }

        public void Unbind()
        {
            GameManager.Instance.players.OnListChanged -= OnPlayersChanged;
            foreach (var (player, handler) in _nameHandlers)
                player.playerName.OnValueChanged -= handler;
            _nameHandlers.Clear();
        }

        private void OnPlayersChanged(NetworkListEvent<NetworkObjectReference> _) => Refresh();

        private void Refresh()
        {
            foreach (var (player, handler) in _nameHandlers)
                player.playerName.OnValueChanged -= handler;
            _nameHandlers.Clear();

            var players = GameManager.Instance.players;
            var names = new[] { "", "", "", "" };
            int i = 0;
            foreach (var playerRef in players)
            {
                if (i >= names.Length) break;
                playerRef.TryGet(out NetworkObject networkObject, NetworkManager.Singleton);
                if (networkObject != null)
                {
                    var player = networkObject.GetComponent<Player>();
                    names[i] = player.playerName.Value.ToString();

                    NetworkVariable<FixedString64Bytes>.OnValueChangedDelegate handler = (_, _) => Refresh();
                    player.playerName.OnValueChanged += handler;
                    _nameHandlers[player] = handler;
                }
                i++;
            }
            P1Name = names[0];
            P2Name = names[1];
            P3Name = names[2];
            P4Name = names[3];
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string name = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            Notify(name);
            return true;
        }

        private void Notify([CallerMemberName] string property = "")
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }
    }
}
