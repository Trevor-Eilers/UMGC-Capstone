using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UI;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "MainScene";
    public LobbyUI lobbyUI;
    private const int MaxPlayers = 4;

    private Lobby _lobby;
    private bool _isHost;
    private bool _gameStarting;

    private float _heartbeatTimer;
    private float _pollTimer;

    private const float HeartbeatInterval = 15f;
    private const float PollInterval = 2f;

    private void Awake()
    {
        lobbyUI = GetComponent<LobbyUI>();
        lobbyUI.SetVisible(false);
    }

    private void Update()
    {
        if (_lobby == null || _gameStarting) return;

        if (_isHost)
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
            _isHost = true;

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
            _isHost = false;

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
    
    private Player MakePlayer(string displayName)
    {
        return new Player(
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
        catch (LobbyServiceException e)
        {
            Debug.LogWarning($"Heartbeat failed: {e.Message}");
        }
    }

    private async void PollLobbyAsync()
    {
        try
        {
            _lobby = await LobbyService.Instance.GetLobbyAsync(_lobby.Id);
            RefreshUI();
            
            bool wasHost = _isHost;
            _isHost = _lobby.HostId == AuthenticationService.Instance.PlayerId;

            if (_isHost && !wasHost)
            {
                Debug.Log($"You are now the host.");
                lobbyUI.SetStartButtonVisible(true);
                lobbyUI.OnStartClicked += OnStartButtonClicked;
            }

            if (!_isHost) CheckForGameStart();
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
        if (!_isHost || _gameStarting) return;
        _gameStarting = true;

        try
        {
            var update = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    {
                        "GameStarted",
                        new DataObject(DataObject.VisibilityOptions.Member, "true")
                    }
                }
            };
            _lobby = await LobbyService.Instance.UpdateLobbyAsync(_lobby.Id, update);
            StartGame();
        }
        catch (Exception e)
        {
            _gameStarting = false;
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
            if (_isHost)
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
            _lobby = null;
            _isHost = false;
            _gameStarting = false;

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
            started.Value == "true")
        {
            _gameStarting = true;
            StartGame();
        }
    }

    private void StartGame()
    {
        // TODO: establish the Netcode session here or after scene load
        SceneManager.LoadScene(gameSceneName);
    }

    private async void OnDestroy()
    {
        try
        {
            lobbyUI.OnStartClicked -= OnStartButtonClicked;
            lobbyUI.OnLeaveClicked -= OnLeaveButtonClicked;
            await LeaveLobby();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}