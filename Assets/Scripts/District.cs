using Simulation;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class District : NetworkBehaviour
{
    public NetworkVariable<DistrictState> state = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        state.Value = DistrictState.Default();
    }
}
