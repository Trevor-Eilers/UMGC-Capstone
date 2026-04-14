// View-model for city-wide and game-state data, designed for UI Toolkit data binding.
// Set as dataSource on the TopBar VisualElement subtree.
//
// Binding paths (for UIBuilder):
//   City metrics:  CityReputation, SharedInfraQuality, MetroPopulationPool
//   Bar widths:    CityReputation, SharedInfraQuality, MetroInflowPercent
//                  (use with "PercentBar" converter group on style.width)
//   Game timing:   CurrentMonth, CurrentTick, MonthDisplay, TickDisplay
//   Speed state:   GameSpeed, IsPaused
//
// Commands (wired imperatively via BindToPanel):
//   OnSpeedChangeRequested, OnPauseChangeRequested

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Simulation;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "TopBarViewModel", menuName = "TopBar View Model")]
public class TopBarViewModel : ScriptableObject, INotifyBindablePropertyChanged
{
    public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

    // Outbound commands
    public event Action<int> OnSpeedChangeRequested;
    public event Action<bool> OnPauseChangeRequested;

    // City metrics
    [SerializeField] private int _cityReputation;
    [SerializeField] private int _sharedInfraQuality;
    [SerializeField] private int _metroPopulationPool;

    [CreateProperty]
    public int CityReputation
    {
        get => _cityReputation;
        set => SetProperty(ref _cityReputation, value);
    }

    [CreateProperty]
    public int SharedInfraQuality
    {
        get => _sharedInfraQuality;
        set => SetProperty(ref _sharedInfraQuality, value);
    }

    [CreateProperty]
    public int MetroPopulationPool
    {
        get => _metroPopulationPool;
        set => SetProperty(ref _metroPopulationPool, value);
    }
    
    private const float METRO_FLOW_SCALE = 10f;

    [CreateProperty]
    public int MetroInflowPercent
    {
        get
        {
            float normalized = (_metroPopulationPool / METRO_FLOW_SCALE) * 50f + 50f;
            return (int)Mathf.Clamp(normalized, 0f, 100f);
        }
    }

    [SerializeField] private int _currentMonth;
    [SerializeField] private int _currentTick;

    [CreateProperty]
    public int CurrentMonth
    {
        get => _currentMonth;
        set => SetProperty(ref _currentMonth, value);
    }

    [CreateProperty]
    public int CurrentTick
    {
        get => _currentTick;
        set => SetProperty(ref _currentTick, value);
    }
    
    [CreateProperty]
    public string MonthDisplay => $"Month {_currentMonth} / {SimulationConstants.TOTAL_MONTHS}";
    
    [CreateProperty]
    public string TickDisplay => $"Tick {_currentTick} / {SimulationConstants.TOTAL_TICKS}";
    

    [SerializeField] private int _gameSpeed;
    [SerializeField] private bool _isPaused;

    [CreateProperty]
    public int GameSpeed
    {
        get => _gameSpeed;
        set => SetProperty(ref _gameSpeed, value);
    }

    [CreateProperty]
    public bool IsPaused
    {
        get => _isPaused;
        set => SetProperty(ref _isPaused, value);
    }
    

    public void UpdateFromState(GameState state)
    {
        CityReputation = (int) state.cityMetrics.cityReputation;
        SharedInfraQuality = (int) state.cityMetrics.sharedInfraQuality;
        MetroPopulationPool = (int) state.cityMetrics.metroPopulationPool;

        CurrentMonth = state.currentMonth;
        CurrentTick = state.currentTick;

        GameSpeed = state.gameSpeed;
        IsPaused = state.isPaused;

        // Derived properties depend on the fields above; notify explicitly.
        Notify(nameof(MetroInflowPercent));
        Notify(nameof(MonthDisplay));
        Notify(nameof(TickDisplay));
    }


    // Only speed/pause buttons need imperative wiring; all data labels use binding.
    public void BindToPanel(VisualElement root)
    {
        var speed1 = root.Q<Button>("Speed1Btn");
        var speed2 = root.Q<Button>("Speed2Btn");
        var speed3 = root.Q<Button>("Speed3Btn");
        var pause  = root.Q<Button>("PauseBtn");

        if (speed1 != null) speed1.clicked += () => OnSpeedChangeRequested?.Invoke(1);
        if (speed2 != null) speed2.clicked += () => OnSpeedChangeRequested?.Invoke(2);
        if (speed3 != null) speed3.clicked += () => OnSpeedChangeRequested?.Invoke(3);
        if (pause  != null) pause.clicked  += () => OnPauseChangeRequested?.Invoke(!_isPaused);
    }
    

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string name = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Notify(name);
        return true;
    }

    private void Notify([CallerMemberName] string property = "")
    {
        propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
    }
}
