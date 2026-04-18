// Authors: Malcolm Bramble, Trevor Eilers

using System;
using System.Collections;
using System.Threading.Tasks;
using Network;
using Simulation;
using UnityEngine;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    public static NetworkVariable<GameState> GameState { get; private set; } = new();
    private float _tickTimer = 0f;
    private bool _tickReady = false;
    private bool _resolvingTick = false;
    private bool _gameOver =  false;
    private readonly float _tickInterval = 2f;

    [SerializeField] private bool _soloDebug = false;
    private int ExpectedPlayers => _soloDebug
        ? 1
        : (_connectionManager != null ? _connectionManager.PlayerCount : 1);

    private ConnectionManager _connectionManager;

    private int _tickReadyCounter = 0;

    private int _initializationCounter = 0;

    // This is only needed by the host
    private readonly NetworkList<NetworkObjectReference> _players = new();

    private Player _localPlayer;

    private BuildingGenerator _buildingGen;

    private DistrictState[] _lastDistrictStates;

    public static GameManager Instance { get; private set; }

    public int PlayerCount => _players.Count;

    public NetworkObjectReference GetPlayer(int index) => _players[index];

    public DistrictState[] LastDistrictStates => _lastDistrictStates;

    public int NumActivePlayers => _connectionManager != null ? _connectionManager.PlayerCount : _players.Count;

    public event Action<DistrictState[], CityMetrics> OnDistrictStatesUpdated;

    
    public override void OnNetworkSpawn()
    {
        Instance = this;

        if (HasAuthority)
        {
            var state = new GameState();
            state.Default();
            GameState.Value = state;
        }

        StartCoroutine(Initialize());
    }
    
    
    private IEnumerator Initialize()
    {
        _connectionManager = FindFirstObjectByType<ConnectionManager>();
        _buildingGen = FindFirstObjectByType<BuildingGenerator>();
        if (_buildingGen == null)
            Debug.LogWarning("GameManager: no BuildingGenerator found in scene — city visuals will not update.");

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
            while (_initializationCounter < ExpectedPlayers)
            {
                Debug.Log($"Waiting for clients ({_initializationCounter}/{ExpectedPlayers})");
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
        // Skip while netcode is tearing down — Update() can fire for another frame
        // after OnNetworkDespawn, and RPC invocations there throw
        // "Rpc methods can only be invoked after starting the NetworkManager!".
        if (!IsSpawned) return;

        if (HasAuthority)
        {
            if (_tickReadyCounter >= ExpectedPlayers)
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
            districtStates[localIndex].policyValues = _localPlayer.CurrentPolicies;

            var result = TickProcessor.ResolveDistrictTick(
                localIndex, districtStates, cityMetrics);
            _localPlayer.District.state.Value = result;
        }

        _lastDistrictStates = districtStates;

        if (_buildingGen != null)
        {
            for (int i = 0; i < districtStates.Length; i++)
                _buildingGen.UpdateDistrict(i, districtStates[i]);
        }

        OnDistrictStatesUpdated?.Invoke(districtStates, cityMetrics);

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
            StartCoroutine(nameof(RemovePlayer), networkObjectId);
            ConfirmQuitRpc(RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    
    private IEnumerable RemovePlayer(ulong networkObjectId)
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
        // Session.LeaveAsync() tears down the underlying NetworkManager for us —
        // calling Shutdown() afterward produces a "NetworkManager has been shutdown
        // outside of a session" warning and can leave dangling callbacks.
        await _connectionManager.Session.LeaveAsync();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }
}
