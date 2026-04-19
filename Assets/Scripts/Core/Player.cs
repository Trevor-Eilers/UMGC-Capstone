// Author: Trevor Eilers

using Core;
using Network;
using UI;
using UI.TopBar;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine.UIElements;

public class Player : NetworkBehaviour, InputSystem_Actions.IKeyboardActions
{
    public PolicyValues CurrentPolicies { get; private set; } = PolicyValues.Default();

    private PolicySliders _policySliders;
    private PlayerLabelController _playerLabelController;

    private InputSystem_Actions _actions;
    private InputSystem_Actions.KeyboardActions _keyboardActions;


    public NetworkVariable<NetworkBehaviourReference> districtNetRef = new(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString64Bytes> playerName = new(writePerm: NetworkVariableWritePermission.Owner);

    public District District
    {
        get
        {
            districtNetRef.Value.TryGet(out District d, NetworkManager.Singleton);
            return d;
        }
    }

    public void OnPlayerListChanged()
    {
        _playerLabelController.viewModel.Update();
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

        playerName.Value = new FixedString64Bytes(ConnectionManager.Instance.ProfileName);
    }

    protected void Start()
    {
        if (!IsOwner) return;

        _playerLabelController = GetComponent<PlayerLabelController>();

        var ai = GetComponent<AIController>();
        if (ai != null)
        {
            if (TryGetComponent<PolicySliders>(out var ps)) ps.enabled = false;
            if (TryGetComponent<UIDocument>(out var ui)) ui.enabled = false;
            if (TryGetComponent<TopBarViewController>(out var tb)) tb.enabled = false;
            CurrentPolicies = ai.CurrentPolicies;
            return;
        }

        _policySliders = GetComponent<PolicySliders>();
        _policySliders.OnPolicyChanged += values => CurrentPolicies = values;

        GetComponent<TopBarViewController>().Initialize(this);
    }

    public void SetPoliciesFromAI(PolicyValues values)
    {
        CurrentPolicies = values;
    }
}
