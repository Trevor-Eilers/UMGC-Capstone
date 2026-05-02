// Authors: Malcolm Bramble, Trevor Eilers

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Building;
using Network;
using Simulation;
using UI;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Core
{
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
    
        public readonly NetworkList<NetworkObjectReference> players = new();

        public readonly NetworkList<BuildingPlacement> civicPlacements = new();

        public Player localPlayer { get; private set; }

        public static GameManager Instance { get; private set; }

        [SerializeField] private BuildingSystem[] quadrantBuildingSystems = new BuildingSystem[4];

        [SerializeField] private CivicBuildingSystem centerBuildingSystem;

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

            for (int i = players.Count - 1; i >= 0; i--)
            {
                bool resolved = players[i].TryGet(out NetworkObject netObj);
                if (!resolved || netObj == null || netObj.OwnerClientId == clientId)
                {
                    players.RemoveAt(i);
                }
            }
        }


        private List<Player> GetPlayerList()
        {
            var active = new List<Player>(players.Count);
            for (int i = 0; i < players.Count; i++)
            {
                if (!players[i].TryGet(out NetworkObject networkObject)) continue;
                if (networkObject == null) continue;
                var player = networkObject.GetComponent<Player>();
                if (player == null || player.District == null) continue;
                active.Add(player);
            }
            return active;
        }


        public DistrictState[] GetDistrictStates(List<Player> activePlayers)
        {
            var districtStates = new DistrictState[activePlayers.Count];
            for (int i = 0; i < activePlayers.Count; i++)
            {
                districtStates[i] = activePlayers[i].District.state.Value;
            }
            return districtStates;
        }


        private IEnumerator Initialize()
        {
            _connectionManager = FindFirstObjectByType<ConnectionManager>();

            while (localPlayer == null)
            {
                var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
                foreach (var player in players)
                {
                    if (player.IsOwner) localPlayer = player;
                }

                if (localPlayer == null) yield return new WaitForSeconds(0.1f);
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
                    this.players.Add(players[i].NetworkObject);

                    var districtObject = Instantiate(Resources.Load<GameObject>("District"));
                    var networkObject = districtObject.GetComponent<NetworkObject>();
                    networkObject.SpawnWithOwnership(players[i].OwnerClientId, true);
                }

                ulong hostId = NetworkManager.Singleton.LocalClientId;
                for (int i = players.Length; i < 4; i++)
                {
                    var aiPlayerObject = Instantiate(Resources.Load<GameObject>("Player"));
                    aiPlayerObject.AddComponent<AIController>();
                    var aiPlayerNetObj = aiPlayerObject.GetComponent<NetworkObject>();
                    aiPlayerNetObj.SpawnWithOwnership(hostId, true);
                    aiPlayerObject.GetComponent<Player>().playerName.Value =
                        new FixedString64Bytes($"AI {i - players.Length + 1}");
                    this.players.Add(aiPlayerNetObj);
                
                    var districtObject = Instantiate(Resources.Load<GameObject>("District"));
                    var districtNetObj = districtObject.GetComponent<NetworkObject>();
                    districtNetObj.SpawnWithOwnership(hostId, true);
                }

                var gameState = GameState.Value;
                gameState.isPaused = false;
                GameState.Value = gameState;

                Debug.Log("Initialization complete.");
            }

            // All clients wait until all 4 players are registered with non-empty names
            // before initializing labels, ensuring districts and names are fully in place.
            while (players.Count < 4)
                yield return null;

            yield return new WaitUntil(() =>
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (!players[i].TryGet(out NetworkObject netObj) || netObj == null) return false;
                    var p = netObj.GetComponent<Player>();
                    if (p == null || string.IsNullOrEmpty(p.playerName.Value.ToString())) return false;
                }
                return true;
            });

            localPlayer.InitializePlayerLabels();
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

            var activePlayers = GetPlayerList();
            var districtStates = GetDistrictStates(activePlayers);

            for (int i = 0; i < activePlayers.Count; i++)
            {
                var ai = activePlayers[i].GetComponent<AIController>();
                if (ai == null) continue;
                ai.OnTick(districtStates[i]);
                activePlayers[i].SetPoliciesFromAI(ai.CurrentPolicies);
                districtStates[i].policyValues = ai.CurrentPolicies;
                Debug.Log($"AI Policies: (TaxRate: {ai.CurrentPolicies.taxRate}) " +
                          $"(Education: {ai.CurrentPolicies.education}) " +
                          $"(Infrastructure: {ai.CurrentPolicies.infrastructure}) " +
                          $"(Environment: {ai.CurrentPolicies.environment}) " +
                          $"(Housing: {ai.CurrentPolicies.housing}) " +
                          $"(City Contribution: {ai.CurrentPolicies.cityContribution})");
                var aiPlayer = ai.GetComponent<Player>();
                Debug.Log($"AI Metrics: (GDP: {aiPlayer.District.state.Value.gdp:F1}) " +
                          $"(Happiness: {aiPlayer.District.state.Value.happiness:F1}) " +
                          $"(Population: {aiPlayer.District.state.Value.population:F1}) " +
                          $"(Infrastructure: {aiPlayer.District.state.Value.infrastructure:F1}) " +
                          $"(Sustainability: {aiPlayer.District.state.Value.sustainability:F1}) " +
                          $"(Debt: {aiPlayer.District.state.Value.debt:F1}) " +
                          $"(Reserve: {aiPlayer.District.state.Value.reserve:F1}) " +
                          $"(Revenue: {aiPlayer.District.state.Value.revenue:F1}) " +
                          $"(Total Spending: {aiPlayer.District.state.Value.totalSpending:F1})");
            }

            // Host resolves city-wide metrics
            var gameState = GameState.Value;
            gameState.cityMetrics = TickProcessor.ResolveCityMetrics(districtStates, gameState.cityMetrics);
            gameState.currentTick++;
            gameState.currentMonth = gameState.currentTick / SimulationConstants.TICKS_PER_MONTH;
            GameState.Value = gameState;
        
            ResolveDistrictTickRpc(districtStates, gameState.cityMetrics);
        }

    
        [Rpc(SendTo.Everyone)]
        private void ResolveDistrictTickRpc(DistrictState[] districtStates, CityMetrics cityMetrics)
        {
            var activePlayers = GetPlayerList();
            int localIndex = activePlayers.IndexOf(localPlayer);
            
            if (localIndex >= 0 && localIndex < districtStates.Length)
            {
                districtStates[localIndex].policyValues = localPlayer.CurrentPolicies;

                var result = TickProcessor.ResolveDistrictTick(
                    localIndex, districtStates, cityMetrics);
                localPlayer.District.state.Value = result;
            }

            if (HasAuthority)
            {
                for (int i = 0; i < activePlayers.Count; i++)
                {
                    var ai = activePlayers[i].GetComponent<AIController>();
                    if (ai == null) continue;
                    districtStates[i].policyValues = ai.CurrentPolicies;
                    var aiResult = TickProcessor.ResolveDistrictTick(i, districtStates, cityMetrics);
                    activePlayers[i].District.state.Value = aiResult;
                }
            }
            
            for (int i = 0; i < activePlayers.Count && i < quadrantBuildingSystems.Length; i++)
            {
                var d = activePlayers[i].District;
                if (d != null && d.BuildingSystem == null)
                {
                    d.BuildingSystem = quadrantBuildingSystems[i];
                    d.BuildingSystem.District = d;
                }
            }

            if (centerBuildingSystem != null && centerBuildingSystem.Placements == null)
                centerBuildingSystem.Placements = civicPlacements;

            if (localIndex >= 0 && localIndex < districtStates.Length)
                localPlayer.District?.BuildingSystem?.UpdateDistrict(districtStates[localIndex]);

            if (HasAuthority)
            {
                for (int i = 0; i < activePlayers.Count; i++)
                {
                    var ai = activePlayers[i].GetComponent<AIController>();
                    if (ai == null) continue;
                    activePlayers[i].District?.BuildingSystem?.UpdateDistrict(districtStates[i]);
                }
            }

            if (HasAuthority && centerBuildingSystem != null)
                centerBuildingSystem.UpdateCenter(districtStates);

            if (GameState.Value.currentTick >= SimulationConstants.TOTAL_TICKS)
            {
                _gameOver = true;
                if (HasAuthority) EndGameRpc();
            }
            
            _resolvingTick = false;
        }

    
        private void UpdatePolicies()
        {
            var districtState = localPlayer.District.state.Value;
            districtState.policyValues = localPlayer.CurrentPolicies;
            localPlayer.District.state.Value = districtState;
        }

    
        [Rpc(SendTo.Everyone)]
        private void EndGameRpc()
        {
            if (localPlayer.GetComponent<AIController>() != null) return;
            
            var controller = localPlayer.GetComponent<EndCardController>();
            var district = localPlayer.District;
            var districtState = district.state.Value;
            var cityMetrics = GameState.Value.cityMetrics;
            FinalScore score = ScoringSystem.ComputeFinalScore(
                districtState,
                cityMetrics,
                GetDistrictStates(GetPlayerList()),
                4);
            controller.Show(score);
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
        
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].TryGet(out NetworkObject netObj)
                    && netObj.NetworkObjectId == networkObjectId)
                {
                    if (netObj.OwnerClientId != NetworkManager.Singleton.LocalClientId) 
                        netObj.ChangeOwnership(NetworkManager.Singleton.LocalClientId);
                    netObj.gameObject.AddComponent<AIController>();
                    var player = netObj.gameObject.GetComponent<Player>();
                    player.District.GetComponent<NetworkObject>().ChangeOwnership(NetworkManager.Singleton.LocalClientId);
                    if (player != null) player.playerName.Value = $"AI {NextAiNumber()}";
                    break;
                }
            }
            
            players[0] = players[0];
        }


        private int NextAiNumber()
        {
            int aiNum = 0;
            for (int i = 0; i < players.Count; i++)
            {
                if (!players[i].TryGet(out NetworkObject netObj) || netObj == null) continue;
                if (netObj.GetComponent<AIController>() == null) continue;
                var name = netObj.GetComponent<Player>()?.playerName.Value.ToString();
                if (string.IsNullOrEmpty(name) || !name.StartsWith("AI ")) continue;
                if (int.TryParse(name[3..], out int n) && n > aiNum) aiNum = n;
            }
            return aiNum + 1;
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
}
