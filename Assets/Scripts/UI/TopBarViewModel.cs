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
    [SerializeField] private int _prevCityReputation;
    [SerializeField] private int _prevSharedInfraQuality;

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

    [CreateProperty] public float CityReputationDelta => _cityReputation - _prevCityReputation;
    [CreateProperty] public float SharedInfraQualityDelta => _sharedInfraQuality - _prevSharedInfraQuality;

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

    // Previous-tick cache — used to compute per-tick deltas for HUD chips.
    [SerializeField] private float _prevGdp;
    [SerializeField] private float _prevHappiness;
    [SerializeField] private float _prevPopulation;
    [SerializeField] private float _prevInfrastructure;
    [SerializeField] private float _prevSustainability;
    [SerializeField] private float _prevDebt;
    [SerializeField] private float _prevReserve;
    [SerializeField] private float _prevRevenue;
    [SerializeField] private float _prevTotalSpending;
    private bool _deltaSeedReady;

    // Locally-computed pollution index (0-100). The sim doesn't persist pollution on
    // DistrictState (it's an ephemeral spillover each tick), so we mirror the formula
    // here for display only. See CLAUDE.md §3.2.
    [SerializeField] private float _pollution;
    [SerializeField] private float _prevPollution;

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

    [CreateProperty] public float Pollution
    {
        get => _pollution;
        set => SetProperty(ref _pollution, value);
    }

    // Per-tick deltas — 0 until the first two ticks have arrived (no baseline to diff against).
    [CreateProperty] public float GdpDelta            => _deltaSeedReady ? _gdp - _prevGdp : 0f;
    [CreateProperty] public float HappinessDelta      => _deltaSeedReady ? _happiness - _prevHappiness : 0f;
    [CreateProperty] public float PopulationDelta     => _deltaSeedReady ? _population - _prevPopulation : 0f;
    [CreateProperty] public float InfrastructureDelta => _deltaSeedReady ? _infrastructure - _prevInfrastructure : 0f;
    [CreateProperty] public float SustainabilityDelta => _deltaSeedReady ? _sustainability - _prevSustainability : 0f;
    [CreateProperty] public float DebtDelta           => _deltaSeedReady ? _debt - _prevDebt : 0f;
    [CreateProperty] public float ReserveDelta        => _deltaSeedReady ? _reserve - _prevReserve : 0f;
    [CreateProperty] public float RevenueDelta        => _deltaSeedReady ? _revenue - _prevRevenue : 0f;
    [CreateProperty] public float BudgetSurplusDelta  => _deltaSeedReady
        ? (_revenue - _totalSpending) - (_prevRevenue - _prevTotalSpending)
        : 0f;
    [CreateProperty] public float PollutionDelta      => _deltaSeedReady ? _pollution - _prevPollution : 0f;


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
        // Snapshot city-metric deltas before overwriting.
        _prevCityReputation = _cityReputation;
        _prevSharedInfraQuality = _sharedInfraQuality;

        CityReputation = (int)state.cityMetrics.cityReputation;
        SharedInfraQuality = (int)state.cityMetrics.sharedInfraQuality;
        MetroPopulationPool = (int)state.cityMetrics.metroPopulationPool;

        CurrentMonth = state.currentMonth;
        CurrentTick = state.currentTick;

        GameSpeed = state.gameSpeed;
        IsPaused = state.isPaused;

        Notify(nameof(MetroInflowPercent));
        Notify(nameof(CityReputationDelta));
        Notify(nameof(SharedInfraQualityDelta));
        Notify(nameof(MonthDisplay));
        Notify(nameof(TickDisplay));
    }

    public void UpdateFromDistrictState(DistrictState state)
    {
        // Snapshot everything we diff on the next tick.
        _prevGdp = _gdp;
        _prevHappiness = _happiness;
        _prevPopulation = _population;
        _prevInfrastructure = _infrastructure;
        _prevSustainability = _sustainability;
        _prevDebt = _debt;
        _prevReserve = _reserve;
        _prevRevenue = _revenue;
        _prevTotalSpending = _totalSpending;
        _prevPollution = _pollution;

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

        // Mirror the sim's pollution formula for UI display. Matches CLAUDE.md §3.2:
        // pollution output = max(0, POLLUTE_ENV_THRESHOLD - env) + max(0, gdp - POLLUTE_GDP_THRESHOLD)
        // scaled to a 0-100 index for the HUD. No side effects on the sim.
        float envShortfall = Mathf.Max(0f, SimulationConstants.POLLUTE_ENV_THRESHOLD - state.policyValues.environment);
        float gdpExcess    = Mathf.Max(0f, state.gdp - SimulationConstants.POLLUTE_GDP_THRESHOLD);
        float raw          = envShortfall + gdpExcess;
        // Raw max is 30 (env 0) + 60 (gdp 100) = 90. Map to 0-100 with a soft ceiling.
        Pollution = Mathf.Clamp(raw * 100f / 90f, 0f, 100f);

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

        // After the second call, deltas become meaningful. Before that, diffing against
        // zero-initialized prev values would produce a huge fake spike on tick 0.
        if (!_deltaSeedReady)
        {
            _deltaSeedReady = true;
        }
        else
        {
            Notify(nameof(GdpDelta));
            Notify(nameof(HappinessDelta));
            Notify(nameof(PopulationDelta));
            Notify(nameof(InfrastructureDelta));
            Notify(nameof(SustainabilityDelta));
            Notify(nameof(DebtDelta));
            Notify(nameof(ReserveDelta));
            Notify(nameof(RevenueDelta));
            Notify(nameof(BudgetSurplusDelta));
            Notify(nameof(PollutionDelta));
        }
    }

    // Button events
    public void BindToPanel(VisualElement root)
    {
        var speed1 = root.Q<Button>("Speed1Btn");
        var speed2 = root.Q<Button>("Speed2Btn");
        var speed3 = root.Q<Button>("Speed3Btn");
        var pause  = root.Q<Button>("PauseBtn");
        var quit   = root.Q<Button>("QuitButton");

        if (speed1 != null) speed1.clicked += () => OnSpeedChangeRequested?.Invoke(1);
        if (speed2 != null) speed2.clicked += () => OnSpeedChangeRequested?.Invoke(2);
        if (speed3 != null) speed3.clicked += () => OnSpeedChangeRequested?.Invoke(3);
        if (pause  != null) pause.clicked  += () => OnPauseChangeRequested?.Invoke(!_isPaused);
        if (quit != null)   quit.clicked   += () => OnQuitRequested?.Invoke();
    }


    // Notifications
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
