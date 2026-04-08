// Author: Trevor Eilers

using UI;
using UnityEngine;

public class Player : MonoBehaviour, InputSystem_Actions.IKeyboardActions
{
    public PolicySliders policySliders;
    private InputSystem_Actions _actions;
    private InputSystem_Actions.KeyboardActions _keyboardActions;
    
    void Start()
    {
        policySliders = GetComponent<PolicySliders>();
        _actions = new InputSystem_Actions();
        _keyboardActions = _actions.Keyboard;
    }
}
