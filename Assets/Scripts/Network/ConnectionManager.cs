// Author: Trevor Eilers

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
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
        public int PlayerCount => Session.PlayerCount;
   
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
   
        public ISession Session { get; private set; }
        public IHostSession HostSession { get; private set; }
   
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
   
        public async Task CreateOrJoinSessionAsync(string profileName, string sessionName, Lobby lobby)
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

                if (AuthenticationService.Instance.PlayerId != lobby.HostId) await Task.Delay(3000);
                
                Session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, options);
   
                State = ConnectionState.Connected;
            }
            catch (Exception e)
            {
                State = ConnectionState.Disconnected;
                Debug.LogException(e);
            }
        }
        
        public async Task JoinSessionAsync(string profileName, string sessionName)
        {
            State = ConnectionState.Connecting;
            
            ProfileName = profileName;
            SessionName = sessionName;

            do
            {
                await Task.Delay(2000);
                if (!AuthenticationService.Instance.IsSignedIn) await Authenticate(profileName);
                Debug.Log("Attempting to join Netcode session.");
                
                var options = new QuerySessionsOptions
                {
                    FilterOptions = new List<FilterOption>
                    {
                        new(
                            field: FilterField.Name,
                            value: sessionName,
                            operation: FilterOperation.Equal
                        )
                    }
                };

                var results = await MultiplayerService.Instance.QuerySessionsAsync(options);
                
                if (results.Sessions.Count > 0)
                {
                    Session = await MultiplayerService.Instance.JoinSessionByIdAsync(results.Sessions[0].Id);
                }
                else
                {
                    Debug.Log("Session not found. Retrying.");
                }
            } 
            while (Session == null);
            
            State = ConnectionState.Connected;
        }
        
        public async Task CreateSessionAsync(string profileName, string sessionName)
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
   
                Session = await MultiplayerService.Instance.CreateSessionAsync(options);
   
                State = ConnectionState.Connected;
            }
            catch (Exception e)
            {
                State = ConnectionState.Disconnected;
                Debug.LogException(e);
            }
        }

        public async Task JoinSessionByIdDirectAsync(string profileName, string sessionId)
        {
            State = ConnectionState.Connecting;
            const int maxRetries = 3;
            const int retryDelayMs = 3000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    ProfileName = profileName;

                    if (!AuthenticationService.Instance.IsSignedIn) await Authenticate(profileName);

                    Session = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);

                    State = ConnectionState.Connected;
                    return; // success — exit immediately
                }
                catch (SessionException e) when (attempt < maxRetries)
                {
                    Debug.LogWarning($"Session join attempt {attempt}/{maxRetries} failed: {e.Message}. Retrying in {retryDelayMs}ms...");
                    await Task.Delay(retryDelayMs);
                }
                catch (Exception e)
                {
                    // Non-retryable error or final attempt exhausted
                    State = ConnectionState.Disconnected;
                    Debug.LogException(e);
                    return;
                }
            }
            
            State = ConnectionState.Disconnected;
            Debug.LogError("JoinSessionByIdDirectAsync: all retry attempts exhausted.");
        }
    }
}