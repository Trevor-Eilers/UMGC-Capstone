// Author: Malcolm Bramble

#if UNITY_EDITOR

using System;
using NUnit.Framework;
using Simulation;

[TestFixture]
public class LocalEffectCalculatorTests
{
    // ──────────────────────────────────────────────
    // Save/restore all mutable constants between tests
    // ──────────────────────────────────────────────

    private float saved_K_EDU_TO_GDP, saved_K_INFRA_TO_GDP, saved_K_POP_TO_GDP;
    private float saved_K_SUSTAIN_TO_GDP, saved_K_TAX_GDP_DRAG, saved_K_ENV_GDP_DRAG;
    private float saved_K_GDP_DECAY;
    private float saved_W_HAPPY_GDP, saved_W_HAPPY_INFRA, saved_W_HAPPY_SUSTAIN, saved_W_HAPPY_DEBT;
    private float saved_K_BASELINE_WEIGHT, saved_K_HOUSING_TO_HAPPY;
    private float saved_K_TAX_HAPPY_PENALTY, saved_K_DEBT_STRESS, saved_K_HAPPY_SMOOTHING;
    private float saved_K_INFRA_TO_INFRA, saved_K_INFRA_DECAY;
    private float saved_K_INFRA_TO_SUSTAIN, saved_K_ENV_TO_SUSTAIN;
    private float saved_K_POP_SUSTAIN_DRAIN, saved_K_SUSTAIN_DECAY;
    private float saved_SUSTAIN_MIGRATION_THRESHOLD, saved_K_MIGRATION_RATE;
    private float saved_K_BASE, saved_K_HOUSING_CAP, saved_K_INFRA_CAP, saved_K_ENV_CAP;
    private float saved_K_SHARED_CAP, saved_K_REPUTATION_CAP;
    private float saved_K_OVERSHOOT_LINEAR, saved_K_OVERSHOOT_QUAD;
    private float saved_SUSTAIN_COLLAPSE_THRESHOLD, saved_K_SUSTAIN_COLLAPSE_RATE;
    private float saved_HAPPINESS_COLLAPSE_THRESHOLD, saved_K_HAPPINESS_COLLAPSE_RATE;

    [SetUp]
    public void SaveConstants()
    {
        saved_K_EDU_TO_GDP = SimulationConstants.K_EDU_TO_GDP;
        saved_K_INFRA_TO_GDP = SimulationConstants.K_INFRA_TO_GDP;
        saved_K_POP_TO_GDP = SimulationConstants.K_POP_TO_GDP;
        saved_K_SUSTAIN_TO_GDP = SimulationConstants.K_SUSTAIN_TO_GDP;
        saved_K_TAX_GDP_DRAG = SimulationConstants.K_TAX_GDP_DRAG;
        saved_K_ENV_GDP_DRAG = SimulationConstants.K_ENV_GDP_DRAG;
        saved_K_GDP_DECAY = SimulationConstants.K_GDP_DECAY;
        saved_W_HAPPY_GDP = SimulationConstants.W_HAPPY_GDP;
        saved_W_HAPPY_INFRA = SimulationConstants.W_HAPPY_INFRA;
        saved_W_HAPPY_SUSTAIN = SimulationConstants.W_HAPPY_SUSTAIN;
        saved_W_HAPPY_DEBT = SimulationConstants.W_HAPPY_DEBT;
        saved_K_BASELINE_WEIGHT = SimulationConstants.K_BASELINE_WEIGHT;
        saved_K_HOUSING_TO_HAPPY = SimulationConstants.K_HOUSING_TO_HAPPY;
        saved_K_TAX_HAPPY_PENALTY = SimulationConstants.K_TAX_HAPPY_PENALTY;
        saved_K_DEBT_STRESS = SimulationConstants.K_DEBT_STRESS;
        saved_K_HAPPY_SMOOTHING = SimulationConstants.K_HAPPY_SMOOTHING;
        saved_K_INFRA_TO_INFRA = SimulationConstants.K_INFRA_TO_INFRA;
        saved_K_INFRA_DECAY = SimulationConstants.K_INFRA_DECAY;
        saved_K_INFRA_TO_SUSTAIN = SimulationConstants.K_INFRA_TO_SUSTAIN;
        saved_K_ENV_TO_SUSTAIN = SimulationConstants.K_ENV_TO_SUSTAIN;
        saved_K_POP_SUSTAIN_DRAIN = SimulationConstants.K_POP_SUSTAIN_DRAIN;
        saved_K_SUSTAIN_DECAY = SimulationConstants.K_SUSTAIN_DECAY;
        saved_SUSTAIN_MIGRATION_THRESHOLD = SimulationConstants.SUSTAIN_MIGRATION_THRESHOLD;
        saved_K_MIGRATION_RATE = SimulationConstants.K_MIGRATION_RATE;
        saved_K_BASE = SimulationConstants.K_BASE;
        saved_K_HOUSING_CAP = SimulationConstants.K_HOUSING_CAP;
        saved_K_INFRA_CAP = SimulationConstants.K_INFRA_CAP;
        saved_K_ENV_CAP = SimulationConstants.K_ENV_CAP;
        saved_K_SHARED_CAP = SimulationConstants.K_SHARED_CAP;
        saved_K_REPUTATION_CAP = SimulationConstants.K_REPUTATION_CAP;
        saved_K_OVERSHOOT_LINEAR = SimulationConstants.K_OVERSHOOT_LINEAR;
        saved_K_OVERSHOOT_QUAD = SimulationConstants.K_OVERSHOOT_QUAD;
        saved_SUSTAIN_COLLAPSE_THRESHOLD = SimulationConstants.SUSTAIN_COLLAPSE_THRESHOLD;
        saved_K_SUSTAIN_COLLAPSE_RATE = SimulationConstants.K_SUSTAIN_COLLAPSE_RATE;
        saved_HAPPINESS_COLLAPSE_THRESHOLD = SimulationConstants.HAPPINESS_COLLAPSE_THRESHOLD;
        saved_K_HAPPINESS_COLLAPSE_RATE = SimulationConstants.K_HAPPINESS_COLLAPSE_RATE;
    }

    [TearDown]
    public void RestoreConstants()
    {
        SimulationConstants.K_EDU_TO_GDP = saved_K_EDU_TO_GDP;
        SimulationConstants.K_INFRA_TO_GDP = saved_K_INFRA_TO_GDP;
        SimulationConstants.K_POP_TO_GDP = saved_K_POP_TO_GDP;
        SimulationConstants.K_SUSTAIN_TO_GDP = saved_K_SUSTAIN_TO_GDP;
        SimulationConstants.K_TAX_GDP_DRAG = saved_K_TAX_GDP_DRAG;
        SimulationConstants.K_ENV_GDP_DRAG = saved_K_ENV_GDP_DRAG;
        SimulationConstants.K_GDP_DECAY = saved_K_GDP_DECAY;
        SimulationConstants.W_HAPPY_GDP = saved_W_HAPPY_GDP;
        SimulationConstants.W_HAPPY_INFRA = saved_W_HAPPY_INFRA;
        SimulationConstants.W_HAPPY_SUSTAIN = saved_W_HAPPY_SUSTAIN;
        SimulationConstants.W_HAPPY_DEBT = saved_W_HAPPY_DEBT;
        SimulationConstants.K_BASELINE_WEIGHT = saved_K_BASELINE_WEIGHT;
        SimulationConstants.K_HOUSING_TO_HAPPY = saved_K_HOUSING_TO_HAPPY;
        SimulationConstants.K_TAX_HAPPY_PENALTY = saved_K_TAX_HAPPY_PENALTY;
        SimulationConstants.K_DEBT_STRESS = saved_K_DEBT_STRESS;
        SimulationConstants.K_HAPPY_SMOOTHING = saved_K_HAPPY_SMOOTHING;
        SimulationConstants.K_INFRA_TO_INFRA = saved_K_INFRA_TO_INFRA;
        SimulationConstants.K_INFRA_DECAY = saved_K_INFRA_DECAY;
        SimulationConstants.K_INFRA_TO_SUSTAIN = saved_K_INFRA_TO_SUSTAIN;
        SimulationConstants.K_ENV_TO_SUSTAIN = saved_K_ENV_TO_SUSTAIN;
        SimulationConstants.K_POP_SUSTAIN_DRAIN = saved_K_POP_SUSTAIN_DRAIN;
        SimulationConstants.K_SUSTAIN_DECAY = saved_K_SUSTAIN_DECAY;
        SimulationConstants.SUSTAIN_MIGRATION_THRESHOLD = saved_SUSTAIN_MIGRATION_THRESHOLD;
        SimulationConstants.K_MIGRATION_RATE = saved_K_MIGRATION_RATE;
        SimulationConstants.K_BASE = saved_K_BASE;
        SimulationConstants.K_HOUSING_CAP = saved_K_HOUSING_CAP;
        SimulationConstants.K_INFRA_CAP = saved_K_INFRA_CAP;
        SimulationConstants.K_ENV_CAP = saved_K_ENV_CAP;
        SimulationConstants.K_SHARED_CAP = saved_K_SHARED_CAP;
        SimulationConstants.K_REPUTATION_CAP = saved_K_REPUTATION_CAP;
        SimulationConstants.K_OVERSHOOT_LINEAR = saved_K_OVERSHOOT_LINEAR;
        SimulationConstants.K_OVERSHOOT_QUAD = saved_K_OVERSHOOT_QUAD;
        SimulationConstants.SUSTAIN_COLLAPSE_THRESHOLD = saved_SUSTAIN_COLLAPSE_THRESHOLD;
        SimulationConstants.K_SUSTAIN_COLLAPSE_RATE = saved_K_SUSTAIN_COLLAPSE_RATE;
        SimulationConstants.HAPPINESS_COLLAPSE_THRESHOLD = saved_HAPPINESS_COLLAPSE_THRESHOLD;
        SimulationConstants.K_HAPPINESS_COLLAPSE_RATE = saved_K_HAPPINESS_COLLAPSE_RATE;
    }

    // ──────────────────────────────────────────────
    // Helper: create a district with controlled values
    // ──────────────────────────────────────────────

    private static DistrictState MakeDistrict(
        float gdp = 50f, float happiness = 55f, float population = 150f,
        float infrastructure = 50f, float sustainability = 55f,
        float debt = 15f, float taxRate = 15f)
    {
        var d = DistrictState.Default();
        d.gdp = gdp;
        d.happiness = happiness;
        d.population = population;
        d.infrastructure = infrastructure;
        d.sustainability = sustainability;
        d.debt = debt;
        d.policyValues.taxRate = taxRate;
        return d;
    }

    private static ScaledSpending MakeSpending(
        float edu = 0f, float infra = 0f, float housing = 0f,
        float env = 0f, float city = 0f)
    {
        return new ScaledSpending
        {
            actualEduCost = edu,
            actualInfraCost = infra,
            actualHousingCost = housing,
            actualEnvCost = env,
            actualCityCost = city,
            actualTotalSpending = edu + infra + housing + env + city,
            scaleFactor = 1.0f
        };
    }

    // ══════════════════════════════════════════════
    // GDP DIMINISHING RETURNS
    // ══════════════════════════════════════════════

    [Test]
    public void GdpDelta_DiminishingReturns_AtGdp50_QuartersPositiveDelta()
    {
        // Isolate: only education growth, zero everything else
        SimulationConstants.K_EDU_TO_GDP = 1.0f;
        SimulationConstants.K_INFRA_TO_GDP = 0f;
        SimulationConstants.K_POP_TO_GDP = 0f;
        SimulationConstants.K_SUSTAIN_TO_GDP = 0f;
        SimulationConstants.K_TAX_GDP_DRAG = 0f;
        SimulationConstants.K_ENV_GDP_DRAG = 0f;
        SimulationConstants.K_GDP_DECAY = 0f;

        var d = MakeDistrict(gdp: 50f, infrastructure: 50f, sustainability: 50f);
        var s = MakeSpending(edu: 10f);

        float delta = LocalEffectCalculator.ComputeGdpDelta(d, s);

        // Raw growth = 10 * 1.0 = 10. Diminished (squared): 10 * ((100-50)/100)^2 = 2.5
        Assert.AreEqual(2.5f, delta, 0.01f,
            "At GDP 50, positive delta should be quartered (squared DR)");
    }

    [Test]
    public void GdpDelta_DiminishingReturns_AtGdp90_ReducedToOnePercent()
    {
        SimulationConstants.K_EDU_TO_GDP = 1.0f;
        SimulationConstants.K_INFRA_TO_GDP = 0f;
        SimulationConstants.K_POP_TO_GDP = 0f;
        SimulationConstants.K_SUSTAIN_TO_GDP = 0f;
        SimulationConstants.K_TAX_GDP_DRAG = 0f;
        SimulationConstants.K_ENV_GDP_DRAG = 0f;
        SimulationConstants.K_GDP_DECAY = 0f;

        var d = MakeDistrict(gdp: 90f, infrastructure: 50f, sustainability: 50f);
        var s = MakeSpending(edu: 10f);

        float delta = LocalEffectCalculator.ComputeGdpDelta(d, s);

        // Raw growth = 10. Diminished: 10 * ((100-90)/100)^2 = 10 * 0.01 = 0.1
        Assert.AreEqual(0.1f, delta, 0.01f,
            "At GDP 90, positive delta should be 1% of raw (squared DR)");
    }

    [Test]
    public void GdpDelta_DiminishingReturns_AtGdp0_FullEffect()
    {
        SimulationConstants.K_EDU_TO_GDP = 1.0f;
        SimulationConstants.K_INFRA_TO_GDP = 0f;
        SimulationConstants.K_POP_TO_GDP = 0f;
        SimulationConstants.K_SUSTAIN_TO_GDP = 0f;
        SimulationConstants.K_TAX_GDP_DRAG = 0f;
        SimulationConstants.K_ENV_GDP_DRAG = 0f;
        SimulationConstants.K_GDP_DECAY = 0f;

        var d = MakeDistrict(gdp: 0f, infrastructure: 50f, sustainability: 50f);
        var s = MakeSpending(edu: 10f);

        float delta = LocalEffectCalculator.ComputeGdpDelta(d, s);

        // Raw growth = 10. Diminished: 10 * (1 - 0/100) = 10
        Assert.AreEqual(10.0f, delta, 0.01f,
            "At GDP 0, positive delta should get full effect");
    }

    [Test]
    public void GdpDelta_NegativeDelta_NotDiminished()
    {
        // Only decay active — produces negative delta
        SimulationConstants.K_EDU_TO_GDP = 0f;
        SimulationConstants.K_INFRA_TO_GDP = 0f;
        SimulationConstants.K_POP_TO_GDP = 0f;
        SimulationConstants.K_SUSTAIN_TO_GDP = 0f;
        SimulationConstants.K_TAX_GDP_DRAG = 0f;
        SimulationConstants.K_ENV_GDP_DRAG = 0f;
        SimulationConstants.K_GDP_DECAY = 0.1f;

        var d = MakeDistrict(gdp: 50f, infrastructure: 50f, sustainability: 50f);
        var s = MakeSpending();

        float delta = LocalEffectCalculator.ComputeGdpDelta(d, s);

        // Decay = 50 * 0.1 = -5. Negative → no diminishing returns applied
        Assert.AreEqual(-5.0f, delta, 0.01f,
            "Negative GDP delta should not be diminished");
    }

    [Test]
    public void GdpDelta_PopulationGuard_AtMinPopulation()
    {
        SimulationConstants.K_EDU_TO_GDP = 0f;
        SimulationConstants.K_INFRA_TO_GDP = 0f;
        SimulationConstants.K_POP_TO_GDP = 1.0f;
        SimulationConstants.K_SUSTAIN_TO_GDP = 0f;
        SimulationConstants.K_TAX_GDP_DRAG = 0f;
        SimulationConstants.K_ENV_GDP_DRAG = 0f;
        SimulationConstants.K_GDP_DECAY = 0f;

        // Population at minimum (1.0 = 1k residents)
        var d = MakeDistrict(gdp: 0f, population: 1.0f, infrastructure: 50f, sustainability: 50f);
        var s = MakeSpending();

        float delta = LocalEffectCalculator.ComputeGdpDelta(d, s);

        // log(max(1.0, 1.0)) * 1.0 = log(1.0) = 0. No NaN or -Infinity.
        Assert.AreEqual(0f, delta, 0.01f,
            "log(1.0) should be 0 — safe at minimum population");
        Assert.IsFalse(float.IsNaN(delta));
        Assert.IsFalse(float.IsInfinity(delta));
    }

    // ══════════════════════════════════════════════
    // CARRYING-CAPACITY OUTMIGRATION
    // ══════════════════════════════════════════════

    private static CityMetrics MakeCity(float reputation = 0f, float sharedInfra = 0f)
    {
        return new CityMetrics
        {
            cityReputation = reputation,
            sharedInfraQuality = sharedInfra,
            metroPopulationPool = 0f
        };
    }

    [Test]
    public void Capacity_ZeroInvestment_FloorAtKBase()
    {
        var d = MakeDistrict(infrastructure: 0f, sustainability: 55f, population: 5f);
        var s = MakeSpending();
        var cm = MakeCity();

        float K = LocalEffectCalculator.ComputeCarryingCapacity(d, s, cm);

        // Only K_BASE contributes when everything is zero.
        Assert.AreEqual(SimulationConstants.K_BASE, K, 0.01f,
            "K should collapse to K_BASE when all inputs are zero");
    }

    [Test]
    public void Capacity_DefaultDistrict_InMidBand()
    {
        // Defaults: housing=50, env=50, infra-metric=50, shared=50, rep=50, pop=150.
        // With K_SPEND=3.0: actualHousingCost = 0.5 * 150 * 3 = 225, sqrt = 15.
        // K = 5 + 4*15 + 1.5*50 + 1.5*15 + 1.5*50 + 1.0*50
        //   = 5 + 60 + 75 + 22.5 + 75 + 50 = 287.5
        var d = MakeDistrict(infrastructure: 50f, sustainability: 55f, population: 150f);
        var s = MakeSpending(housing: 225f, env: 225f);
        var cm = MakeCity(reputation: 50f, sharedInfra: 50f);

        float K = LocalEffectCalculator.ComputeCarryingCapacity(d, s, cm);

        Assert.AreEqual(287.5f, K, 1.0f,
            "Default mid-game district K should land near 287");
        // pop=150 well below K=287.5 → no outmigration.
        float loss = LocalEffectCalculator.ComputeOutmigration(d, s, cm);
        Assert.AreEqual(0f, loss, 0.001f, "No outmigration when below capacity");
    }

    [Test]
    public void Capacity_MaxInvestment_HitsTargetBand()
    {
        // Max sliders, max city signals, pop=500.
        // actualHousingCost = 1.0 * 500 * 3 = 1500, sqrt ≈ 38.73, *4 = 154.9
        // K = 5 + 154.9 + 150 + 58.1 + 150 + 100 = ~618
        var d = MakeDistrict(infrastructure: 100f, sustainability: 70f, population: 500f);
        var s = MakeSpending(housing: 1500f, env: 1500f);
        var cm = MakeCity(reputation: 100f, sharedInfra: 100f);

        float K = LocalEffectCalculator.ComputeCarryingCapacity(d, s, cm);

        Assert.Greater(K, 600f,
            "Max-invested district should support 600k+ residents");
        Assert.Less(K, 700f,
            "Max-invested district K should land in 600-700 band, not unbounded");
        float loss = LocalEffectCalculator.ComputeOutmigration(d, s, cm);
        Assert.AreEqual(0f, loss, 0.001f, "pop=500 well below K — no outmigration");
    }

    [Test]
    public void Outmigration_OvershootRampsQuadratically()
    {
        // Pin K to a known value: K_BASE only (5) and pop driven to overshoot.
        SimulationConstants.K_HOUSING_CAP = 0f;
        SimulationConstants.K_INFRA_CAP = 0f;
        SimulationConstants.K_ENV_CAP = 0f;
        SimulationConstants.K_SHARED_CAP = 0f;
        SimulationConstants.K_REPUTATION_CAP = 0f;
        // K = K_BASE = 5. Sustain=55 (above collapse threshold).
        var d = MakeDistrict(sustainability: 55f);
        var s = MakeSpending();
        var cm = MakeCity();

        d.population = 5f + 10f;
        float loss10 = LocalEffectCalculator.ComputeOutmigration(d, s, cm);
        d.population = 5f + 50f;
        float loss50 = LocalEffectCalculator.ComputeOutmigration(d, s, cm);
        d.population = 5f + 150f;
        float loss150 = LocalEffectCalculator.ComputeOutmigration(d, s, cm);

        // Linear: 0.05 * o; Quad: 0.0015 * o^2.
        // o=10: 0.5 + 0.15 = 0.65
        // o=50: 2.5 + 3.75 = 6.25
        // o=150: 7.5 + 33.75 = 41.25
        Assert.AreEqual(0.65f, loss10, 0.01f);
        Assert.AreEqual(6.25f, loss50, 0.01f);
        Assert.AreEqual(41.25f, loss150, 0.01f);
        // Super-linear ramp: ratio at higher overshoot grows faster than linear.
        Assert.Greater(loss150 / loss50, loss50 / loss10,
            "Outmigration should ramp super-linearly with overshoot");
    }

    [Test]
    public void Outmigration_DebtThrottled_CapacityCollapses()
    {
        // scaleFactor=0 means BudgetCalculator delivers actualHousingCost = 0,
        // so the housing/env capacity terms drop out entirely.
        var d = MakeDistrict(infrastructure: 100f, sustainability: 55f, population: 200f);
        var s = MakeSpending(housing: 0f, env: 0f);
        s.scaleFactor = 0f;
        var cm = MakeCity(reputation: 100f, sharedInfra: 100f);

        float K = LocalEffectCalculator.ComputeCarryingCapacity(d, s, cm);
        // K = 5 + 0 + 150 + 0 + 150 + 100 = 405. Pop=200 still below.
        // The point is: housing/env contributions go to zero with scaleFactor=0.
        Assert.AreEqual(405f, K, 1.0f,
            "Debt-throttled district loses its housing/env capacity bonus");
    }

    [Test]
    public void Outmigration_SustainCollapseFloorTriggers()
    {
        // High capacity, but sustain=0 → collapse floor still evicts.
        var d = MakeDistrict(infrastructure: 100f, sustainability: 0f, population: 100f);
        var s = MakeSpending(housing: 1000f, env: 1000f);
        var cm = MakeCity(reputation: 100f, sharedInfra: 100f);

        float loss = LocalEffectCalculator.ComputeOutmigration(d, s, cm);

        // Capacity term is zero (pop well below K). Sustain term:
        // (15 - 0) * 0.5 = 7.5
        Assert.AreEqual(7.5f, loss, 0.01f,
            "Sustain collapse below 15 should evict regardless of capacity");
    }

    [Test]
    public void Outmigration_SustainAt15_NoCollapseTerm()
    {
        // Sustain at the boundary — no collapse term, pop below capacity.
        var d = MakeDistrict(infrastructure: 100f, sustainability: 15f, population: 100f);
        var s = MakeSpending(housing: 1000f, env: 1000f);
        var cm = MakeCity(reputation: 100f, sharedInfra: 100f);

        float loss = LocalEffectCalculator.ComputeOutmigration(d, s, cm);

        Assert.AreEqual(0f, loss, 0.001f,
            "At sustain=15 (boundary), only capacity term active and pop is below K");
    }

    // ══════════════════════════════════════════════
    // HAPPINESS RANGE AT EXTREMES
    // ══════════════════════════════════════════════

    [Test]
    public void Happiness_BestCase_DoesNotExceed100()
    {
        // Max all metrics, max housing spending, min tax, zero debt
        SimulationConstants.K_HOUSING_TO_HAPPY = 0.05f; // reasonable calibration
        SimulationConstants.K_TAX_HAPPY_PENALTY = 10f;
        SimulationConstants.K_DEBT_STRESS = 1.0f;

        var d = MakeDistrict(
            gdp: 100f, happiness: 100f, infrastructure: 100f,
            sustainability: 100f, debt: 0f, taxRate: 5f);
        var s = MakeSpending(housing: 500f); // large housing spend

        float happiness = LocalEffectCalculator.ComputeHappiness(d, s);

        Assert.LessOrEqual(happiness, 100f, "Happiness must not exceed 100");
        Assert.GreaterOrEqual(happiness, 0f, "Happiness must not go below 0");
    }

    [Test]
    public void Happiness_WorstCase_DoesNotGoBelowZero()
    {
        SimulationConstants.K_HOUSING_TO_HAPPY = 0.05f;
        SimulationConstants.K_TAX_HAPPY_PENALTY = 10f;
        SimulationConstants.K_DEBT_STRESS = 1.0f;

        // All metrics 0, zero housing, max tax, max debt
        var d = MakeDistrict(
            gdp: 0f, happiness: 0f, infrastructure: 0f,
            sustainability: 0f, debt: 80f, taxRate: 30f);
        var s = MakeSpending(housing: 0f);

        float happiness = LocalEffectCalculator.ComputeHappiness(d, s);

        Assert.GreaterOrEqual(happiness, 0f, "Happiness must not go below 0");
        Assert.LessOrEqual(happiness, 100f, "Happiness must not exceed 100");
    }

    [Test]
    public void Happiness_Smoothing_BlendsHalfway()
    {
        SimulationConstants.K_HAPPY_SMOOTHING = 0.5f;
        SimulationConstants.K_HOUSING_TO_HAPPY = 0f;
        SimulationConstants.K_TAX_HAPPY_PENALTY = 0f;
        SimulationConstants.K_DEBT_STRESS = 0f;

        // Metric baseline: all at 50, debt 0 → inverseDebt = 100
        // baseline = 50*0.30 + 50*0.25 + 50*0.25 + 100*0.20 = 15+12.5+12.5+20 = 60
        // target = 60 * 0.60 = 36
        // Current happiness = 80
        // smoothed = 80 + (36 - 80) * 0.5 = 80 - 22 = 58
        var d = MakeDistrict(
            gdp: 50f, happiness: 80f, infrastructure: 50f,
            sustainability: 50f, debt: 0f, taxRate: 15f);
        var s = MakeSpending();

        float happiness = LocalEffectCalculator.ComputeHappiness(d, s);

        Assert.AreEqual(58f, happiness, 0.1f,
            "With K_HAPPY_SMOOTHING=0.5, should blend halfway between old and target");
    }

    [Test]
    public void Happiness_DebtStress_ZeroWhenDebtAtOrBelow40()
    {
        SimulationConstants.K_HOUSING_TO_HAPPY = 0f;
        SimulationConstants.K_TAX_HAPPY_PENALTY = 0f;
        SimulationConstants.K_DEBT_STRESS = 5.0f; // large value to make stress obvious

        // Compare debt 41 (stress active) vs debt 40 (stress zero).
        // Both have nearly identical inverseDebt so baseline is close,
        // but the stress penalty should create a measurable gap.
        var d41 = MakeDistrict(gdp: 50f, happiness: 50f, infrastructure: 50f,
            sustainability: 50f, debt: 41f, taxRate: 15f);
        var d40 = MakeDistrict(gdp: 50f, happiness: 50f, infrastructure: 50f,
            sustainability: 50f, debt: 40f, taxRate: 15f);
        var s = MakeSpending();

        float h41 = LocalEffectCalculator.ComputeHappiness(d41, s);
        float h40 = LocalEffectCalculator.ComputeHappiness(d40, s);

        // debt stress at 41 = max(0, 41-40)*5.0 = 5.0
        // debt stress at 40 = max(0, 40-40)*5.0 = 0.0
        // The inverseDebt difference is only (1*100/80)*0.20*0.60 ≈ 0.15
        // So h40 should be notably higher than h41 due to the 5-point stress penalty
        Assert.Greater(h40, h41,
            "Debt stress should be zero at debt 40 but active at debt 41");
        Assert.Greater(h40 - h41, 4f,
            "Stress penalty at debt 41 should cause significant happiness drop");
    }

    // ══════════════════════════════════════════════
    // INFRASTRUCTURE
    // ══════════════════════════════════════════════

    [Test]
    public void InfrastructureDelta_DiminishingReturns_HighInfra()
    {
        SimulationConstants.K_INFRA_TO_INFRA = 1.0f;
        SimulationConstants.K_INFRA_DECAY = 0f;

        var dLow = MakeDistrict(infrastructure: 20f);
        var dHigh = MakeDistrict(infrastructure: 80f);
        var s = MakeSpending(infra: 100f);

        float deltaLow = LocalEffectCalculator.ComputeInfrastructureDelta(dLow, s);
        float deltaHigh = LocalEffectCalculator.ComputeInfrastructureDelta(dHigh, s);

        // At infra 20: growth = 100 * 1.0 * (1 - 0.2) = 80
        // At infra 80: growth = 100 * 1.0 * (1 - 0.8) = 20
        Assert.AreEqual(80f, deltaLow, 0.01f);
        Assert.AreEqual(20f, deltaHigh, 0.01f);
        Assert.Greater(deltaLow, deltaHigh,
            "Same spending should produce less growth at high infrastructure");
    }

    // ══════════════════════════════════════════════
    // SUSTAINABILITY
    // ══════════════════════════════════════════════

    [Test]
    public void SustainabilityDelta_InfraAbove50_PositiveContribution()
    {
        SimulationConstants.K_INFRA_TO_SUSTAIN = 1.0f;
        SimulationConstants.K_ENV_TO_SUSTAIN = 0f;
        SimulationConstants.K_POP_SUSTAIN_DRAIN = 0f;
        SimulationConstants.K_SUSTAIN_DECAY = 0f;

        var d = MakeDistrict(infrastructure: 70f, sustainability: 50f);
        var s = MakeSpending();

        float delta = LocalEffectCalculator.ComputeSustainabilityDelta(d, s);

        // (70 - 50) * 1.0 = 20
        Assert.AreEqual(20f, delta, 0.01f);
    }

    [Test]
    public void SustainabilityDelta_InfraBelow50_NegativeContribution()
    {
        SimulationConstants.K_INFRA_TO_SUSTAIN = 1.0f;
        SimulationConstants.K_ENV_TO_SUSTAIN = 0f;
        SimulationConstants.K_POP_SUSTAIN_DRAIN = 0f;
        SimulationConstants.K_SUSTAIN_DECAY = 0f;

        var d = MakeDistrict(infrastructure: 30f, sustainability: 50f);
        var s = MakeSpending();

        float delta = LocalEffectCalculator.ComputeSustainabilityDelta(d, s);

        // (30 - 50) * 1.0 = -20
        Assert.AreEqual(-20f, delta, 0.01f);
    }

    // ══════════════════════════════════════════════
    // POPULATION FLOOR REGRESSION
    // The bug being fixed: with production constants, population always
    // collapsed to MIN_POPULATION=5 regardless of player investment.
    // ══════════════════════════════════════════════

    [Test]
    public void Population_EscapesMinFloor_DefaultDistrict()
    {
        const int numPlayers = 4;
        var districts = new DistrictState[numPlayers];
        for (int i = 0; i < numPlayers; i++) districts[i] = DistrictState.Default();
        var cityMetrics = CityMetrics.Default();

        for (int tick = 0; tick < SimulationConstants.TOTAL_TICKS; tick++)
            TickProcessor.ResolveFullTick(districts, ref cityMetrics);

        for (int i = 0; i < numPlayers; i++)
        {
            Assert.Greater(districts[i].population, SimulationConstants.MIN_POPULATION + 20f,
                $"District {i} should not collapse to floor (got {districts[i].population:F1})");
        }
    }
}

#endif