// Author: Trevor Eilers

using UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class Player : NetworkBehaviour, InputSystem_Actions.IKeyboardActions
{
    public PolicyValues CurrentPolicies { get; protected set; } = PolicyValues.Default();

    private PolicySliders _policySliders;

    private InputSystem_Actions _actions;
    private InputSystem_Actions.KeyboardActions _keyboardActions;

    public NetworkVariable<NetworkBehaviourReference> districtNetRef = new(writePerm: NetworkVariableWritePermission.Owner);

    public District District
    {
        get
        {
            districtNetRef.Value.TryGet(out District d, NetworkManager.Singleton);
            return d;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            GetComponent<UIDocument>().enabled = false;
            GetComponent<PolicySliders>().enabled = false;
            GetComponent<TopBarViewController>().enabled = false;
            return;
        }
    }

    protected virtual void Start()
    {
        if (!IsOwner) return;

        _policySliders = GetComponent<PolicySliders>();
        _policySliders.OnPolicyChanged += values => CurrentPolicies = values;

        GetComponent<TopBarViewController>().Initialize(this);
    }
}
