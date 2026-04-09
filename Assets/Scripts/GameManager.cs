// Authors: Malcolm Bramble, Trevor Eilers

using System.Collections.Generic;
using System.Linq;
using Network;
using Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    private GameState _gameState;
    private float _tickTimer;
    private bool _tickReady = false;
    private bool _gameOver;

    // TODO: Move this out
    // Metric bar colors
    private static readonly Color ColGdp =     new(0.30f, 0.72f, 0.91f);
    private static readonly Color ColHappy =   new(0.91f, 0.75f, 0.19f);
    private static readonly Color ColPop =     new(0.38f, 0.78f, 0.38f);
    private static readonly Color ColInfra =   new(0.63f, 0.44f, 0.82f);
    private static readonly Color ColSustain = new(0.25f, 0.80f, 0.60f);
    private static readonly Color ColDebt =    new(0.88f, 0.31f, 0.31f);

    private ConnectionManager _connectionManager;
    private int _tickReadyCounter;
    private Dictionary<Player, District> _playerDistrictMap = new();
    private int _myDistrictIndex = -1;

    void Start()
    {
        _connectionManager = FindFirstObjectByType<ConnectionManager>();
        
        if (HasAuthority)
        {
            for (int i = 0; i < _connectionManager.playerCount; i++)
            {
                var playerObject = Instantiate(Resources.Load<GameObject>("Player"));
                var player = playerObject.GetComponent<Player>();
            
                var districtObject = Instantiate(Resources.Load<GameObject>("District"));
                var district = districtObject.GetComponent<District>();
            
                _playerDistrictMap.Add(player, district);
            } 
            
            _gameState.districts = _playerDistrictMap.Values.ToArray();
            _gameState.numActivePlayers = _gameState.districts.Length;
            _gameState.cityMetrics = CityMetrics.Default();
            _gameState.currentTick = 0;
            _gameState.currentMonth = 0;
            _gameState.gameSpeed = 1f;
            _gameState.isPaused = false;
        }
        
        // Determine which district this client owns
        for (int i = 0; i < _gameState.districts.Length; i++)
        {
            if (_gameState.districts[i].IsOwner)
            {
                _myDistrictIndex = i;
                Debug.Log($"Client found district with index {_myDistrictIndex}");
                break;
            }

            Debug.LogError($"Client could not find owned district");
            _gameOver =  true;
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
            }
        }
        
        if (_tickReady || _gameState.isPaused || _gameOver) return;

        float tickInterval = 3.125f / _gameState.gameSpeed;
        _tickTimer += Time.deltaTime;

        if (_tickTimer >= tickInterval)
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
        int n = _gameState.numActivePlayers;

        // Take snapshot from all district network variables (replicated state)
        var snapshot = new DistrictState[n];
        for (int i = 0; i < n; i++)
            snapshot[i] = _gameState.districts[i].state.Value;

        // Apply local policy slider values to our district in the snapshot
        UpdatePolicies(snapshot);

        // Compute city-wide metrics (deterministic — all clients get same result)
        _gameState.cityMetrics = TickProcessor.ResolveCityMetrics(
            snapshot, _gameState.cityMetrics, n);

        // Resolve our own district only
        if (_myDistrictIndex >= 0)
        {
            DistrictState result = TickProcessor.ResolveDistrictTick(
                _myDistrictIndex, snapshot, _gameState.cityMetrics, n);

            // Write back to our district's network variable for replication
            _gameState.districts[_myDistrictIndex].state.Value = result;
        }

        _gameState.currentTick++;
        _gameState.currentMonth = _gameState.currentTick / SimulationConstants.TICKS_PER_MONTH;

        if (_gameState.currentTick >= SimulationConstants.TOTAL_TICKS)
        {
            _gameOver = true;
        }
    }

    private void UpdatePolicies(DistrictState[] snapshot)
    {
        // Each client updates their own district's policy values in the snapshot
        // before the tick processes. The updated snapshot is used for simulation.
        foreach (var kvp in _playerDistrictMap)
        {
            Player player = kvp.Key;
            District district = kvp.Value;

            if (!district.IsOwner)
                continue;

            for (int i = 0; i < _gameState.districts.Length; i++)
            {
                if (_gameState.districts[i] == district)
                {
                    snapshot[i].policyValues = new PolicyValues
                    {
                        taxRate = player.policySliders.taxRate,
                        education = player.policySliders.education,
                        infrastructure = player.policySliders.infrastructure,
                        housing = player.policySliders.housing,
                        environment = player.policySliders.environment,
                        cityContribution = player.policySliders.cityContribution
                    };
                    break;
                }
            }
        }
    }

    [Rpc(SendTo.Authority)]
    private void SignalTickReadyRpc()
    {
        _tickReadyCounter++;
        Debug.Log($"Ready signals received: {_tickReadyCounter}");
    }
}
