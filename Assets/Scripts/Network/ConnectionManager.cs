// Author: Trevor Eilers

using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Network
{
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
    }

    public class ConnectionManager : MonoBehaviour
    {
        public string ProfileName { get; private set; }
        public string SessionName { get; private set; }
   
        private readonly int _maxPlayers = 4;
        public int playerCount = 0;
   
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
   
        public ISession Session { get; private set; }
   
        private NetworkManager _networkManager;
        
   
        private async void Awake()
        {
            try
            {
                _networkManager = GetComponent<NetworkManager>();
                _networkManager.OnClientConnectedCallback += OnClientConnectedCallback;
                _networkManager.OnSessionOwnerPromoted += OnSessionOwnerPromoted;
                await UnityServices.InitializeAsync();
            }
            catch (Exception e)
            {
                Debug.Log(e.StackTrace); // TODO handle exception
            }
        }

        private void OnSessionOwnerPromoted(ulong sessionOwnerPromoted)
        {
            if (_networkManager.LocalClient.IsSessionOwner)
            {
                Debug.Log($"Client-{_networkManager.LocalClientId} is the session owner!");
            }
        }

        private void OnClientConnectedCallback(ulong clientId)
        {
            if (_networkManager.LocalClientId == clientId)
            {
                Debug.Log($"Client-{clientId} is connected.");
            }
        }

        private void OnDestroy()
        {
            Session?.LeaveAsync();
        }

        public async Task<bool> Authenticate(string profileName)
        {
            try
            {
                ProfileName = profileName;
                AuthenticationService.Instance.SwitchProfile(profileName);
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Authentication Succeeded");
                return true;
            }
            catch (Exception e)
            {
                Debug.Log("Authentication failed");
                Debug.LogException(e);
                return false;
            }
        }
   
        public async Task CreateOrJoinSessionAsync(string profileName, string sessionName)
        {
            State = ConnectionState.Connecting;
   
            try
            {
                ProfileName = profileName;
                SessionName = sessionName;

                if (!AuthenticationService.Instance.IsSignedIn) await Authenticate(profileName);
                
   
                var options = new SessionOptions() {
                    Name = sessionName,
                    MaxPlayers = _maxPlayers
                }.WithDistributedAuthorityNetwork();
   
                Session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, options);
   
                State = ConnectionState.Connected;
            }
            catch (Exception e)
            {
                State = ConnectionState.Disconnected;
                Debug.LogException(e);
            }
        }
    }
}