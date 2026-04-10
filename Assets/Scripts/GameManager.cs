// Authors: Malcolm Bramble, Trevor Eilers

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Network;
using Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    private NetworkVariable<GameState> _gameState = new();
    private float _tickTimer = 0f;
    private bool _tickReady = false;
    private bool _gameOver =  false;
    private float _tickInterval = 3.125f;

    // TODO: Move this out
    // Metric bar colors
    private static readonly Color ColGdp =     new(0.30f, 0.72f, 0.91f);
    private static readonly Color ColHappy =   new(0.91f, 0.75f, 0.19f);
    private static readonly Color ColPop =     new(0.38f, 0.78f, 0.38f);
    private static readonly Color ColInfra =   new(0.63f, 0.44f, 0.82f);
    private static readonly Color ColSustain = new(0.25f, 0.80f, 0.60f);
    private static readonly Color ColDebt =    new(0.88f, 0.31f, 0.31f);

    private ConnectionManager _connectionManager;
    
    private int _tickReadyCounter = 0;
    
    private int _initializationCounter = 0;
    
    // This is only needed by the host
    private NetworkList<NetworkObjectReference> _players = new();
    
    private Player _localPlayer;

    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            var state = new GameState();
            state.Reset();
            _gameState.Value = state;
        }
        
        Initialize();
    }

    private async void Initialize()
    {
        try
        {
            _connectionManager = FindFirstObjectByType<ConnectionManager>();
            
            // Wait for local player to spawn
            while (_localPlayer == null)
            {
                var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
                foreach (var player in players)
                {
                    if (player.IsOwner) _localPlayer = player;
                }

                if (_localPlayer == null) await Task.Delay(100);
            }

            SignalInitializeRpc();

            if (HasAuthority)
            {
                while (_initializationCounter < _connectionManager.playerCount)
                {
                    Debug.Log("Waiting for clients");
                    await Task.Delay(1500);
                }
                
                var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
                for (int i = 0; i < players.Length; i++)
                {
                    _players.Add(players[i].NetworkObject);
                    
                    var districtObject = Instantiate(Resources.Load<GameObject>("District"));
                    var networkObject = districtObject.GetComponent<NetworkObject>();
                    networkObject.SpawnWithOwnership(NetworkManager.Singleton.ConnectedClientsIds[i], true);
                }

                var value = _gameState.Value;
                value.isPaused = false;
                _gameState.Value = value;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    
    
    void Update()
    {
        if (HasAuthority)
        {
            // TODO: This is will not work with the addition of AI
            if (_tickReadyCounter >= _connectionManager.playerCount)
            {
                ResolveTickRpc();
                return;
            }
        }
        
        if (_tickReady || _gameState.Value.isPaused || _gameOver) return;

        _tickTimer += Time.deltaTime;
        
        if (_tickTimer >= _tickInterval)
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

        UpdatePolicies();

        if (!HasAuthority) return;

        // Gather all district states
        var districtStates = new DistrictState[_players.Count];
        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].TryGet(out NetworkObject networkObject))
            {
                var player = networkObject.GetComponent<Player>();
                districtStates[i] = player.District.state.Value;
            }
        }

        // Host resolves city-wide metrics
        var gameState = _gameState.Value;
        gameState.cityMetrics = TickProcessor.ResolveCityMetrics(districtStates, gameState.cityMetrics);
        gameState.currentTick++;
        gameState.currentMonth = gameState.currentTick / SimulationConstants.TICKS_PER_MONTH;
        _gameState.Value = gameState;

        if (gameState.currentTick >= SimulationConstants.TOTAL_TICKS)
        {
            _gameOver = true;
        }
        
        ResolveDistrictTickRpc(districtStates, gameState.cityMetrics);
    }

    [Rpc(SendTo.Everyone)]
    private void ResolveDistrictTickRpc(DistrictState[] districtStates, CityMetrics cityMetrics)
    {
        int localIndex = -1;
        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].TryGet(out NetworkObject networkObject) &&
                networkObject.GetComponent<Player>() == _localPlayer)
            {
                localIndex = i;
                break;
            }
        }

        if (localIndex >= 0)
        {
            var result = TickProcessor.ResolveDistrictTick(
                localIndex, districtStates, cityMetrics);
            _localPlayer.District.state.Value = result;
        }
        
        _localPlayer.UpdateUI();
    }

    private void UpdatePolicies()
    {
        var newValues = new PolicyValues()
        {
            taxRate = _localPlayer.policySliders.taxRate,
            education = _localPlayer.policySliders.education,
            infrastructure = _localPlayer.policySliders.infrastructure,
            housing = _localPlayer.policySliders.housing,
            environment = _localPlayer.policySliders.environment,
            cityContribution = _localPlayer.policySliders.cityContribution
        };
        
        var districtState = _localPlayer.District.state.Value;
        districtState.policyValues = newValues;
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
}
