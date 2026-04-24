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

        private int _synchronizedClients = 1;

        private async void Start()
        {
            try
            {
                lobbyUI = GetComponent<LobbyUI>();

                connectionManager = ConnectionManager.Instance;

                while (NetworkManager.Singleton == null || NetworkManager.Singleton.SceneManager == null)
                {
                    await Task.Delay(500);
                }
            
                NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += OnSynchronizeComplete;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void OnSynchronizeComplete(ulong clientId) => _synchronizedClients++;
        
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
                Debug.Log($"Lobby created: {_lobby.Id} - {_lobby.Name}");

                lobbyUI.SetStartButtonVisible(true);
                lobbyUI.SetLeaveButtonVisible(true);
                lobbyUI.OnStartClicked += OnStartButtonClicked;
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
                Debug.LogException(e);
            }
        }

        public async Task JoinLobby(string lobbyId, string playerName)
        {
            try
            {
                lobbyUI.SetVisible(true);
                isHost = false;

                var options = new JoinLobbyByIdOptions
                {
                    Player = MakePlayer(playerName)
                };

                _lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
                Debug.Log($"Joined lobby: {_lobby.Id} - {_lobby.Name}");

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
                Debug.LogException(e);
            }
        }

        public async Task JoinLobbyByName(string lobbyName, string playerName)
        {
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

                if (query.Results.Count == 0)
                {
                    Debug.LogWarning($"No lobby found with name: {lobbyName}");
                    lobbyUI.SetVisible(false);
                    return;
                }

                await JoinLobby(query.Results[0].Id, playerName);
            }
            catch (Exception e)
            {
                lobbyUI.SetVisible(false);
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
                Debug.LogWarning("Heartbeat: lobby no longer exists, clearing local state.");
                _lobby = null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Heartbeat failed: {e.Message}");
            }
        }

        private async void PollLobbyAsync()
        {
            try
            {
                _lobby = await LobbyService.Instance.GetLobbyAsync(_lobby.Id);
                if (_gameStarting) return;
                RefreshUI();
            
                bool wasHost = isHost;
                isHost = _lobby.HostId == AuthenticationService.Instance.PlayerId;

                if (isHost && !wasHost)
                {
                    Debug.Log($"You are now the host.");
                    lobbyUI.SetStartButtonVisible(true);
                    lobbyUI.OnStartClicked -= OnStartButtonClicked;
                    lobbyUI.OnStartClicked += OnStartButtonClicked;
                }

                if (!isHost) CheckForGameStart();
            }
            catch (LobbyServiceException e)
            {
                Debug.LogWarning($"Poll failed: {e.Message}");
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

            try
            {
                var sessionName = _lobby.Id + "_session";
                
                await connectionManager.CreateOrJoinSessionAsync(connectionManager.ProfileName, sessionName, _lobby);

                if (connectionManager.State != ConnectionState.Connected)
                {
                    Debug.LogError("Failed to establish Netcode session.");
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

                StartCoroutine(WaitForPlayersAndLoadScene());
            }
            catch (Exception e)
            {
                _gameStarting = false;
                await connectionManager.DisconnectAsync();
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

            try
            {
                if (isHost)
                {
                    // Get current player list
                    _lobby = await LobbyService.Instance.GetLobbyAsync(_lobby.Id);

                    if (_lobby.Players.Count > 1)
                    {
                        // Find the first player who isn't us
                        string newHostId = null;
                        foreach (var player in _lobby.Players)
                        {
                            if (player.Id != AuthenticationService.Instance.PlayerId)
                            {
                                newHostId = player.Id;
                                break;
                            }
                        }

                        // Transfer ownership, then remove ourselves
                        await LobbyService.Instance.UpdateLobbyAsync(_lobby.Id, new UpdateLobbyOptions
                        {
                            HostId = newHostId
                        });

                        await LobbyService.Instance.RemovePlayerAsync(
                            _lobby.Id, AuthenticationService.Instance.PlayerId);
                    }
                    else
                    {
                        // If we are the last player, delete the lobby
                        await LobbyService.Instance.DeleteLobbyAsync(_lobby.Id);
                    }
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(
                        _lobby.Id, AuthenticationService.Instance.PlayerId);
                }
            }
            catch (LobbyServiceException e)
            {
                Debug.LogWarning($"Leave lobby failed: {e.Message}");
            }
            finally
            {
                if (connectionManager != null && connectionManager.State != ConnectionState.Disconnected)
                    await connectionManager.DisconnectAsync();

                _lobby = null;
                isHost = false;
                _gameStarting = false;
                _synchronizedClients = 1;

                lobbyUI.SetVisible(false);
                lobbyUI.SetConnected(false);
                lobbyUI.SetStartButtonVisible(false);
                lobbyUI.SetLeaveButtonVisible(false);
                lobbyUI.SetPlayerList(new List<string>());
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
                await connectionManager.JoinSessionByIdDirectAsync(connectionManager.ProfileName, netcodeSessionId);

                if (connectionManager.State != ConnectionState.Connected)
                {
                    Debug.LogError("Failed to establish Netcode session.");
                    _gameStarting = false;
                    await connectionManager.DisconnectAsync();
                    return;
                }
            }
            catch (Exception e)
            {
                _gameStarting = false;
                await connectionManager.DisconnectAsync();
                Debug.LogException(e);
            }
        }

        private IEnumerator WaitForPlayersAndLoadScene()
        {
            var waitOneSecond = new WaitForSeconds(1f);
            float elapsed = 0f;

            while (connectionManager.Session.PlayerCount != _lobby.Players.Count)
            {
                if (elapsed >= maxWaitSeconds)
                {
                    Debug.LogError($"Timed out waiting for clients to connect ({connectionManager.Session.PlayerCount}/{_lobby.Players.Count}).");
                    _gameStarting = false;
                    _ = connectionManager.DisconnectAsync();
                    yield break;
                }
                Debug.Log("Not all clients have connected. Delaying...");
                yield return waitOneSecond;
                elapsed += 1f;
            }

            elapsed = 0f;
            while (_synchronizedClients != _lobby.Players.Count)
            {
                if (elapsed >= maxWaitSeconds)
                {
                    Debug.LogError($"Timed out waiting for clients to synchronize ({_synchronizedClients}/{_lobby.Players.Count}).");
                    _gameStarting = false;
                    _ = connectionManager.DisconnectAsync();
                    yield break;
                }
                Debug.Log("Not all clients have synchronized. Delaying...");
                yield return waitOneSecond;
                elapsed += 1f;
            }

            if (connectionManager.Session.IsHost)
            {
                Debug.Log("Loading scene");
                NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
            }
        }
        

        private async void OnDestroy()
        {
            try
            {
                lobbyUI.OnStartClicked -= OnStartButtonClicked;
                lobbyUI.OnLeaveClicked -= OnLeaveButtonClicked;

                if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
                    NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= OnSynchronizeComplete;

                if (!_gameStarting) await LeaveLobby();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}