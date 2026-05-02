// Author: Trevor Eilers

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Simulation;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    // View-model for the Details panel (score preview + per-district stats).
    //
    // Pure data holder — no calculations happen here. Per-tick district state flows
    // in through UpdateFromDistrictState. Final-score values are populated once at
    // end of game via SetFinalScore, which the Presenter/GameManager forwards from
    // ScoringSystem.ComputeFinalScore output.
    [CreateAssetMenu(fileName = "DetailsPanelViewModel", menuName = "Details Panel View Model")]
    public class DetailsPanelViewModel : ScriptableObject,
        INotifyBindablePropertyChanged, IDistrictBoundViewModel
    {
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
        public event Action OnCloseRequested;

        // ── Score preview (end-of-game) ───────────────────────────────────────────
        [SerializeField] private float _neighborhoodScore;
        [SerializeField] private float _cityContribScore;
        [SerializeField] private float _finalScore;

        [CreateProperty] public float NeighborhoodScore
        {
            get => _neighborhoodScore;
            set => SetProperty(ref _neighborhoodScore, value);
        }

        [CreateProperty] public float CityContribScore
        {
            get => _cityContribScore;
            set => SetProperty(ref _cityContribScore, value);
        }

        [CreateProperty] public float FinalScore
        {
            get => _finalScore;
            set => SetProperty(ref _finalScore, value);
        }

        [CreateProperty] public string NeighborhoodScoreDisplay => _neighborhoodScore.ToString("F1");
        [CreateProperty] public string CityContribScoreDisplay  => _cityContribScore.ToString("F1");
        [CreateProperty] public string FinalScoreDisplay        => _finalScore.ToString("F1");

        // ── Crisis tracking (live, from DistrictState) ────────────────────────────
        [SerializeField] private int _ticksAtDebtCap;
        [SerializeField] private int _ticksBelowHappiness20;

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

        // Trivial pass-through aggregates — same style TopBarViewModel uses for its
        // CrisisAvoidance computed property (TopBarViewModel.cs:286-287).
        [CreateProperty] public int CrisisTotal => _ticksAtDebtCap + _ticksBelowHappiness20;

        [CreateProperty] public float CrisisAvoidance =>
            Mathf.Max(0f, 100f - CrisisTotal * SimulationConstants.K_CRISIS_PENALTY);

        [CreateProperty] public string CrisisAvoidanceDisplay => CrisisAvoidance.ToString("F1");

        // ── Grant streaks (live, from DistrictState) ──────────────────────────────
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

        [CreateProperty] public string GreenStreakDisplay    => $"{_greenGrantStreak} ticks";
        [CreateProperty] public string TransitStreakDisplay  => $"{_transitGrantStreak} ticks";
        [CreateProperty] public string LifeStreakDisplay     => $"{_lifeGrantStreak} ticks";
        [CreateProperty] public string DevStreakDisplay      => $"{_devGrantStreak} ticks";
        [CreateProperty] public string GrantsEligibleDisplay => _grantsEligible ? "Yes" : "No";

        // ── City contributions ────────────────────────────────────────────────────
        [SerializeField] private float _totalCitySpending;
        [SerializeField] private float _citySpendSharePercent;

        [CreateProperty] public float TotalCitySpending
        {
            get => _totalCitySpending;
            set => SetProperty(ref _totalCitySpending, value);
        }

        [CreateProperty] public float CitySpendSharePercent
        {
            get => _citySpendSharePercent;
            set => SetProperty(ref _citySpendSharePercent, value);
        }

        [CreateProperty] public string CitySpendShareDisplay => $"{_citySpendSharePercent:F1}%";


        // ── IDistrictBoundViewModel.UpdateFromDistrictState ───────────────────────
        // Populates fields that come directly from a single DistrictState. Fires each
        // tick via DistrictBoundPresenter. No ScoringSystem calls, no cross-district
        // math — the Presenter base class hands us the new state and we store it.
        public void UpdateFromDistrictState(DistrictState state)
        {
            TicksAtDebtCap = state.ticksAtDebtCap;
            TicksBelowHappiness20 = state.ticksBelowHappiness20;

            GreenGrantStreak = state.greenGrantStreak;
            TransitGrantStreak = state.transitGrantStreak;
            LifeGrantStreak = state.lifeGrantStreak;
            DevGrantStreak = state.devGrantStreak;
            GrantsEligible = state.grantsEligible;

            TotalCitySpending = state.totalCitySpending;

            // Re-notify computed properties that depend on the fields we just wrote.
            Notify(nameof(CrisisTotal));
            Notify(nameof(CrisisAvoidance));
            Notify(nameof(CrisisAvoidanceDisplay));
            Notify(nameof(GreenStreakDisplay));
            Notify(nameof(TransitStreakDisplay));
            Notify(nameof(LifeStreakDisplay));
            Notify(nameof(DevStreakDisplay));
            Notify(nameof(GrantsEligibleDisplay));
        }

        // ── End-of-game score populator ───────────────────────────────────────────
        // Called once by the Presenter after GameManager.EndGameRpc has computed the
        // FinalScore via ScoringSystem.ComputeFinalScore. The VM just stores and
        // notifies — the calculation lives entirely in the Model layer.
        public void SetFinalScore(FinalScore score, float citySpendSharePercent)
        {
            NeighborhoodScore = score.neighborhoodScore;
            CityContribScore  = score.cityContribScore;
            FinalScore        = score.finalScore;
            CitySpendSharePercent = citySpendSharePercent;

            Notify(nameof(NeighborhoodScoreDisplay));
            Notify(nameof(CityContribScoreDisplay));
            Notify(nameof(FinalScoreDisplay));
            Notify(nameof(CitySpendShareDisplay));
        }

        // ── IDistrictBoundViewModel.BindToPanel ───────────────────────────────────
        // Wires the close button to the OnCloseRequested event. The Presenter
        // subscribes to that event and hides the overlay.
        public void BindToPanel(VisualElement root)
        {
            var closeBtn = root.Q<Button>("CloseDetailsBtn");
            if (closeBtn != null)
                closeBtn.clicked += () => OnCloseRequested?.Invoke();
        }


        // ── Notification helpers (verbatim from TopBarViewModel.cs:404-415) ───────
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
}
