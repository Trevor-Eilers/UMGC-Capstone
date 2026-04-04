// Author: Malcolm Bramble

using System;
using NUnit.Framework;

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
    }

    // ──────────────────────────────────────────────
    // Helper: create a district with controlled values
    // ──────────────────────────────────────────────

    private static DistrictState MakeDistrict(
        float gdp = 50f, float happiness = 55f, float population = 150f,
        float infrastructure = 50f, float sustainability = 55f,
        float debt = 15f, float taxRate = 15f)
    {
        var d = DistrictState.Default(0);
        d.gdp = gdp;
        d.happiness = happiness;
        d.population = population;
        d.infrastructure = infrastructure;
        d.sustainability = sustainability;
        d.debt = debt;
        d.sliders.taxRate = taxRate;
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
    public void GdpDelta_DiminishingReturns_AtGdp50_PositiveDeltaHalved()
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

        // Raw growth = 10 * 1.0 = 10. Diminished: 10 * (1 - 50/100) = 5
        Assert.AreEqual(5.0f, delta, 0.01f,
            "At GDP 50, positive delta should be halved");
    }

    [Test]
    public void GdpDelta_DiminishingReturns_AtGdp90_ReducedTo10Percent()
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

        // Raw growth = 10. Diminished: 10 * (1 - 90/100) = 1.0
        Assert.AreEqual(1.0f, delta, 0.01f,
            "At GDP 90, positive delta should be 10% of raw");
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
    // SUSTAINABILITY OUTMIGRATION
    // ══════════════════════════════════════════════

    [Test]
    public void Outmigration_AtThreshold30_NoMigration()
    {
        var d = MakeDistrict(sustainability: 30f);
        float loss = LocalEffectCalculator.ComputeOutmigration(d);
        Assert.AreEqual(0f, loss, 0.001f,
            "No outmigration at exactly the threshold");
    }

    [Test]
    public void Outmigration_AboveThreshold_NoMigration()
    {
        var d = MakeDistrict(sustainability: 55f);
        float loss = LocalEffectCalculator.ComputeOutmigration(d);
        Assert.AreEqual(0f, loss, 0.001f);
    }

    [Test]
    public void Outmigration_BelowThreshold_ScalesLinearly()
    {
        SimulationConstants.K_MIGRATION_RATE = 2.0f;

        var d = MakeDistrict(sustainability: 20f);
        float loss = LocalEffectCalculator.ComputeOutmigration(d);

        // (30 - 20) * 2.0 = 20.0
        Assert.AreEqual(20.0f, loss, 0.01f,
            "Outmigration should be (threshold - sustainability) * K_MIGRATION_RATE");
    }

    [Test]
    public void Outmigration_AtZeroSustainability_MaximumLoss()
    {
        SimulationConstants.K_MIGRATION_RATE = 1.0f;

        var d = MakeDistrict(sustainability: 0f);
        float loss = LocalEffectCalculator.ComputeOutmigration(d);

        // (30 - 0) * 1.0 = 30
        Assert.AreEqual(30.0f, loss, 0.01f);
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
}
