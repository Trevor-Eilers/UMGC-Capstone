// Author: Trevor Eilers

using Simulation;
using UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class Player : NetworkBehaviour, InputSystem_Actions.IKeyboardActions
{
    public PolicyValues CurrentPolicies { get; protected set; } = PolicyValues.Default();
    
    private TopBarViewModel _topBar;
    private PolicySliders _policySliders;
    private UIDocument _doc;
    private VisualElement _root;

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
            return;
        }
    }

    protected virtual void Start()
    {
        if (!IsOwner) return;

        _policySliders = GetComponent<PolicySliders>();
        
        _policySliders.OnPolicyChanged += values => CurrentPolicies = values;
        
        _doc = GetComponent<UIDocument>();
        _root = _doc.rootVisualElement;
        _topBar = ScriptableObject.CreateInstance<TopBarViewModel>();
        _root.dataSource = _topBar;
        _topBar.BindToPanel(_root);
        
        _topBar.OnSpeedChangeRequested += speed => GameManager.Instance.RequestSetSpeedRpc(speed);
        _topBar.OnPauseChangeRequested += paused => GameManager.Instance.RequestSetPauseRpc(paused);
        _topBar.OnQuitRequested += () => GameManager.Instance.RequestQuitRpc(NetworkObjectId);
        
        GameManager.GameState.OnValueChanged += (_, newVal) => _topBar.UpdateFromGameState(newVal);
        
        districtNetRef.OnValueChanged += (_, _) =>
        {
            var district = District;
            if (district == null) return;
            district.state.OnValueChanged -= OnDistrictStateChanged;
            district.state.OnValueChanged += OnDistrictStateChanged;
        };
    }

    private void OnDistrictStateChanged(DistrictState _, DistrictState newState)
        => _topBar.UpdateFromDistrictState(newState);

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (_topBar != null) Destroy(_topBar);
    }
}
