// Author: Trevor Eilers

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UI;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;


namespace Network
{
    public class LobbyManager : MonoBehaviour
    {
        [SerializeField] 
        private string gameSceneName = "MainScene";
        
        [SerializeField]
        private ConnectionManager connectionManager;

        [SerializeField]
        private float maxWaitSeconds = 30f;
        
        public LobbyUI lobbyUI;
        
        private const int MaxPlayers = 4;

        private Lobby _lobby;
        
        [SerializeField]
        [ReadOnly]
        private bool isHost;
        
        private bool _gameStarting;

        private float _heartbeatTimer;
        private float _pollTimer;
        private const float HeartbeatInterval = 15f;
        private const float PollInterval = 2.5f;

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

        private void Update()
        {
            if (_lobby == null || _gameStarting) return;
        
            if (isHost)
            {
                _heartbeatTimer -= Time.deltaTime;
                if (_heartbeatTimer <= 0f)
                {
                    _heartbeatTimer = HeartbeatInterval;
                    SendHeartbeatAsync();
                }
            }

            _pollTimer -= Time.deltaTime;
            if (_pollTimer <= 0f)
            {
                _pollTimer = PollInterval;
                PollLobbyAsync();
            }
        }

        public async Task CreateLobby(string lobbyName, string playerName)
        {
            Debug.Log($"[LobbyManager] CreateLobby(lobbyName='{lobbyName}', playerName='{playerName}')");
            if (_lobby != null)
            {
                Debug.LogWarning($"[LobbyManager] CreateLobby ignored: already in lobby {_lobby.Id}");
                return;
            }

            try
            {
                lobbyUI.SetVisible(true);
                isHost = true;

                var options = new CreateLobbyOptions
                {
                    IsPrivate = false,
                    Player = MakePlayer(playerName)
                };

                _lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MaxPlayers, options);
                Debug.Log($"[LobbyManager] Lobby created: id={_lobby.Id} name={_lobby.Name}");

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
                Debug.LogError($"[LobbyManager] CreateLobby failed (lobbyName='{lobbyName}')");
                Debug.LogException(e);
            }
        }

        public async Task JoinLobby(string lobbyId, string playerName)
        {
            Debug.Log($"[LobbyManager] JoinLobby(lobbyId='{lobbyId}', playerName='{playerName}')");
            if (_lobby != null)
            {
                Debug.LogWarning($"[LobbyManager] JoinLobby ignored: already in lobby {_lobby.Id}");
                return;
            }

            try
            {
                lobbyUI.SetVisible(true);
                isHost = false;

                var options = new JoinLobbyByIdOptions
                {
                    Player = MakePlayer(playerName)
                };

                _lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
                Debug.Log($"[LobbyManager] Joined lobby: id={_lobby.Id} name={_lobby.Name} players={_lobby.Players.Count}");

                lobbyUI.SetStartButtonVisible(false);
                lobbyUI.SetLeaveButtonVisible(true);
                lobbyUI.OnLeaveClicked += OnLeaveButtonClicked;
                lobbyUI.SetConnected(true);
                RefreshUI();
            }
            catch (Exception e)
            {
                lobbyUI.SetVisible(false);
                lobbyUI.SetLeaveButtonVisible(false);
                lobbyUI.SetConnected(false);
                Debug.LogError($"[LobbyManager] JoinLobby failed (lobbyId='{lobbyId}')");
                Debug.LogException(e);
            }
        }

        public async Task JoinLobbyByName(string lobbyName, string playerName)
        {
            Debug.Log($"[LobbyManager] JoinLobbyByName(lobbyName='{lobbyName}', playerName='{playerName}')");
            try
            {
                lobbyUI.SetVisible(true);
                var query = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
                {
                    Filters = new List<QueryFilter>
                    {
                        new(QueryFilter.FieldOptions.Name, lobbyName, QueryFilter.OpOptions.EQ)
                    }
                });

                Debug.Log($"[LobbyManager] JoinLobbyByName: query returned {query.Results.Count} result(s)");

                if (query.Results.Count == 0)
                {
                    Debug.LogWarning($"[LobbyManager] No lobby found with name: '{lobbyName}'");
                    lobbyUI.SetVisible(false);
                    return;
                }

                Debug.Log($"[LobbyManager] JoinLobbyByName: selecting lobby id={query.Results[0].Id}");
                await JoinLobby(query.Results[0].Id, playerName);
            }
            catch (Exception e)
            {
                lobbyUI.SetVisible(false);
                Debug.LogError($"[LobbyManager] JoinLobbyByName failed (lobbyName='{lobbyName}')");
                Debug.LogException(e);
            }
        }
    
        private Unity.Services.Lobbies.Models.Player MakePlayer(string displayName)
        {
            return new Unity.Services.Lobbies.Models.Player(
                id: AuthenticationService.Instance.PlayerId,
                data: new Dictionary<string, PlayerDataObject>
                {
                    {
                        "DisplayName",
                        new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, displayName)
                    }
                }
            );
        }

        private async void SendHeartbeatAsync()
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(_lobby.Id);
            }
            catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                Debug.LogWarning("[LobbyManager] Heartbeat: lobby no longer exists");
                await LeaveLobby();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LobbyManager] Heartbeat failed: {e.Message}");
            }
        }

        private async void PollLobbyAsync()
        {
            int previousPlayerCount = _lobby?.Players.Count ?? 0;
            bool previousGameStarted = _lobby?.Data != null
                && _lobby.Data.TryGetValue("GameStarted", out var prevStarted)
                && prevStarted.Value == "true";

            try
            {
                _lobby = await LobbyService.Instance.GetLobbyAsync(_lobby.Id);
                if (_gameStarting) return;
                RefreshUI();

                if (_lobby.Players.Count != previousPlayerCount)
                {
                    Debug.Log($"[LobbyManager] Lobby roster changed: {previousPlayerCount} -> {_lobby.Players.Count} (lobby={_lobby.Id})");
                }

                bool wasHost = isHost;
                isHost = _lobby.HostId == AuthenticationService.Instance.PlayerId;

                if (isHost && !wasHost)
                {
                    Debug.Log($"[LobbyManager] Host promotion: PlayerId={AuthenticationService.Instance.PlayerId} (lobby={_lobby.Id})");
                    lobbyUI.SetStartButtonVisible(true);
                    lobbyUI.OnStartClicked -= OnStartButtonClicked;
                    lobbyUI.OnStartClicked += OnStartButtonClicked;
                }

                bool gameStarted = _lobby.Data != null
                    && _lobby.Data.TryGetValue("GameStarted", out var started)
                    && started.Value == "true";
                if (gameStarted && !previousGameStarted)
                {
                    Debug.Log($"[LobbyManager] Poll detected GameStarted=true (lobby={_lobby.Id})");
                }

                if (!isHost) CheckForGameStart();
            }
            catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                Debug.LogWarning($"[LobbyManager] Poll: lobby no longer exists (id={_lobby?.Id}): {e.Message}");
                await LeaveLobby();
            }
            catch (LobbyServiceException e)
            {
                Debug.LogWarning($"[LobbyManager] Poll failed: {e.Message}");
            }

        }

        private void RefreshUI()
        {
            var names = new List<string>();
            foreach (var player in _lobby.Players)
            {
                if (player.Data != null &&
                    player.Data.TryGetValue("DisplayName", out var nameData))
                    names.Add(nameData.Value);
                else
                    names.Add(player.Id);
            }
            lobbyUI.SetPlayerList(names);
        }

        private async void OnStartButtonClicked()
        {
            if (!isHost || _gameStarting) return;
            _gameStarting = true;
            Debug.Log($"[LobbyManager] OnStartButtonClicked (lobby={_lobby.Id}, players={_lobby.Players.Count})");

            try
            {
                var sessionName = _lobby.Id + "_session" + Random.Range(0, int.MaxValue);
                Debug.Log($"[LobbyManager] Start: creating session '{sessionName}'");

                await connectionManager.CreateOrJoinSessionAsync(connectionManager.ProfileName, sessionName, _lobby);

                if (connectionManager.State != ConnectionState.Connected)
                {
                    Debug.LogError("[LobbyManager] Start: failed to establish Netcode session");
                    _gameStarting = false;
                    await connectionManager.DisconnectAsync();
                    return;
                }

                _lobby = await LobbyService.Instance.GetLobbyAsync(_lobby.Id);

                var netcodeSessionId = connectionManager.Session.Id;
                var update = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { "GameStarted", new DataObject(DataObject.VisibilityOptions.Member, "true") },
                        { "NetcodeSessionId", new DataObject(DataObject.VisibilityOptions.Member, netcodeSessionId) }
                    }
                };
                _lobby = await LobbyService.Instance.UpdateLobbyAsync(_lobby.Id, update);
                Debug.Log($"[LobbyManager] Start: lobby updated with NetcodeSessionId={netcodeSessionId}");

                Debug.Log("[LobbyManager] Start: entering WaitForPlayersAndLoadScene");
                StartCoroutine(WaitForPlayersAndLoadScene());
            }
            catch (Exception e)
            {
                _gameStarting = false;
                await connectionManager.DisconnectAsync();
                Debug.LogError("[LobbyManager] OnStartButtonClicked failed");
                Debug.LogException(e);
            }
        }
        
        private async void OnLeaveButtonClicked()
        {
            await LeaveLobby();
        }
    
        public async Task LeaveLobby()
        {
            if (_lobby == null) return;
            string lobbyId = _lobby.Id;
            string myPlayerId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"[LobbyManager] LeaveLobby (lobby={lobbyId}, isHost={isHost})");

            try
            {
                if (isHost)
                {
                    // Get current player list
                    _lobby = await LobbyService.Instance.GetLobbyAsync(_lobby.Id);

                    if (_lobby?.Players.Count > 1)
                    {
                        // Find the first player who isn't us
                        string newHostId = null;
                        foreach (var player in _lobby.Players)
                        {
                            if (player.Id != myPlayerId)
                            {
                                newHostId = player.Id;
                                break;
                            }
                        }

                        Debug.Log($"[LobbyManager] LeaveLobby: host transfer {myPlayerId} -> {newHostId} (lobby={lobbyId})");
                        await LobbyService.Instance.UpdateLobbyAsync(_lobby.Id, new UpdateLobbyOptions
                        {
                            HostId = newHostId
                        });

                        await LobbyService.Instance.RemovePlayerAsync(_lobby.Id, myPlayerId);
                        Debug.Log($"[LobbyManager] LeaveLobby: removed self after transfer (lobby={lobbyId})");
                    }
                    else
                    {
                        Debug.Log($"[LobbyManager] LeaveLobby: last player, deleting lobby {lobbyId}");
                        await LobbyService.Instance.DeleteLobbyAsync(_lobby.Id);
                    }
                }
                else
                {
                    Debug.Log($"[LobbyManager] LeaveLobby: removing self (non-host) (lobby={lobbyId})");
                    await LobbyService.Instance.RemovePlayerAsync(_lobby.Id, myPlayerId);
                }
            }
            catch (LobbyServiceException e)
            {
                Debug.LogWarning($"[LobbyManager] LeaveLobby failed (lobby={lobbyId}): {e.Message}");
            }
            finally
            {
                if (connectionManager != null && connectionManager.State != ConnectionState.Disconnected)
                    await connectionManager.DisconnectAsync();

                _lobby = null;
                isHost = false;
                _gameStarting = false;
               

                lobbyUI.SetVisible(false);
                lobbyUI.SetConnected(false);
                lobbyUI.SetStartButtonVisible(false);
                lobbyUI.SetLeaveButtonVisible(false);
                lobbyUI.SetPlayerList(new List<string>());
                Debug.Log($"[LobbyManager] LeaveLobby: cleanup complete (lobby={lobbyId})");
            }
        }

        private void CheckForGameStart()
        {
            if (_lobby.Data != null &&
                _lobby.Data.TryGetValue("GameStarted", out var started) &&
                started.Value == "true" &&
                _lobby.Data.TryGetValue("NetcodeSessionId", out _))
            {
                _gameStarting = true;
                StartGame();
            }
        }

        private async void StartGame()
        {
            try
            {
                var netcodeSessionId = _lobby.Data["NetcodeSessionId"].Value;
                Debug.Log($"[LobbyManager] StartGame (NetcodeSessionId={netcodeSessionId}, lobby={_lobby.Id})");
                await connectionManager.JoinSessionByIdDirectAsync(connectionManager.ProfileName, netcodeSessionId);

                if (connectionManager.State != ConnectionState.Connected)
                {
                    Debug.LogError(
                        $"[LobbyManager] StartGame: failed to establish Netcode session (state='{connectionManager.State}')");
                    _gameStarting = false;
                    await connectionManager.DisconnectAsync();
                    return;
                }
                Debug.Log("[LobbyManager] StartGame: session joined, awaiting host scene load");
            }
            catch (Exception e)
            {
                _gameStarting = false;
                await connectionManager.DisconnectAsync();
                Debug.LogError("[LobbyManager] StartGame failed");
                Debug.LogException(e);
            }
        }

        private IEnumerator WaitForPlayersAndLoadScene()
        {
            var wait = new WaitForSeconds(1.5f);
            float elapsed = 0f;
            int total = _lobby.Players.Count;
            Debug.Log($"[LobbyManager] WaitForPlayers: waiting for {total} clients to connect");

            while (connectionManager.Session.PlayerCount != _lobby.Players.Count)
            {
                if (elapsed >= maxWaitSeconds)
                {
                    Debug.LogError($"[LobbyManager] Timed out waiting for clients to connect: {connectionManager.Session.PlayerCount}/{_lobby.Players.Count} after {elapsed:0.0}s");
                    _gameStarting = false;
                    _ = connectionManager.DisconnectAsync();
                    yield break;
                }
                Debug.Log($"[LobbyManager] Waiting for clients: connected={connectionManager.Session.PlayerCount}/{_lobby.Players.Count} elapsed={elapsed:0.0}s");
                yield return wait;
                elapsed += 1.5f;
            }
            Debug.Log($"[LobbyManager] WaitForPlayers: all {total} clients connected ({elapsed:0.0}s)");

            elapsed = 0f;
            Debug.Log($"[LobbyManager] WaitForNGOClients: waiting for {total} NGO clients to connect");
            while (NetworkManager.Singleton.ConnectedClients.Count < _lobby.Players.Count)
            {
                if (elapsed >= maxWaitSeconds)
                {
                    Debug.LogError($"[LobbyManager] Timed out waiting for NGO clients: {NetworkManager.Singleton.ConnectedClients.Count}/{_lobby.Players.Count} after {elapsed:0.0}s");
                    _gameStarting = false;
                    _ = connectionManager.DisconnectAsync();
                    LobbyService.Instance.DeleteLobbyAsync(_lobby.Id);
                    _lobby = null;
                    yield break;
                }
                Debug.Log($"[LobbyManager] Waiting for NGO clients: connected={NetworkManager.Singleton.ConnectedClients.Count}/{_lobby.Players.Count} elapsed={elapsed:0.0}s");
                yield return wait;
                elapsed += 1.5f;
            }
            Debug.Log($"[LobbyManager] All {total} NGO clients connected ({elapsed:0.0}s)");

            if (connectionManager.Session.IsHost)
            {
                Debug.Log($"[LobbyManager] Loading scene '{gameSceneName}' as session host");
                NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
            }
        }
        

        private async void OnDestroy()
        {
            Debug.Log($"[LobbyManager] OnDestroy (gameStarting={_gameStarting}, hasLobby={_lobby != null})");
            try
            {
                lobbyUI.OnStartClicked -= OnStartButtonClicked;
                lobbyUI.OnLeaveClicked -= OnLeaveButtonClicked;

                if (!_gameStarting) await LeaveLobby();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}