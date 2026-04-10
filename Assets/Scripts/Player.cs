// Author: Trevor Eilers

using System;
using UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class Player : NetworkBehaviour, InputSystem_Actions.IKeyboardActions
{
    public NetworkVariable<NetworkBehaviourReference> districtNetRef = new(writePerm: NetworkVariableWritePermission.Owner);

    public PolicySliders policySliders;
    private UIDocument _doc;
    private VisualElement _root;
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

        policySliders = GetComponent<PolicySliders>();
        _doc = GetComponent<UIDocument>();
        _root = _doc.rootVisualElement;
        _gdpIndicator = (IndicatorWidget)_root.Q("gdp-indicator");
        _surpIndicator = (IndicatorWidget)_root.Q("surp-indicator");
        _revIndicator = (IndicatorWidget)_root.Q("rev-indicator");
        _popIndicator = (IndicatorWidget)_root.Q("pop-indicator");
        _happIndicator = (IndicatorWidget)_root.Q("happ-indicator");
        _infraIndicator = (IndicatorWidget)_root.Q("infra-indicator");
        _sustIndicator = (IndicatorWidget)_root.Q("sust-indicator");
        _pollIndicator = (IndicatorWidget)_root.Q("poll-indicator");
    }

    public void UpdateUI()
    {
        if (!IsOwner) return;

        Debug.Log($"GDP: {District.state.Value.gdp} " +
                  $"Surp: {District.state.Value.reserve} " +
                  $"Rev: {District.state.Value.revenue}" +
                  $"Pop: {District.state.Value.population} " +
                  $"Happ: {District.state.Value.happiness}");
        
        _gdpIndicator.Value = District.state.Value.gdp.ToString();
        _surpIndicator.Value = District.state.Value.reserve.ToString();
        _revIndicator.Value = District.state.Value.revenue.ToString();
        _popIndicator.Value = District.state.Value.population.ToString();
        _happIndicator.Value = District.state.Value.happiness.ToString();
        _infraIndicator.Value = District.state.Value.infrastructure.ToString();
        _sustIndicator.Value = District.state.Value.sustainability.ToString();
        _pollIndicator.Value = "0";
    }
    
    public District District
    {
        get
        {
            districtNetRef.Value.TryGet(out District d, NetworkManager.Singleton);
            return d;
        }
    }
}
