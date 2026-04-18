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
    public event Action<int> OnSpeedChangeRequested;
    public event Action<bool> OnPauseChangeRequested;
    public event Action OnQuitRequested;
    
    // TODO: Move to view
    private static readonly Color ColGdp =     new(0.30f, 0.72f, 0.91f);
    private static readonly Color ColHappy =   new(0.91f, 0.75f, 0.19f);
    private static readonly Color ColPop =     new(0.38f, 0.78f, 0.38f);
    private static readonly Color ColInfra =   new(0.63f, 0.44f, 0.82f);
    private static readonly Color ColSustain = new(0.25f, 0.80f, 0.60f);
    private static readonly Color ColDebt =    new(0.88f, 0.31f, 0.31f);
    
    // City metrics
    [SerializeField] private int _cityReputation;
    [SerializeField] private int _sharedInfraQuality;
    [SerializeField] private int _metroPopulationPool;

    [CreateProperty] public int CityReputation
    {
        get => _cityReputation;
        set => SetProperty(ref _cityReputation, value);
    }

    [CreateProperty] public int SharedInfraQuality
    {
        get => _sharedInfraQuality;
        set => SetProperty(ref _sharedInfraQuality, value);
    }

    [CreateProperty] public int MetroPopulationPool
    {
        get => _metroPopulationPool;
        set => SetProperty(ref _metroPopulationPool, value);
    }

    private const float METRO_FLOW_SCALE = 10f;

    [CreateProperty] public int MetroInflowPercent
    {
        get
        {
            float normalized = (_metroPopulationPool / METRO_FLOW_SCALE) * 50f + 50f;
            return (int)Mathf.Clamp(normalized, 0f, 100f);
        }
    }


    //  Game timing
    [SerializeField] private int _currentMonth;
    [SerializeField] private int _currentTick;

    [CreateProperty] public int CurrentMonth
    {
        get => _currentMonth;
        set => SetProperty(ref _currentMonth, value);
    }

    [CreateProperty] public int CurrentTick
    {
        get => _currentTick;
        set => SetProperty(ref _currentTick, value);
    }

    [CreateProperty] public string MonthDisplay =>
        $"Month {_currentMonth} / {SimulationConstants.TOTAL_MONTHS}";

    [CreateProperty] public string TickDisplay =>
        $"Tick {_currentTick} / {SimulationConstants.TOTAL_TICKS}";


    // Game speed
    [SerializeField] private int _gameSpeed;
    [SerializeField] private bool _isPaused;

    [CreateProperty] public int GameSpeed
    {
        get => _gameSpeed;
        set => SetProperty(ref _gameSpeed, value);
    }

    [CreateProperty] public bool IsPaused
    {
        get => _isPaused;
        set => SetProperty(ref _isPaused, value);
    }


    // District metrics
    [SerializeField] private float _gdp;
    [SerializeField] private float _happiness;
    [SerializeField] private float _population;
    [SerializeField] private float _infrastructure;
    [SerializeField] private float _sustainability;
    [SerializeField] private float _debt;
    [SerializeField] private float _reserve;
    [SerializeField] private float _revenue;
    [SerializeField] private float _totalSpending;
    [SerializeField] private float _scaleFactor;

    [CreateProperty] public float Gdp
    {
        get => _gdp;
        set => SetProperty(ref _gdp, value);
    }

    [CreateProperty] public float Happiness
    {
        get => _happiness;
        set => SetProperty(ref _happiness, value);
    }

    [CreateProperty] public float Population
    {
        get => _population;
        set => SetProperty(ref _population, value);
    }

    [CreateProperty] public float Infrastructure
    {
        get => _infrastructure;
        set => SetProperty(ref _infrastructure, value);
    }

    [CreateProperty] public float Sustainability
    {
        get => _sustainability;
        set => SetProperty(ref _sustainability, value);
    }

    [CreateProperty] public float Debt
    {
        get => _debt;
        set => SetProperty(ref _debt, value);
    }

    [CreateProperty] public float Reserve
    {
        get => _reserve;
        set => SetProperty(ref _reserve, value);
    }

    [CreateProperty] public float Revenue
    {
        get => _revenue;
        set => SetProperty(ref _revenue, value);
    }

    [CreateProperty] public float TotalSpending
    {
        get => _totalSpending;
        set => SetProperty(ref _totalSpending, value);
    }

    [CreateProperty] public float ScaleFactor
    {
        get => _scaleFactor;
        set => SetProperty(ref _scaleFactor, value);
    }

    [CreateProperty] public float BudgetSurplus => _revenue - _totalSpending;

    [CreateProperty] public float Efficiency => _scaleFactor * 100f;


    // Grants
    [SerializeField] private int _greenGrantStreak;
    [SerializeField] private int _transitGrantStreak;
    [SerializeField] private int _lifeGrantStreak;
    [SerializeField] private int _devGrantStreak;
    [SerializeField] private bool _grantsEligible;

    [CreateProperty] public int GreenGrantStreak
    {
        get => _greenGrantStreak;
        set => SetProperty(ref _greenGrantStreak, value);
    }

    [CreateProperty] public int TransitGrantStreak
    {
        get => _transitGrantStreak;
        set => SetProperty(ref _transitGrantStreak, value);
    }

    [CreateProperty] public int LifeGrantStreak
    {
        get => _lifeGrantStreak;
        set => SetProperty(ref _lifeGrantStreak, value);
    }

    [CreateProperty] public int DevGrantStreak
    {
        get => _devGrantStreak;
        set => SetProperty(ref _devGrantStreak, value);
    }

    [CreateProperty] public bool GrantsEligible
    {
        get => _grantsEligible;
        set => SetProperty(ref _grantsEligible, value);
    }
    
    
    //  Crisis tracking
    [SerializeField] private int _ticksAtDebtCap;
    [SerializeField] private int _ticksBelowHappiness20;
    [SerializeField] private float _totalCitySpending;

    [CreateProperty] public int TicksAtDebtCap
    {
        get => _ticksAtDebtCap;
        set => SetProperty(ref _ticksAtDebtCap, value);
    }

    [CreateProperty] public int TicksBelowHappiness20
    {
        get => _ticksBelowHappiness20;
        set => SetProperty(ref _ticksBelowHappiness20, value);
    }

    [CreateProperty] public float TotalCitySpending
    {
        get => _totalCitySpending;
        set => SetProperty(ref _totalCitySpending, value);
    }

    [CreateProperty] public int CrisisTotal => _ticksAtDebtCap + _ticksBelowHappiness20;

    [CreateProperty] public float CrisisAvoidance =>
        Mathf.Max(0f, 100f - CrisisTotal * SimulationConstants.K_CRISIS_PENALTY);
    
    
    //  Updaters
    public void UpdateFromGameState(GameState state)
    {
        CityReputation = (int)state.cityMetrics.cityReputation;
        SharedInfraQuality = (int)state.cityMetrics.sharedInfraQuality;
        MetroPopulationPool = (int)state.cityMetrics.metroPopulationPool;

        CurrentMonth = state.currentMonth;
        CurrentTick = state.currentTick;

        GameSpeed = state.gameSpeed;
        IsPaused = state.isPaused;

        Notify(nameof(MetroInflowPercent));
        Notify(nameof(MonthDisplay));
        Notify(nameof(TickDisplay));
    }

    public void UpdateFromDistrictState(DistrictState state)
    {
        Gdp = state.gdp;
        Happiness = state.happiness;
        Population = state.population;
        Infrastructure = state.infrastructure;
        Sustainability = state.sustainability;

        Debt = state.debt;
        Reserve = state.reserve;
        Revenue = state.revenue;
        TotalSpending = state.totalSpending;
        ScaleFactor = state.scaleFactor;

        GreenGrantStreak = state.greenGrantStreak;
        TransitGrantStreak = state.transitGrantStreak;
        LifeGrantStreak = state.lifeGrantStreak;
        DevGrantStreak = state.devGrantStreak;
        GrantsEligible = state.grantsEligible;

        TicksAtDebtCap = state.ticksAtDebtCap;
        TicksBelowHappiness20 = state.ticksBelowHappiness20;
        TotalCitySpending = state.totalCitySpending;

        Notify(nameof(BudgetSurplus));
        Notify(nameof(Efficiency));
        Notify(nameof(CrisisTotal));
        Notify(nameof(CrisisAvoidance));
    }

    // Button events
    public void BindToPanel(VisualElement root)
    {
        var speed1 = root.Q<Button>("Speed1Btn");
        var speed2 = root.Q<Button>("Speed2Btn");
        var speed3 = root.Q<Button>("Speed3Btn");
        var pause  = root.Q<Button>("PauseBtn");
        var quit   = root.Q<Button>("QuitButton");

        if (speed1 != null)
        {
            speed1.clicked += () => OnSpeedChangeRequested?.Invoke(1);
 
        }

        if (speed2 != null)
        {
            speed2.clicked += () => OnSpeedChangeRequested?.Invoke(2);
        }

        if (speed3 != null)
        {
            speed3.clicked += () => OnSpeedChangeRequested?.Invoke(3);
        }

        if (pause != null)
        {
            pause.clicked  += () => OnPauseChangeRequested?.Invoke(!_isPaused);
        }

        if (quit != null)
        {
            quit.clicked   += () => OnQuitRequested?.Invoke();
        }
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
