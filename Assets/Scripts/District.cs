using Simulation;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class District : NetworkBehaviour
{
    public NetworkVariable<DistrictState> state = new();

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        state.Value = DistrictState.Default();

        // Find our owning Player and register this district
        foreach (var player in FindObjectsByType<Player>(FindObjectsSortMode.None))
        {
            if (player.OwnerClientId == OwnerClientId)
            {
                player.district.Value = new NetworkBehaviourReference(this);
                break;
            }
        }
    }
}
