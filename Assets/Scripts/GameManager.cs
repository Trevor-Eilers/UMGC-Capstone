// Authors: Malcolm Bramble, Trevor Eilers

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Network;
using Simulation;
using UnityEngine;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    public NetworkVariable<GameState> GameState = new();
    private float _tickTimer = 0f;
    private bool _tickReady = false;
    private bool _resolvingTick = false;
    private bool _gameOver =  false;
    private readonly float _tickInterval = 2f;

    private ConnectionManager _connectionManager;
    
    private int _tickReadyCounter = 0;
    
    private int _initializationCounter = 0;
    
    private readonly NetworkList<NetworkObjectReference> _players = new();
    
    private Player _localPlayer;

    public static GameManager Instance { get; private set; }

    
    public override void OnNetworkSpawn()
    {
        Instance = this;

        if (HasAuthority)
        {
            var state = new GameState();
            state.Default();
            GameState.Value = state;

            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        StartCoroutine(Initialize());
    }


    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }


    private void OnClientDisconnected(ulong clientId)
    {
        if (!HasAuthority) return;

        for (int i = _players.Count - 1; i >= 0; i--)
        {
            bool resolved = _players[i].TryGet(out NetworkObject netObj);
            if (!resolved || netObj == null || netObj.OwnerClientId == clientId)
            {
                _players.RemoveAt(i);
            }
        }
    }


    private List<Player> GetActivePlayers()
    {
        var active = new List<Player>(_players.Count);
        for (int i = 0; i < _players.Count; i++)
        {
            if (!_players[i].TryGet(out NetworkObject networkObject)) continue;
            if (networkObject == null) continue;
            var player = networkObject.GetComponent<Player>();
            if (player == null || player.District == null) continue;
            active.Add(player);
        }
        return active;
    }


    private IEnumerator Initialize()
    {
        _connectionManager = FindFirstObjectByType<ConnectionManager>();
        
        while (_localPlayer == null)
        {
            var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.IsOwner) _localPlayer = player;
            }

            if (_localPlayer == null) yield return new WaitForSeconds(0.1f);
        }

        SignalInitializeRpc();

        if (HasAuthority)
        {
            while (_initializationCounter < _connectionManager.PlayerCount)
            {
                Debug.Log("Waiting for clients");
                yield return new WaitForSeconds(1.5f);
            }

            var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                _players.Add(players[i].NetworkObject);

                var districtObject = Instantiate(Resources.Load<GameObject>("District"));
                var networkObject = districtObject.GetComponent<NetworkObject>();
                networkObject.SpawnWithOwnership(NetworkManager.Singleton.ConnectedClientsIds[i], true);
            }

            var gameState = GameState.Value;
            gameState.isPaused = false;
            GameState.Value = gameState;

            Debug.Log("Initialization complete.");
        }
    }
    
    
    void Update()
    {
        if (HasAuthority)
        {
            if (_tickReadyCounter >= _connectionManager.PlayerCount)
            {
                ResolveTickRpc();
                return;
            }
        }
        
        if (_tickReady || GameState.Value.isPaused || _gameOver) return;

        _tickTimer += Time.deltaTime;
        
        if (_tickTimer >= _tickInterval / GameState.Value.gameSpeed)
        {
            _tickTimer = 0;
            _tickReady = true;
            SignalTickReadyRpc();
        }
    }

    
    [Rpc(SendTo.Everyone)]
    private void ResolveTickRpc()
    {
        Debug.Log("Tick advancing");
        
        _tickReadyCounter = 0;
        _tickReady = false;
        _resolvingTick = true;

        UpdatePolicies();

        if (!HasAuthority) return;

        var activePlayers = GetActivePlayers();
        var districtStates = new DistrictState[activePlayers.Count];
        for (int i = 0; i < activePlayers.Count; i++)
        {
            districtStates[i] = activePlayers[i].District.state.Value;
        }

        // Host resolves city-wide metrics
        var gameState = GameState.Value;
        gameState.cityMetrics = TickProcessor.ResolveCityMetrics(districtStates, gameState.cityMetrics);
        gameState.currentTick++;
        gameState.currentMonth = gameState.currentTick / SimulationConstants.TICKS_PER_MONTH;
        GameState.Value = gameState;

        if (gameState.currentTick >= SimulationConstants.TOTAL_TICKS)
        {
            _gameOver = true;
        }
        
        ResolveDistrictTickRpc(districtStates, gameState.cityMetrics);
    }

    
    [Rpc(SendTo.Everyone)]
    private void ResolveDistrictTickRpc(DistrictState[] districtStates, CityMetrics cityMetrics)
    {
        var activePlayers = GetActivePlayers();
        int localIndex = activePlayers.IndexOf(_localPlayer);

        if (localIndex >= 0 && localIndex < districtStates.Length)
        {
            districtStates[localIndex].policyValues = _localPlayer.CurrentPolicies;

            var result = TickProcessor.ResolveDistrictTick(
                localIndex, districtStates, cityMetrics);
            _localPlayer.District.state.Value = result;
        }

        _resolvingTick = false;
    }

    
    private void UpdatePolicies()
    {
        var districtState = _localPlayer.District.state.Value;
        districtState.policyValues = _localPlayer.CurrentPolicies;
        _localPlayer.District.state.Value = districtState;
    }

    
    [Rpc(SendTo.Authority)]
    private void SignalTickReadyRpc()
    {
        _tickReadyCounter++;
        Debug.Log($"Ready signals received: {_tickReadyCounter}");
    }

    
    [Rpc(SendTo.Authority)]
    private void SignalInitializeRpc()
    {
        _initializationCounter++;
        if (HasAuthority) Debug.Log($"Initialization signals received: {_initializationCounter}");
    }

    
    [Rpc(SendTo.Authority)]
    public void RequestSetSpeedRpc(int speed)
    {
        var state = GameState.Value;
        state.gameSpeed = speed;
        state.isPaused = false;
        GameState.Value = state;
    }

    
    [Rpc(SendTo.Authority)]
    public void RequestSetPauseRpc(bool paused)
    {
        var state = GameState.Value;
        state.isPaused = paused;
        if (paused) state.gameSpeed = 0;
        GameState.Value = state;
    }

    
    [Rpc(SendTo.Authority)]
    public void RequestQuitRpc(ulong networkObjectId, RpcParams rpcParams = default)
    {
        if (!HasAuthority) return;
        try
        {
            StartCoroutine(RemovePlayer(networkObjectId));
            ConfirmQuitRpc(RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }


    private IEnumerator RemovePlayer(ulong networkObjectId)
    {
        if (!HasAuthority) yield break;

        while (_resolvingTick) yield return null;
        
        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].TryGet(out NetworkObject netObj) 
                && netObj.NetworkObjectId == networkObjectId)
            {
                _players.RemoveAt(i);
                break;
            }
        }
    }
    
    
    [Rpc(SendTo.SpecifiedInParams)]
    private void ConfirmQuitRpc(RpcParams rpcParams = default)
    {
        _ = LeaveGame();
    }

    
    private async Task LeaveGame()
    {
        await _connectionManager.Session.LeaveAsync();
        NetworkManager.Singleton.Shutdown();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }
}
