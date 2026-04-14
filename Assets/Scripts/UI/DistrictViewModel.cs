// View-model for per-district data, designed for UI Toolkit data binding.
// Set as dataSource on any VisualElement subtree whose children need district metrics.
//
// Binding paths (for UIBuilder):
//   IndicatorBar:  Gdp, Reserve, Revenue, Population, Happiness, Infrastructure, Sustainability
//   TopBar budget: Revenue, TotalSpending, BudgetSurplus, Efficiency
//   DetailsPanel:  TicksAtDebtCap, TicksBelowHappiness20, CrisisTotal, CrisisAvoidance,
//                  GreenGrantStreak, TransitGrantStreak, LifeGrantStreak, DevGrantStreak,
//                  GrantsEligible, TotalCitySpending

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Simulation;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "DistrictViewModel", menuName = "District View Model")]
public class DistrictViewModel : ScriptableObject, INotifyBindablePropertyChanged
{
    public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
    
    private static readonly Color ColGdp =     new(0.30f, 0.72f, 0.91f);
    private static readonly Color ColHappy =   new(0.91f, 0.75f, 0.19f);
    private static readonly Color ColPop =     new(0.38f, 0.78f, 0.38f);
    private static readonly Color ColInfra =   new(0.63f, 0.44f, 0.82f);
    private static readonly Color ColSustain = new(0.25f, 0.80f, 0.60f);
    private static readonly Color ColDebt =    new(0.88f, 0.31f, 0.31f);
    
    // ── Primary metrics ──

    [SerializeField] private float _gdp;
    [SerializeField] private float _happiness;
    [SerializeField] private float _population;
    [SerializeField] private float _infrastructure;
    [SerializeField] private float _sustainability;

    [CreateProperty]
    public float Gdp
    {
        get => _gdp;
        set => SetProperty(ref _gdp, value);
    }

    [CreateProperty]
    public float Happiness
    {
        get => _happiness;
        set => SetProperty(ref _happiness, value);
    }

    [CreateProperty]
    public float Population
    {
        get => _population;
        set => SetProperty(ref _population, value);
    }

    [CreateProperty]
    public float Infrastructure
    {
        get => _infrastructure;
        set => SetProperty(ref _infrastructure, value);
    }

    [CreateProperty]
    public float Sustainability
    {
        get => _sustainability;
        set => SetProperty(ref _sustainability, value);
    }

    // ── Fiscal ──

    [SerializeField] private float _debt;
    [SerializeField] private float _reserve;
    [SerializeField] private float _revenue;
    [SerializeField] private float _totalSpending;
    [SerializeField] private float _scaleFactor;

    [CreateProperty]
    public float Debt
    {
        get => _debt;
        set => SetProperty(ref _debt, value);
    }

    [CreateProperty]
    public float Reserve
    {
        get => _reserve;
        set => SetProperty(ref _reserve, value);
    }

    [CreateProperty]
    public float Revenue
    {
        get => _revenue;
        set => SetProperty(ref _revenue, value);
    }

    [CreateProperty]
    public float TotalSpending
    {
        get => _totalSpending;
        set => SetProperty(ref _totalSpending, value);
    }

    [CreateProperty]
    public float ScaleFactor
    {
        get => _scaleFactor;
        set => SetProperty(ref _scaleFactor, value);
    }

    // ── Computed budget display ──

    [CreateProperty]
    public float BudgetSurplus => _revenue - _totalSpending;

    [CreateProperty]
    public float Efficiency => _scaleFactor * 100f;

    // ── Grant streaks ──

    [SerializeField] private int _greenGrantStreak;
    [SerializeField] private int _transitGrantStreak;
    [SerializeField] private int _lifeGrantStreak;
    [SerializeField] private int _devGrantStreak;
    [SerializeField] private bool _grantsEligible;

    [CreateProperty]
    public int GreenGrantStreak
    {
        get => _greenGrantStreak;
        set => SetProperty(ref _greenGrantStreak, value);
    }

    [CreateProperty]
    public int TransitGrantStreak
    {
        get => _transitGrantStreak;
        set => SetProperty(ref _transitGrantStreak, value);
    }

    [CreateProperty]
    public int LifeGrantStreak
    {
        get => _lifeGrantStreak;
        set => SetProperty(ref _lifeGrantStreak, value);
    }

    [CreateProperty]
    public int DevGrantStreak
    {
        get => _devGrantStreak;
        set => SetProperty(ref _devGrantStreak, value);
    }

    [CreateProperty]
    public bool GrantsEligible
    {
        get => _grantsEligible;
        set => SetProperty(ref _grantsEligible, value);
    }

    // ── Crisis tracking ──

    [SerializeField] private int _ticksAtDebtCap;
    [SerializeField] private int _ticksBelowHappiness20;
    [SerializeField] private float _totalCitySpending;

    [CreateProperty]
    public int TicksAtDebtCap
    {
        get => _ticksAtDebtCap;
        set => SetProperty(ref _ticksAtDebtCap, value);
    }

    [CreateProperty]
    public int TicksBelowHappiness20
    {
        get => _ticksBelowHappiness20;
        set => SetProperty(ref _ticksBelowHappiness20, value);
    }

    [CreateProperty]
    public float TotalCitySpending
    {
        get => _totalCitySpending;
        set => SetProperty(ref _totalCitySpending, value);
    }

    // ── Computed scoring display ──

    [CreateProperty]
    public int CrisisTotal => _ticksAtDebtCap + _ticksBelowHappiness20;

    [CreateProperty]
    public float CrisisAvoidance =>
        Mathf.Max(0f, 100f - CrisisTotal * SimulationConstants.K_CRISIS_PENALTY);

    // ── Bulk update from simulation state ──

    public void UpdateFromState(DistrictState state)
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

        // Computed properties depend on fields set above; notify them explicitly.
        Notify(nameof(BudgetSurplus));
        Notify(nameof(Efficiency));
        Notify(nameof(CrisisTotal));
        Notify(nameof(CrisisAvoidance));
    }

    // ── Change notification plumbing ──

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
