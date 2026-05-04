using Building;
using Simulation;
using Unity.Netcode;
using UnityEngine;

namespace Core
{
    public class District : NetworkBehaviour
    {
        public NetworkVariable<DistrictState> state = new();

        public NetworkList<BuildingPlacement> placements;

        public BuildingSystem BuildingSystem { get; set; }
        public DistrictCivilianSpawner CivilianSpawner { get; set; }

        private void Awake()
        {
            placements = new NetworkList<BuildingPlacement>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;

            state.Value = DistrictState.Default();
        
            foreach (var player in FindObjectsByType<Player>(FindObjectsSortMode.None))
            {
                if (player.OwnerClientId != OwnerClientId) continue;
                if (player.districtNetRef.Value.TryGet(out District _, NetworkManager.Singleton)) continue;
                player.districtNetRef.Value = new NetworkBehaviourReference(this);
                break;
            }
        }
    }
}
