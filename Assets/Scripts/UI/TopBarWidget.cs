using System;
using UI;
using UnityEngine;
using UnityEngine.UIElements;

public class TopBarWidget : MonoBehaviour
{
    private UIDocument _doc;
    private VisualElement _root;
    
    // Top bar
    private Label _cityRepValue;
    private VisualElement _cityRepBar;
    private Label _sharedInfraValue;
    private VisualElement _sharedInfraBar;
    private Label _metroInflowValue;
    private Label _monthLabel;
    private Label _tickLabel;
    private Button _speed1Btn;
    private Button _speed2Btn;
    private Button _speed3Btn;
    private Button _pauseBtn;

    private void Start()
    {
        _doc = GetComponent<UIDocument>();
        _root = _doc.rootVisualElement;
        
        _cityRepValue = _root.Q<Label>("CityRepValue");
        _cityRepBar = _root.Q<VisualElement>("CityRepBar");
        _sharedInfraValue = _root.Q<Label>("SharedInfraValue");
        _sharedInfraBar = _root.Q<VisualElement>("SharedInfraBar");
        _metroInflowValue = _root.Q<Label>("MetroInflowValue");
        _monthLabel = _root.Q<Label>("MonthLabel");
        _tickLabel = _root.Q<Label>("TickLabel");
        _speed1Btn = _root.Q<Button>("Speed1Btn");
        _speed2Btn = _root.Q<Button>("Speed2Btn");
        _speed3Btn = _root.Q<Button>("Speed3Btn");
        _pauseBtn = _root.Q<Button>("PauseBtn");
        
        _speed1Btn.clicked += UpdateSpeedButtons;
        _speed2Btn.clicked += UpdateSpeedButtons;
        _speed3Btn.clicked += UpdateSpeedButtons;
        _pauseBtn.clicked += UpdateSpeedButtons;
    }
    
    private void UpdateSpeedButtons()
    {
        var gameSpeed = GameManager.GameState.Value.gameSpeed;
        _speed1Btn.SetEnabled(Mathf.Approximately(gameSpeed, 1f));
        _speed2Btn.SetEnabled(Mathf.Approximately(gameSpeed, 2f));
        _speed3Btn.SetEnabled(Mathf.Approximately(gameSpeed, 3f));
        _pauseBtn.SetEnabled(GameManager.GameState.Value.isPaused);
    }


}
