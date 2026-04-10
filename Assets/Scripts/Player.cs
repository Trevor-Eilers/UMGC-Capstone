// Author: Trevor Eilers

using UI;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour, InputSystem_Actions.IKeyboardActions
{
    public PolicySliders policySliders;
    public NetworkVariable<NetworkBehaviourReference> district = new();
    private InputSystem_Actions _actions;
    private InputSystem_Actions.KeyboardActions _keyboardActions;

    public District District
    {
        get
        {
            district.Value.TryGet(out District d, NetworkManager.Singleton);
            return d;
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        policySliders = GetComponent<PolicySliders>();
    }
}
