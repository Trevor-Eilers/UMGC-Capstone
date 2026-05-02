// Author: Trevor Eilers

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UI;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Network
{
    public class LobbyManager : MonoBehaviour
    {
        [SerializeField]
        private string gameSceneName = "MainScene";

        [SerializeField]
        private ConnectionManager connectionManager;

        public LobbyUI lobbyUI;

        private ISession _session;

        [SerializeField]
        [ReadOnly]
        private bool isHost;

        private async void Start()
        {
            Debug.Log("[LobbyManager] Start");
            try
            {
                lobbyUI = GetComponent<LobbyUI>();

                connectionManager = ConnectionManager.Instance;

                bool waitedForNetworkManager = false;
                while (NetworkManager.Singleton == null || NetworkManager.Singleton.SceneManager == null)
                {
                    if (!waitedForNetworkManager)
                    {
                        Debug.Log("[LobbyManager] Start: waiting for NetworkManager.Singleton.SceneManager");
                        waitedForNetworkManager = true;
                    }
                    await Task.Delay(500);
                }

                Debug.Log("[LobbyManager] Start: ready");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public async Task CreateLobby(string lobbyName, string playerName)
        {
            Debug.Log($"[LobbyManager] CreateLobby(lobbyName='{lobbyName}', playerName='{playerName}')");
            if (_session != null)
            {
                Debug.LogWarning($"[LobbyManager] CreateLobby ignored: already in session {_session.Id}");
                return;
            }

            try
            {
                lobbyUI.SetVisible(true);
                isHost = true;

                await connectionManager.CreateSessionAsync(playerName, lobbyName);

                if (connectionManager.State != ConnectionState.Connected)
                {
                    Debug.LogError("[LobbyManager] CreateLobby: failed to connect (state not Connected)");
                    lobbyUI.SetVisible(false);
                    isHost = false;
                    return;
                }

                _session = connectionManager.Session;

                _session.PlayerJoined += OnPlayerJoined;
                _session.PlayerHasLeft += OnPlayerHasLeft;
                _session.RemovedFromSession += OnRemovedFromSession;
                _session.SessionHostChanged += OnSessionHostChanged;
                _session.Changed += OnSessionChanged;

                lobbyUI.SetStartButtonVisible(true);
                lobbyUI.SetLeaveButtonVisible(true);
                lobbyUI.OnStartClicked -= OnStartButtonClicked;
                lobbyUI.OnStartClicked += OnStartButtonClicked;
                lobbyUI.OnLeaveClicked -= OnLeaveButtonClicked;
                lobbyUI.OnLeaveClicked += OnLeaveButtonClicked;
                lobbyUI.SetConnected(true);
                RefreshUI();
            }
            catch (Exception e)
            {
                lobbyUI.SetVisible(false);
                lobbyUI.SetStartButtonVisible(false);
                lobbyUI.SetLeaveButtonVisible(false);
                lobbyUI.SetConnected(false);
                isHost = false;
                Debug.LogError($"[LobbyManager] CreateLobby failed (lobbyName='{lobbyName}')");
                Debug.LogException(e);
            }
        }

        public async Task JoinLobbyByName(string lobbyName, string playerName)
        {
            Debug.Log($"[LobbyManager] JoinLobbyByName(lobbyName='{lobbyName}', playerName='{playerName}')");
            if (_session != null)
            {
                Debug.LogWarning($"[LobbyManager] JoinLobbyByName ignored: already in session {_session.Id}");
                return;
            }

            lobbyUI.SetVisible(true);
            isHost = false;

            await connectionManager.JoinSessionByNameAsync(playerName, lobbyName);

            if (connectionManager.State != ConnectionState.Connected)
            {
                Debug.LogWarning($"[LobbyManager] JoinLobbyByName: failed to connect (lobbyName='{lobbyName}')");
                lobbyUI.SetConnected(false);
                lobbyUI.SetVisible(false);
                return;
            }

            _session = connectionManager.Session;

            _session.PlayerJoined += OnPlayerJoined;
            _session.PlayerHasLeft += OnPlayerHasLeft;
            _session.RemovedFromSession += OnRemovedFromSession;
            _session.SessionHostChanged += OnSessionHostChanged;
            _session.Changed += OnSessionChanged;

            lobbyUI.SetStartButtonVisible(false);
            lobbyUI.SetLeaveButtonVisible(true);
            lobbyUI.OnLeaveClicked -= OnLeaveButtonClicked;
            lobbyUI.OnLeaveClicked += OnLeaveButtonClicked;
            lobbyUI.SetConnected(true);
            RefreshUI();
        }

        private void RefreshUI()
        {
            var names = new List<string>();
            if (_session != null)
            {
                foreach (var player in _session.Players)
                {
                    string name = null;
                    if (player.Properties != null && player.Properties.TryGetValue("DisplayName", out var prop))
                    {
                        name = prop?.Value;
                    }
                    names.Add(string.IsNullOrEmpty(name) ? "Connecting..." : name);
                }
            }
            lobbyUI.SetPlayerList(names);
        }

        private void OnPlayerJoined(string playerId)
        {
            Debug.Log($"[LobbyManager] OnPlayerJoined: playerId={playerId}");
            RefreshUI();
        }

        private void OnSessionChanged()
        {
            RefreshUI();
        }

        private void OnPlayerHasLeft(string playerId)
        {
            Debug.Log($"[LobbyManager] OnPlayerHasLeft: playerId={playerId}");
            RefreshUI();
        }

        private void OnRemovedFromSession()
        {
            Debug.Log("[LobbyManager] OnRemovedFromSession: removed from session");
            _ = LeaveLobby();
        }

        private void OnSessionHostChanged(string newHostId)
        {
            Debug.Log($"[LobbyManager] OnSessionHostChanged: newHostId={newHostId}");
            if (_session != null && _session.IsHost && !isHost)
            {
                isHost = true;
                lobbyUI.SetStartButtonVisible(true);
                lobbyUI.OnStartClicked -= OnStartButtonClicked;
                lobbyUI.OnStartClicked += OnStartButtonClicked;
            }
        }

        private void OnStartButtonClicked()
        {
            if (!isHost) return;
            Debug.Log("[LobbyManager] OnStartButtonClicked: loading scene");
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }

        private void OnLeaveButtonClicked()
        {
            _ = LeaveLobby();
        }

        public async Task LeaveLobby()
        {
            if (_session == null) return;
            string sessionId = _session.Id;
            Debug.Log($"[LobbyManager] LeaveLobby (session={sessionId}, isHost={isHost})");

            _session.PlayerJoined -= OnPlayerJoined;
            _session.PlayerHasLeft -= OnPlayerHasLeft;
            _session.RemovedFromSession -= OnRemovedFromSession;
            _session.SessionHostChanged -= OnSessionHostChanged;
            _session.Changed -= OnSessionChanged;

            try { await _session.LeaveAsync(); }
            catch (Exception e) { Debug.LogWarning($"[LobbyManager] LeaveAsync failed: {e.Message}"); }

            if (connectionManager != null && connectionManager.State != ConnectionState.Disconnected)
            {
                await connectionManager.DisconnectAsync();
            }

            _session = null;
            isHost = false;

            lobbyUI.SetVisible(false);
            lobbyUI.SetConnected(false);
            lobbyUI.SetStartButtonVisible(false);
            lobbyUI.SetLeaveButtonVisible(false);
            lobbyUI.SetPlayerList(new List<string>());
        }

        private void OnDestroy()
        {
            var validSession = _session != null;
            Debug.Log($"[LobbyManager] OnDestroy (hasSession={validSession})");
            try
            {
                if (validSession) _session.Changed -= OnSessionChanged;
                if (lobbyUI != null)
                {
                    lobbyUI.OnStartClicked -= OnStartButtonClicked;
                    lobbyUI.OnLeaveClicked -= OnLeaveButtonClicked;
                }
            }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
