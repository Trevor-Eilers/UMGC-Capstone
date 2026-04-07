using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UI;

public class GameManager : MonoBehaviour
{
    private GameState _gameState;
    private PolicyValues _policyValues;
    private float _tickTimer;
    private bool _gameOver;

    // Root
    private VisualElement _root;

    // Metric indicators
    private IndicatorWidget _gdpIndicator;
    private IndicatorWidget _happyIndicator;
    private IndicatorWidget _popIndicator;
    private IndicatorWidget _infraIndicator;
    private IndicatorWidget _sustainIndicator;
    private IndicatorWidget _debtIndicator;

    // Metric bar colors
    private static readonly Color ColGdp =     new(0.30f, 0.72f, 0.91f);
    private static readonly Color ColHappy =   new(0.91f, 0.75f, 0.19f);
    private static readonly Color ColPop =     new(0.38f, 0.78f, 0.38f);
    private static readonly Color ColInfra =   new(0.63f, 0.44f, 0.82f);
    private static readonly Color ColSustain = new(0.25f, 0.80f, 0.60f);
    private static readonly Color ColDebt =    new(0.88f, 0.31f, 0.31f);

    // Budget labels
    private Label _revenueValue;
    private Label _spendingValue;
    private Label _surplusValue;
    private Label _reserveValue;
    private Label _efficiencyValue;

    // Slider value labels
    private Label _taxValue;
    private Label _eduValue;
    private Label _infraValue;
    private Label _housingValue;
    private Label _envValue;
    private Label _cityValue;

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

    // Grants
    private VisualElement _greenGrant;
    private VisualElement _transitGrant;
    private VisualElement _lifeGrant;
    private VisualElement _devGrant;

    // Details overlay
    private VisualElement _detailsOverlay;
    private Label _neighborhoodScore;
    private Label _cityContribScore;
    private Label _finalScore;
    private Label _debtCapTicks;
    private Label _lowHappyTicks;
    private Label _crisisTotal;
    private Label _crisisAvoidance;
    private Label _greenStreak;
    private Label _transitStreak;
    private Label _lifeStreak;
    private Label _devStreak;
    private Label _grantsEligible;
    private Label _totalCitySpend;
    private Label _citySpendShare;

    // Map visual controller
    private BuildingGenerator _mapVisuals;

    void Start()
    {
        _gameState = GameState.NewGame(4);
        // _policyValues = FindAnyObjectByType<PolicyValues>();
        _mapVisuals = FindAnyObjectByType<BuildingGenerator>();



        CacheUIReferences();
        RegisterSpeedButtons();
        RegisterDetailsButton();
        RegisterSliderValueLabels();

        UpdateAllUI();
    }

    private void CacheUIReferences()
    {
        // Metrics
        _gdpIndicator = _root.Q<IndicatorWidget>("GdpIndicator");
        _happyIndicator = _root.Q<IndicatorWidget>("HappyIndicator");
        _popIndicator = _root.Q<IndicatorWidget>("PopIndicator");
        _infraIndicator = _root.Q<IndicatorWidget>("InfraIndicator");
        _sustainIndicator = _root.Q<IndicatorWidget>("SustainIndicator");
        _debtIndicator = _root.Q<IndicatorWidget>("DebtIndicator");

        // Budget
        _revenueValue = _root.Q<Label>("RevenueValue");
        _spendingValue = _root.Q<Label>("SpendingValue");
        _surplusValue = _root.Q<Label>("SurplusValue");
        _reserveValue = _root.Q<Label>("ReserveValue");
        _efficiencyValue = _root.Q<Label>("EfficiencyValue");

        // Slider values
        _taxValue = _root.Q<Label>("TaxValue");
        _eduValue = _root.Q<Label>("EduValue");
        _infraValue = _root.Q<Label>("InfraValue");
        _housingValue = _root.Q<Label>("HousingValue");
        _envValue = _root.Q<Label>("EnvValue");
        _cityValue = _root.Q<Label>("CityValue");

        // Top bar
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

        // Grants
        _greenGrant = _root.Q<VisualElement>("GreenGrant");
        _transitGrant = _root.Q<VisualElement>("TransitGrant");
        _lifeGrant = _root.Q<VisualElement>("LifeGrant");
        _devGrant = _root.Q<VisualElement>("DevGrant");

        // Details
        _detailsOverlay = _root.Q<VisualElement>("DetailsOverlay");
        _neighborhoodScore = _root.Q<Label>("NeighborhoodScore");
        _cityContribScore = _root.Q<Label>("CityContribScore");
        _finalScore = _root.Q<Label>("FinalScore");
        _debtCapTicks = _root.Q<Label>("DebtCapTicks");
        _lowHappyTicks = _root.Q<Label>("LowHappyTicks");
        _crisisTotal = _root.Q<Label>("CrisisTotal");
        _crisisAvoidance = _root.Q<Label>("CrisisAvoidance");
        _greenStreak = _root.Q<Label>("GreenStreak");
        _transitStreak = _root.Q<Label>("TransitStreak");
        _lifeStreak = _root.Q<Label>("LifeStreak");
        _devStreak = _root.Q<Label>("DevStreak");
        _grantsEligible = _root.Q<Label>("GrantsEligible");
        _totalCitySpend = _root.Q<Label>("TotalCitySpend");
        _citySpendShare = _root.Q<Label>("CitySpendShare");
    }

    private void RegisterSpeedButtons()
    {
        _speed1Btn?.RegisterCallback<ClickEvent>(_ => SetSpeed(1f));
        _speed2Btn?.RegisterCallback<ClickEvent>(_ => SetSpeed(2f));
        _speed3Btn?.RegisterCallback<ClickEvent>(_ => SetSpeed(3f));
        _pauseBtn?.RegisterCallback<ClickEvent>(_ => {
            _gameState.isPaused = !_gameState.isPaused;
            UpdateSpeedButtons();
        });
    }

    private void RegisterDetailsButton()
    {
        var detailsBtn = _root.Q<Button>("DetailsButton");
        detailsBtn?.RegisterCallback<ClickEvent>(_ => ToggleDetails());

        var closeBtn = _root.Q<Button>("CloseDetailsBtn");
        closeBtn?.RegisterCallback<ClickEvent>(_ => ToggleDetails());
    }

    private void RegisterSliderValueLabels()
    {
        RegisterSliderLabel("TaxSlider", _taxValue, "{0:F0}%");
        RegisterSliderLabel("EduSlider", _eduValue, "{0:F0}");
        RegisterSliderLabel("InfraSlider", _infraValue, "{0:F0}");
        RegisterSliderLabel("HousingSlider", _housingValue, "{0:F0}");
        RegisterSliderLabel("EnvSlider", _envValue, "{0:F0}");
        RegisterSliderLabel("CitySlider", _cityValue, "{0:F0}");
    }

    private void RegisterSliderLabel(string sliderName, Label label, string format)
    {
        var slider = _root.Q<Slider>(sliderName);
        if (slider == null || label == null) return;
        slider.RegisterValueChangedCallback(evt => label.text = string.Format(format, evt.newValue));
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) SetSpeed(1f);
        if (kb.digit2Key.wasPressedThisFrame) SetSpeed(2f);
        if (kb.digit3Key.wasPressedThisFrame) SetSpeed(3f);
        if (kb.spaceKey.wasPressedThisFrame)
        {
            _gameState.isPaused = !_gameState.isPaused;
            UpdateSpeedButtons();
        }
        if (kb.tabKey.wasPressedThisFrame) ToggleDetails();

        if (_gameState.isPaused || _gameOver) return;

        float tickInterval = 3.125f / _gameState.gameSpeed;
        _tickTimer += Time.deltaTime;

        if (_tickTimer >= tickInterval)
        {
            _tickTimer -= tickInterval;
            ResolveTick();
        }
    }

    private void ResolveTick()
    {
        _gameState.districts[0].values = new PolicyValues
        {
            taxRate = _policyValues.taxRate,
            education = _policyValues.education,
            infrastructure = _policyValues.infrastructure,
            housing = _policyValues.housing,
            environment = _policyValues.environment,
            cityContribution = _policyValues.cityContribution
        };

        _gameState = TickProcessor.ResolveTick(_gameState);
        UpdateAllUI();

        if (_gameState.currentTick >= 576)
        {
            _gameOver = true;
            LogFinalScores();
        }
    }

    private void UpdateAllUI()
    {
        UpdateIndicators();
        UpdateBudget();
        UpdateTopBar();
        UpdateGrants();
        UpdateMapVisuals();
        if (_detailsOverlay != null && _detailsOverlay.style.display == DisplayStyle.Flex)
            UpdateDetails();
    }

    private void UpdateIndicators()
    {
        DistrictState d = _gameState.districts[0];

        SetIndicator(_gdpIndicator, d.gdp.ToString("F1"), d.gdp, 100f, ColGdp);
        SetIndicator(_happyIndicator, d.happiness.ToString("F1"), d.happiness, 100f, ColHappy);
        SetIndicator(_popIndicator, $"{d.population:F1}k", d.population / 3f, 100f, ColPop);
        SetIndicator(_infraIndicator, d.infrastructure.ToString("F1"), d.infrastructure, 100f, ColInfra);
        SetIndicator(_sustainIndicator, d.sustainability.ToString("F1"), d.sustainability, 100f, ColSustain);
        SetIndicator(_debtIndicator, $"{d.debt:F1} / 60", d.debt / 80f * 100f, 100f, ColDebt);
    }

    private static void SetIndicator(IndicatorWidget widget, string text, float fillPct, float max, Color color)
    {
        if (widget == null) return;
        widget.Value = text;
        widget.SetFill(Mathf.Clamp(fillPct, 0f, max), color);
    }

    private void UpdateBudget()
    {
        DistrictState d = _gameState.districts[0];
        float surplus = d.revenue - d.totalSpending;

        SetLabel(_revenueValue, $"${d.revenue:F0}");
        SetLabel(_spendingValue, $"${d.totalSpending:F0}");

        if (_surplusValue != null)
        {
            _surplusValue.text = surplus >= 0 ? $"+${surplus:F0}" : $"-${Math.Abs(surplus):F0}";
            _surplusValue.RemoveFromClassList("budget-surplus");
            _surplusValue.RemoveFromClassList("budget-deficit");
            _surplusValue.AddToClassList(surplus >= 0 ? "budget-surplus" : "budget-deficit");
        }

        SetLabel(_reserveValue, $"${d.reserve:F0}");
        SetLabel(_efficiencyValue, $"{d.scaleFactor * 100f:F0}%");
    }

    private void UpdateTopBar()
    {
        CityMetrics cm = _gameState.cityMetrics;

        SetLabel(_cityRepValue, cm.cityReputation.ToString("F1"));
        SetBarWidth(_cityRepBar, cm.cityReputation);
        SetLabel(_sharedInfraValue, cm.sharedInfraQuality.ToString("F1"));
        SetBarWidth(_sharedInfraBar, cm.sharedInfraQuality);
        SetLabel(_metroInflowValue, $"{cm.metroPopulationPool:+0.0;-0.0}k");
        SetLabel(_monthLabel, $"Month {_gameState.currentMonth} / 48");
        SetLabel(_tickLabel, $"Tick {_gameState.currentTick} / 576");
    }

    private void UpdateGrants()
    {
        DistrictState d = _gameState.districts[0];
        SetGrantActive(_greenGrant, d.sustainability > 70f && d.grantsEligible);
        SetGrantActive(_transitGrant, d.population > 300f && d.grantsEligible);
        SetGrantActive(_lifeGrant, d.happiness > 75f && d.grantsEligible);
        SetGrantActive(_devGrant, d.infrastructure > 80f && d.grantsEligible);
    }

    private static void SetGrantActive(VisualElement badge, bool active)
    {
        if (badge == null) return;
        badge.EnableInClassList("grant-badge-active", active);
    }

    private void UpdateDetails()
    {
        DistrictState d = _gameState.districts[0];

        FinalScore score = ScoringSystem.ComputeFinalScore(
            d, _gameState.cityMetrics, _gameState.districts, _gameState.numActivePlayers);

        SetLabel(_neighborhoodScore, score.neighborhoodScore.ToString("F1"));
        SetLabel(_cityContribScore, score.cityContribScore.ToString("F1"));
        SetLabel(_finalScore, score.finalScore.ToString("F1"));

        SetLabel(_debtCapTicks, d.ticksAtDebtCap.ToString());
        SetLabel(_lowHappyTicks, d.ticksBelowHappiness20.ToString());
        int crisisTotal = d.ticksAtDebtCap + d.ticksBelowHappiness20;
        SetLabel(_crisisTotal, crisisTotal.ToString());
        float crisisAvoid = Math.Max(0f, 100f - crisisTotal * SimulationConstants.K_CRISIS_PENALTY);
        SetLabel(_crisisAvoidance, crisisAvoid.ToString("F1"));

        SetLabel(_greenStreak, $"{d.greenGrantStreak} ticks");
        SetLabel(_transitStreak, $"{d.transitGrantStreak} ticks");
        SetLabel(_lifeStreak, $"{d.lifeGrantStreak} ticks");
        SetLabel(_devStreak, $"{d.devGrantStreak} ticks");
        SetLabel(_grantsEligible, d.grantsEligible ? "Yes" : "No");

        float totalAll = 0f;
        for (int i = 0; i < _gameState.numActivePlayers; i++)
            totalAll += _gameState.districts[i].totalCitySpending;

        SetLabel(_totalCitySpend, $"${d.totalCitySpending:F0}");
        float share = totalAll > 0f ? d.totalCitySpending / totalAll * 100f : 25f;
        SetLabel(_citySpendShare, $"{share:F1}%");
    }

    private void ToggleDetails()
    {
        if (_detailsOverlay == null) return;
        bool visible = _detailsOverlay.style.display == DisplayStyle.Flex;
        _detailsOverlay.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
        if (!visible) UpdateDetails();
    }

    private void UpdateSpeedButtons()
    {
        _speed1Btn?.EnableInClassList("speed-btn-active", !_gameState.isPaused && _gameState.gameSpeed == 1f);
        _speed2Btn?.EnableInClassList("speed-btn-active", !_gameState.isPaused && _gameState.gameSpeed == 2f);
        _speed3Btn?.EnableInClassList("speed-btn-active", !_gameState.isPaused && _gameState.gameSpeed == 3f);
        _pauseBtn?.EnableInClassList("speed-btn-active", _gameState.isPaused);
    }

    private void UpdateMapVisuals()
    {
        if (_mapVisuals == null) return;
        for (int i = 0; i < _gameState.numActivePlayers; i++)
            _mapVisuals.UpdateDistrict(i, _gameState.districts[i]);
    }

    public void SetSpeed(float speed)
    {
        _gameState.gameSpeed = speed;
        _gameState.isPaused = false;
        UpdateSpeedButtons();
    }

    private void LogFinalScores()
    {
        Debug.Log("=== GAME OVER — Final Scores ===");
        for (int i = 0; i < _gameState.numActivePlayers; i++)
        {
            FinalScore score = ScoringSystem.ComputeFinalScore(
                _gameState.districts[i], _gameState.cityMetrics,
                _gameState.districts, _gameState.numActivePlayers);
            Debug.Log($"District {i}: Neighborhood={score.neighborhoodScore:F1} City={score.cityContribScore:F1} Final={score.finalScore:F1}");
        }
    }

    private static void SetLabel(Label label, string text)
    {
        if (label != null) label.text = text;
    }

    private static void SetBarWidth(VisualElement bar, float percent)
    {
        if (bar != null) bar.style.width = Length.Percent(Mathf.Clamp(percent, 0f, 100f));
    }
}
