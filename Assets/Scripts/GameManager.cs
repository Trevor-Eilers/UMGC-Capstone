// Authors: Malcolm Bramble, Trevor Eilers

using System;
using System.Collections.Generic;
using System.Linq;
using Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    private GameState _gameState;
    private float _tickTimer;
    private bool _gameOver;

    // TODO: Move this out
    // Metric bar colors
    private static readonly Color ColGdp =     new(0.30f, 0.72f, 0.91f);
    private static readonly Color ColHappy =   new(0.91f, 0.75f, 0.19f);
    private static readonly Color ColPop =     new(0.38f, 0.78f, 0.38f);
    private static readonly Color ColInfra =   new(0.63f, 0.44f, 0.82f);
    private static readonly Color ColSustain = new(0.25f, 0.80f, 0.60f);
    private static readonly Color ColDebt =    new(0.88f, 0.31f, 0.31f);


    private Dictionary<Player, District> _playerDistrictMap;
    private District[] _districtObjects;

    void Start()
    {
        var districts = FindObjectsByType<District>(FindObjectsSortMode.None);
        var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            _playerDistrictMap.Add(players[i], districts[i]);
        }

        _districtObjects = _playerDistrictMap.Values.ToArray();

        // Extract DistrictState from each District's network variable
        var states = new DistrictState[_districtObjects.Length];
        for (int i = 0; i < _districtObjects.Length; i++)
            states[i] = _districtObjects[i].state.Value;

        _gameState = GameState.NewGame(states);
    }

    public void Setup()
    {

    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (_gameState.isPaused || _gameOver) return;

        float tickInterval = 3.125f / _gameState.gameSpeed;
        _tickTimer += Time.deltaTime;

        if (_tickTimer >= tickInterval)
        {
            _tickTimer -= tickInterval;
            ResolveTick();
        }
    }

    private void ResolveTick()
    {
        // Read latest state from network variables (includes policy changes from client RPCs)
        for (int i = 0; i < _districtObjects.Length; i++)
            _gameState.districts[i] = _districtObjects[i].state.Value;

        UpdatePolicies();

        _gameState = TickProcessor.ResolveTick(_gameState);

        // Write updated state back to network variables for replication
        for (int i = 0; i < _districtObjects.Length; i++)
            _districtObjects[i].state.Value = _gameState.districts[i];

        if (_gameState.currentTick >= 576)
        {
            _gameOver = true;
        }
    }

    private void UpdatePolicies()
    {
        int i = 0;
        foreach (var player in _playerDistrictMap.Keys)
        {
            _gameState.districts[i].policyValues = new PolicyValues
            {
                taxRate = player.policySliders.taxRate,
                education = player.policySliders.education,
                infrastructure = player.policySliders.infrastructure,
                housing = player.policySliders.housing,
                environment = player.policySliders.environment,
                cityContribution = player.policySliders.cityContribution
            };
            i++;
        }
    }
}
