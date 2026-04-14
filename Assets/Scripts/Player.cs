// Author: Trevor Eilers

using System;
using Simulation;
using UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class Player : NetworkBehaviour, InputSystem_Actions.IKeyboardActions
{
    public NetworkVariable<NetworkBehaviourReference> districtNetRef = new(writePerm: NetworkVariableWritePermission.Owner);

    public PolicyValues CurrentPolicies { get; private set; } = PolicyValues.Default();

    // Data sources for UI Toolkit binding
    private DistrictViewModel _districtVM;
    private TopBarViewModel _topBar;

    private PolicySliders _policySliders;
    private UIDocument _doc;
    private VisualElement _root;

    // Indicator references kept as manual-update fallback until
    // binding paths are configured in UIBuilder.
    private IndicatorWidget _gdpIndicator;
    private IndicatorWidget _surpIndicator;
    private IndicatorWidget _revIndicator;
    private IndicatorWidget _popIndicator;
    private IndicatorWidget _happIndicator;
    private IndicatorWidget _infraIndicator;
    private IndicatorWidget _sustIndicator;
    private IndicatorWidget _pollIndicator;

    private InputSystem_Actions _actions;
    private InputSystem_Actions.KeyboardActions _keyboardActions;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            GetComponent<UIDocument>().enabled = false;
            GetComponent<PolicySliders>().enabled = false;
            return;
        }
    }

    private void Start()
    {
        if (!IsOwner) return;

        // ── Resolve components ──
        _policySliders = GetComponent<PolicySliders>();
        _doc = GetComponent<UIDocument>();
        _root = _doc.rootVisualElement;
        
        _districtVM = ScriptableObject.CreateInstance<DistrictViewModel>();
        _topBar = ScriptableObject.CreateInstance<TopBarViewModel>();
        
        _root.dataSource = _districtVM;
        _topBar.BindToPanel(_root);
        
        _policySliders.OnPolicyChanged += values => CurrentPolicies = values;
        
        _topBar.OnSpeedChangeRequested += speed => GameManager.Instance.RequestSetSpeedRpc(speed);
        _topBar.OnPauseChangeRequested += paused => GameManager.Instance.RequestSetPauseRpc(paused);
        
        GameManager.GameState.OnValueChanged += (oldVal, newVal) =>
        {
            _topBar.UpdateFromState(newVal);
        };
    }

    public District District
    {
        get
        {
            districtNetRef.Value.TryGet(out District d, NetworkManager.Singleton);
            return d;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (_districtVM != null) Destroy(_districtVM);
        if (_topBar != null) Destroy(_topBar);
    }
}
