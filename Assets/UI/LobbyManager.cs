using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UI;

public class LobbyManager : NetworkBehaviour
{
    private NetworkList<FixedString64Bytes> _playerNames;

    [SerializeField] private string gameSceneName = "MainScene";
    [SerializeField] private LobbyUI lobbyUI;

    private void Awake()
    {
        _playerNames = new NetworkList<FixedString64Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );
        
        lobbyUI = GetComponent<LobbyUI>();
    }

    public override void OnNetworkSpawn()
    {
        _playerNames.OnListChanged += OnPlayerListChanged;
        lobbyUI.SetStartButtonVisible(IsSessionOwner);
        lobbyUI.OnStartClicked += OnStartButtonClicked;
        
        NetworkManager.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

        lobbyUI.SetConnected(false);
        
        // Submit our own name now that the network is ready
        var connectionManager = FindFirstObjectByType<ConnectionManager>();
        string profileName = connectionManager != null
            ? connectionManager.ProfileName
            : $"Player {NetworkManager.LocalClientId}";
        SubmitNameRpc(profileName);

        RefreshUI();
    }

    public override void OnNetworkDespawn()
    {
        _playerNames.OnListChanged -= OnPlayerListChanged;
        lobbyUI.OnStartClicked -= OnStartButtonClicked;
        NetworkManager.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        // Each client submits their own name when they connect;
        // nothing needed here from the session owner's side
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsSessionOwner) return;
        // Find and remove by a placeholder — see note below
        RemovePlayerRpc(clientId);
    }

    // Every client calls this on themselves — session owner appends to the list
    [Rpc(SendTo.Authority)]
    private void SubmitNameRpc(string displayName)
    {
        _playerNames.Add(new FixedString64Bytes(displayName));
    }

    [Rpc(SendTo.Authority)]
    private void RemovePlayerRpc(ulong clientId)
    {
        // Without a clientId→index map this is still a stub.
        // See note below about tracking identity.
    }

    private void OnPlayerListChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
    {
        lobbyUI.SetConnected(_playerNames.Count > 0);
        RefreshUI();
    }

    private void RefreshUI()
    {
        var names = new List<string>();
        foreach (var name in _playerNames)
            names.Add(name.ToString());
        lobbyUI.SetPlayerList(names);
    }

    private void OnStartButtonClicked()
    {
        if (!IsSessionOwner) return;
        StartGameRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void StartGameRpc()
    {
        NetworkManager.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }
}