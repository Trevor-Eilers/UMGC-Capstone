// Author: Trevor Eilers

using System;
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

        private ConnectionState _state = ConnectionState.Disconnected;
        public ConnectionState State
        {
            get => _state;
            private set
            {
                if (_state == value) return;
                Debug.Log($"[ConnectionManager] State: {_state} -> {value}");
                _state = value;
            }
        }
   
        public ISession Session { get; private set; }
   
        private NetworkManager _networkManager;

        private static ConnectionManager _instance;
        public static ConnectionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<ConnectionManager>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[ConnectionManager] Awake: duplicate instance, destroying GameObject");
                Destroy(gameObject);
                return;
            }
            _instance = this;
            Debug.Log("[ConnectionManager] Awake");

            _networkManager = GetComponent<NetworkManager>();
            _networkManager.OnClientConnectedCallback += OnClientConnectedCallback;
            _networkManager.OnSessionOwnerPromoted += OnSessionOwnerPromoted;

            _ = InitializeServicesAsync();
        }

        private async Task InitializeServicesAsync()
        {
            try
            {
                await UnityServices.InitializeAsync();
                Debug.Log("[ConnectionManager] Unity Services initialized");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void OnSessionOwnerPromoted(ulong sessionOwnerPromoted)
        {
            if (_networkManager.LocalClient.IsSessionOwner)
            {
                Debug.Log($"[ConnectionManager] Client-{_networkManager.LocalClientId} is the session owner");
            }
        }

        private void OnClientConnectedCallback(ulong clientId)
        {
            if (_networkManager.LocalClientId == clientId)
            {
                Debug.Log($"[ConnectionManager] Client-{clientId} connected");
            }
        }

        private async void OnDestroy()
        {
            Debug.Log("[ConnectionManager] OnDestroy");

            if (_networkManager != null)
            {
                _networkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
                _networkManager.OnSessionOwnerPromoted -= OnSessionOwnerPromoted;
            }

            if (Session != null)
            {
                Debug.Log($"[ConnectionManager] OnDestroy: leaving session {Session.Id}");
                try { await Session.LeaveAsync(); }
                catch (Exception e) { Debug.LogWarning($"[ConnectionManager] Session leave on destroy failed: {e.Message}"); }
                Session = null;
            }

            if (_instance == this) _instance = null;
        }

        public async Task<bool> Authenticate(string profileName)
        {
            Debug.Log($"[ConnectionManager] Authenticate(profileName='{profileName}')");
            try
            {
                if (AuthenticationService.Instance.IsSignedIn && ProfileName == profileName)
                {
                    Debug.Log($"[ConnectionManager] Authenticate: already signed in as '{profileName}' (PlayerId={AuthenticationService.Instance.PlayerId})");
                    return true;
                }

                ProfileName = profileName;
                AuthenticationService.Instance.SwitchProfile(profileName);
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[ConnectionManager] Authentication succeeded (profile='{profileName}', PlayerId={AuthenticationService.Instance.PlayerId})");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConnectionManager] Authenticate failed (profile='{profileName}')");
                Debug.LogException(e);
                return false;
            }
        }
   
        public async Task CreateOrJoinSessionAsync(string profileName, string sessionName, Lobby lobby)
        {
            bool isLobbyHost = AuthenticationService.Instance.PlayerId == lobby.HostId;
            Debug.Log($"[ConnectionManager] CreateOrJoinSessionAsync(profile='{profileName}', sessionName='{sessionName}', isLobbyHost={isLobbyHost})");

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

                if (!isLobbyHost)
                {
                    Debug.Log("[ConnectionManager] Non-host: waiting 3s for host to create session");
                    await Task.Delay(3000);
                }

                Session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, options);
                Debug.Log($"[ConnectionManager] Session ready: id={Session.Id} name={sessionName}");

                State = ConnectionState.Connected;
            }
            catch (Exception e)
            {
                State = ConnectionState.Disconnected;
                Debug.LogError($"[ConnectionManager] CreateOrJoinSessionAsync failed (sessionName='{sessionName}')");
                Debug.LogException(e);
            }
        }

        public async Task JoinSessionByIdDirectAsync(string profileName, string sessionId)
        {
            Debug.Log($"[ConnectionManager] JoinSessionByIdDirectAsync(profile='{profileName}', sessionId='{sessionId}')");
            State = ConnectionState.Connecting;
            const int maxRetries = 3;
            const int retryDelayMs = 3000;

            int jitterMs = UnityEngine.Random.Range(100, 501);
            Debug.Log($"[ConnectionManager] JoinSessionByIdDirectAsync: jitter delay {jitterMs}ms before subscribing");
            await Task.Delay(jitterMs);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Debug.Log($"[ConnectionManager] JoinSessionById attempt {attempt}/{maxRetries} (sessionId='{sessionId}')");
                    ProfileName = profileName;

                    if (!AuthenticationService.Instance.IsSignedIn) await Authenticate(profileName);

                    Session = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);
                    Debug.Log($"[ConnectionManager] JoinSessionById succeeded: id={Session.Id}");

                    State = ConnectionState.Connected;
                    return;
                }
                catch (SessionException e) when (attempt < maxRetries)
                {
                    Debug.LogWarning($"[ConnectionManager] JoinSessionById attempt {attempt}/{maxRetries} failed: {e.Message}. Retrying in {retryDelayMs}ms...");
                    await CleanupPartialSessionAsync();
                    await Task.Delay(retryDelayMs);
                }
                catch (Exception e)
                {
                    // Non-retryable error or final attempt exhausted
                    State = ConnectionState.Disconnected;
                    Debug.LogError($"[ConnectionManager] JoinSessionById failed on attempt {attempt}/{maxRetries} (sessionId='{sessionId}')");
                    Debug.LogException(e);
                    await CleanupPartialSessionAsync();
                    return;
                }
            }

            State = ConnectionState.Disconnected;
            Debug.LogError($"[ConnectionManager] JoinSessionByIdDirectAsync: all retry attempts exhausted (sessionId='{sessionId}')");
        }

        public async Task CreateSessionAsync(string profileName, string sessionName)
        {
            Debug.Log($"[ConnectionManager] CreateSessionAsync(profile='{profileName}', sessionName='{sessionName}')");
            State = ConnectionState.Connecting;
            ProfileName = profileName;
            SessionName = sessionName;

            try
            {
                if (!AuthenticationService.Instance.IsSignedIn) await Authenticate(profileName);

                var options = new SessionOptions {
                    Name = sessionName,
                    MaxPlayers = _maxPlayers
                }.WithDistributedAuthorityNetwork();

                Session = await MultiplayerService.Instance.CreateSessionAsync(options);

                Session.CurrentPlayer.SetProperties(new Dictionary<string, PlayerProperty> {
                    { "DisplayName", new PlayerProperty(profileName, VisibilityPropertyOptions.Member) }
                });
                await Session.SaveCurrentPlayerDataAsync();

                Debug.Log($"[ConnectionManager] Session created: id={Session.Id} name={sessionName}");
                State = ConnectionState.Connected;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConnectionManager] CreateSessionAsync failed (sessionName='{sessionName}')");
                Debug.LogException(e);
                State = ConnectionState.Disconnected;
                await CleanupPartialSessionAsync();
            }
        }

        public async Task JoinSessionByNameAsync(string profileName, string sessionName)
        {
            Debug.Log($"[ConnectionManager] JoinSessionByNameAsync(profile='{profileName}', sessionName='{sessionName}')");
            State = ConnectionState.Connecting;
            const int maxRetries = 3;
            const int retryDelayMs = 3000;

            int jitterMs = UnityEngine.Random.Range(100, 501);
            Debug.Log($"[ConnectionManager] JoinSessionByNameAsync: jitter delay {jitterMs}ms before subscribing");
            await Task.Delay(jitterMs);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Debug.Log($"[ConnectionManager] JoinSessionByName attempt {attempt}/{maxRetries} (sessionName='{sessionName}')");
                    ProfileName = profileName;

                    if (!AuthenticationService.Instance.IsSignedIn) await Authenticate(profileName);

                    var query = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions {
                        FilterOptions = new List<FilterOption> {
                            new FilterOption(FilterField.Name, sessionName, FilterOperation.Equal)
                        }
                    });

                    if (query.Sessions == null || query.Sessions.Count == 0)
                    {
                        Debug.LogWarning($"[ConnectionManager] JoinSessionByName attempt {attempt}/{maxRetries}: no session found with name '{sessionName}'");
                        throw new SessionException("No session found with name " + sessionName, SessionError.None, null);
                    }

                    Session = await MultiplayerService.Instance.JoinSessionByIdAsync(query.Sessions[0].Id);

                    Session.CurrentPlayer.SetProperties(new Dictionary<string, PlayerProperty> {
                        { "DisplayName", new PlayerProperty(profileName, VisibilityPropertyOptions.Member) }
                    });
                    await Session.SaveCurrentPlayerDataAsync();

                    Debug.Log($"[ConnectionManager] JoinSessionByName succeeded: id={Session.Id} name={sessionName}");
                    State = ConnectionState.Connected;
                    return;
                }
                catch (SessionException e) when (attempt < maxRetries)
                {
                    Debug.LogWarning($"[ConnectionManager] JoinSessionByName attempt {attempt}/{maxRetries} failed: {e.Message}. Retrying in {retryDelayMs}ms...");
                    await CleanupPartialSessionAsync();
                    await Task.Delay(retryDelayMs);
                }
                catch (Exception e)
                {
                    State = ConnectionState.Disconnected;
                    Debug.LogError($"[ConnectionManager] JoinSessionByName failed on attempt {attempt}/{maxRetries} (sessionName='{sessionName}')");
                    Debug.LogException(e);
                    await CleanupPartialSessionAsync();
                    return;
                }
            }

            State = ConnectionState.Disconnected;
            Debug.LogError($"[ConnectionManager] JoinSessionByNameAsync: all retry attempts exhausted (sessionName='{sessionName}')");
        }

        public async Task DisconnectAsync()
        {
            Debug.Log("[ConnectionManager] DisconnectAsync");
            State = ConnectionState.Disconnected;
            await CleanupPartialSessionAsync();
        }

        private async Task CleanupPartialSessionAsync()
        {
            if (Session != null)
            {
                Debug.Log($"[ConnectionManager] CleanupPartialSession: leaving session {Session.Id}");
                try { await Session.LeaveAsync(); }
                catch (Exception cleanupEx) { Debug.LogWarning($"[ConnectionManager] Session cleanup failed: {cleanupEx.Message}"); }
                Session = null;
            }
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                Debug.Log("[ConnectionManager] CleanupPartialSession: shutting down NetworkManager");
                NetworkManager.Singleton.Shutdown();
                await Task.Delay(250);
            }
        }
    }
}