// Author: Malcolm Bramble

using System;
using NUnit.Framework;

[TestFixture]
public class IntegrationTests
{
    // Save ALL mutable constants so we can set calibrated values for integration tests
    // without affecting other test fixtures.

    private float[] savedConstants;
    private string[] constantNames;

    [SetUp]
    public void SaveAndSetConstants()
    {
        // Save current values
        savedConstants = new float[]
        {
            SimulationConstants.K_REV,
            SimulationConstants.K_SPEND,
            SimulationConstants.K_CITY_WEIGHT,
            SimulationConstants.K_DEBT_ACCRUAL,
            SimulationConstants.K_DEBT_RECOVERY,
            SimulationConstants.K_RESERVE_DECAY,
            SimulationConstants.DEBT_CAP,
            SimulationConstants.RESERVE_CAP,
            SimulationConstants.K_EDU_TO_GDP,
            SimulationConstants.K_INFRA_TO_GDP,
            SimulationConstants.K_POP_TO_GDP,
            SimulationConstants.K_SUSTAIN_TO_GDP,
            SimulationConstants.K_TAX_GDP_DRAG,
            SimulationConstants.K_ENV_GDP_DRAG,
            SimulationConstants.K_GDP_DECAY,
            SimulationConstants.W_HAPPY_GDP,
            SimulationConstants.W_HAPPY_INFRA,
            SimulationConstants.W_HAPPY_SUSTAIN,
            SimulationConstants.W_HAPPY_DEBT,
            SimulationConstants.K_BASELINE_WEIGHT,
            SimulationConstants.K_HOUSING_TO_HAPPY,
            SimulationConstants.K_TAX_HAPPY_PENALTY,
            SimulationConstants.K_DEBT_STRESS,
            SimulationConstants.K_HAPPY_SMOOTHING,
            SimulationConstants.K_INFRA_TO_INFRA,
            SimulationConstants.K_INFRA_DECAY,
            SimulationConstants.K_INFRA_TO_SUSTAIN,
            SimulationConstants.K_ENV_TO_SUSTAIN,
            SimulationConstants.K_POP_SUSTAIN_DRAIN,
            SimulationConstants.K_SUSTAIN_DECAY,
            SimulationConstants.SUSTAIN_MIGRATION_THRESHOLD,
            SimulationConstants.K_MIGRATION_RATE,
            SimulationConstants.MIN_POPULATION,
            SimulationConstants.MAX_POPULATION,
            SimulationConstants.GENTRIFY_THRESHOLD,
            SimulationConstants.K_GENTRIFY_HAPPY,
            SimulationConstants.K_GENTRIFY_POP,
            SimulationConstants.K_GENTRIFY_GDP_GAIN,
            SimulationConstants.K_GENTRIFY_WEALTHY_HAPPY,
            SimulationConstants.POLLUTE_ENV_THRESHOLD,
            SimulationConstants.POLLUTE_GDP_THRESHOLD,
            SimulationConstants.K_POLLUTION_GENERATE,
            SimulationConstants.K_POLLUTION_SUSTAIN,
            SimulationConstants.K_POLLUTION_HAPPY,
            SimulationConstants.K_POLLUTION_SELF_SUSTAIN,
            SimulationConstants.K_POLLUTION_SELF_HAPPY,
            SimulationConstants.COMMUTE_GDP_THRESHOLD,
            SimulationConstants.COMMUTE_INFRA_THRESHOLD,
            SimulationConstants.K_COMMUTE_VOLUME,
            SimulationConstants.K_COMMUTE_GDP_GAIN,
            SimulationConstants.K_COMMUTE_CONGESTION,
            SimulationConstants.K_COMMUTE_GDP_DRAIN,
            SimulationConstants.K_COMMUTE_HOME_HAPPY,
            SimulationConstants.K_VARIANCE_PENALTY,
            SimulationConstants.K_POP_INFLOW_HIGH,
            SimulationConstants.K_POP_INFLOW_NORMAL,
            SimulationConstants.K_POP_OUTFLOW,
            SimulationConstants.K_SHARED_INFRA_GROWTH,
            SimulationConstants.K_SHARED_INFRA_DECAY,
            SimulationConstants.GRANT_BASE_GREEN,
            SimulationConstants.GRANT_BASE_TRANSIT,
            SimulationConstants.GRANT_BASE_LIFE,
            SimulationConstants.GRANT_BASE_DEV,
            SimulationConstants.K_STABILIZATION_RATE,
            SimulationConstants.POP_MAX_SCORE,
            SimulationConstants.K_CRISIS_PENALTY,
        };

        // ── Set calibrated constants for steady-state test ──
        // These are chosen so that at default starting values:
        // - Budget is balanced (revenue = spending, already true by design)
        // - GDP growth inputs ≈ GDP decay (near-zero net delta)
        // - Infrastructure growth ≈ infrastructure decay
        // - Sustainability growth ≈ sustainability drains
        // - No spillover fires (all districts identical, no differentials)

        // Budget (already balanced at defaults)
        SimulationConstants.K_REV = 1.0f;
        SimulationConstants.K_SPEND = 3.0f;
        SimulationConstants.K_CITY_WEIGHT = 1.0f;
        SimulationConstants.K_DEBT_ACCRUAL = 0.005f;
        SimulationConstants.K_DEBT_RECOVERY = SimulationConstants.K_DEBT_ACCRUAL / 3.0f;
        SimulationConstants.K_RESERVE_DECAY = 0.005f;
        SimulationConstants.DEBT_CAP = 60f;
        SimulationConstants.RESERVE_CAP = 22500f;

        // GDP equilibrium at defaults:
        // edu=225*0.0005=0.1125, pop=log(150)*0.03≈0.15, sustain=(55-50)*0.001=0.005
        // tax=0.15*50*0.005=0.0375, env=225*0.0003=0.0675, decay=50*0.003=0.15
        // Net ≈ +0.01 before diminishing returns
        SimulationConstants.K_EDU_TO_GDP = 0.0012f;
        SimulationConstants.K_INFRA_TO_GDP = 0.001f;
        SimulationConstants.K_POP_TO_GDP = 0.03f;
        SimulationConstants.K_SUSTAIN_TO_GDP = 0.001f;
        SimulationConstants.K_TAX_GDP_DRAG = 0.005f;
        SimulationConstants.K_ENV_GDP_DRAG = 0.0003f;
        SimulationConstants.K_GDP_DECAY = 0.006f;

        // Happiness
        SimulationConstants.W_HAPPY_GDP = 0.30f;
        SimulationConstants.W_HAPPY_INFRA = 0.25f;
        SimulationConstants.W_HAPPY_SUSTAIN = 0.25f;
        SimulationConstants.W_HAPPY_DEBT = 0.20f;
        SimulationConstants.K_BASELINE_WEIGHT = 0.60f;
        SimulationConstants.K_HOUSING_TO_HAPPY = 0.08f;
        SimulationConstants.K_TAX_HAPPY_PENALTY = 5.0f;
        SimulationConstants.K_DEBT_STRESS = 0.3f;
        SimulationConstants.K_HAPPY_SMOOTHING = 1.0f;

        // Infrastructure: at defaults, actualInfraCost = 225
        // growth = 225 * K * (1 - 50/100) = 225 * K * 0.5
        // decay = 50 * K_decay
        // For equilibrium: 112.5 * K_growth ≈ 50 * K_decay
        SimulationConstants.K_INFRA_TO_INFRA = 0.002f;
        SimulationConstants.K_INFRA_DECAY = 0.0045f;

        // Sustainability equilibrium at defaults:
        // infra=(50-50)*K=0, env=225*0.001=0.225
        // popDrain=150*0.001=0.15, decay=55*0.0014≈0.077
        // Net ≈ 0.225 - 0.15 - 0.077 ≈ 0.0
        SimulationConstants.K_INFRA_TO_SUSTAIN = 0.002f;
        SimulationConstants.K_ENV_TO_SUSTAIN = 0.001f;
        SimulationConstants.K_POP_SUSTAIN_DRAIN = 0.001f;
        SimulationConstants.K_SUSTAIN_DECAY = 0.0014f;
        SimulationConstants.SUSTAIN_MIGRATION_THRESHOLD = 30f;
        SimulationConstants.K_MIGRATION_RATE = 0.5f;
        SimulationConstants.MIN_POPULATION = 1.0f;
        SimulationConstants.MAX_POPULATION = 1000.0f;

        // Spillover — thresholds remain at spec values;
        // with identical districts no spillover fires
        SimulationConstants.GENTRIFY_THRESHOLD = 8f;
        SimulationConstants.K_GENTRIFY_HAPPY = 0.5f;
        SimulationConstants.K_GENTRIFY_POP = 0.1f;
        SimulationConstants.K_GENTRIFY_GDP_GAIN = 0.3f;
        SimulationConstants.K_GENTRIFY_WEALTHY_HAPPY = 0.2f;
        SimulationConstants.POLLUTE_ENV_THRESHOLD = 30f;
        SimulationConstants.POLLUTE_GDP_THRESHOLD = 40f;
        SimulationConstants.K_POLLUTION_GENERATE = 0.1f;
        SimulationConstants.K_POLLUTION_SUSTAIN = 0.1f;
        SimulationConstants.K_POLLUTION_HAPPY = 0.05f;
        SimulationConstants.K_POLLUTION_SELF_SUSTAIN = 0.05f;
        SimulationConstants.K_POLLUTION_SELF_HAPPY = 0.025f;
        SimulationConstants.COMMUTE_GDP_THRESHOLD = 5f;
        SimulationConstants.COMMUTE_INFRA_THRESHOLD = 25f;
        SimulationConstants.K_COMMUTE_VOLUME = 0.5f;
        SimulationConstants.K_COMMUTE_GDP_GAIN = 0.3f;
        SimulationConstants.K_COMMUTE_CONGESTION = 0.1f;
        SimulationConstants.K_COMMUTE_GDP_DRAIN = 0.2f;
        SimulationConstants.K_COMMUTE_HOME_HAPPY = 0.1f;

        // City Metrics — reduce pop inflow so population doesn't compound
        SimulationConstants.K_VARIANCE_PENALTY = 1.0f;
        SimulationConstants.K_POP_INFLOW_HIGH = 0.2f;
        SimulationConstants.K_POP_INFLOW_NORMAL = 0.02f;
        SimulationConstants.K_POP_OUTFLOW = 0.5f;
        SimulationConstants.K_SHARED_INFRA_GROWTH = 0.0002f;
        SimulationConstants.K_SHARED_INFRA_DECAY = 0.005f;

        // Federal Funding
        SimulationConstants.GRANT_BASE_GREEN = 50f;
        SimulationConstants.GRANT_BASE_TRANSIT = 50f;
        SimulationConstants.GRANT_BASE_LIFE = 50f;
        SimulationConstants.GRANT_BASE_DEV = 50f;
        SimulationConstants.K_STABILIZATION_RATE = 1.0f;

        // Scoring
        SimulationConstants.POP_MAX_SCORE = 300f;
        SimulationConstants.K_CRISIS_PENALTY = 0.5f;
    }

    [TearDown]
    public void RestoreConstants()
    {
        SimulationConstants.K_REV = savedConstants[0];
        SimulationConstants.K_SPEND = savedConstants[1];
        SimulationConstants.K_CITY_WEIGHT = savedConstants[2];
        SimulationConstants.K_DEBT_ACCRUAL = savedConstants[3];
        SimulationConstants.K_DEBT_RECOVERY = savedConstants[4];
        SimulationConstants.K_RESERVE_DECAY = savedConstants[5];
        SimulationConstants.DEBT_CAP = savedConstants[6];
        SimulationConstants.RESERVE_CAP = savedConstants[7];
        SimulationConstants.K_EDU_TO_GDP = savedConstants[8];
        SimulationConstants.K_INFRA_TO_GDP = savedConstants[9];
        SimulationConstants.K_POP_TO_GDP = savedConstants[10];
        SimulationConstants.K_SUSTAIN_TO_GDP = savedConstants[11];
        SimulationConstants.K_TAX_GDP_DRAG = savedConstants[12];
        SimulationConstants.K_ENV_GDP_DRAG = savedConstants[13];
        SimulationConstants.K_GDP_DECAY = savedConstants[14];
        SimulationConstants.W_HAPPY_GDP = savedConstants[15];
        SimulationConstants.W_HAPPY_INFRA = savedConstants[16];
        SimulationConstants.W_HAPPY_SUSTAIN = savedConstants[17];
        SimulationConstants.W_HAPPY_DEBT = savedConstants[18];
        SimulationConstants.K_BASELINE_WEIGHT = savedConstants[19];
        SimulationConstants.K_HOUSING_TO_HAPPY = savedConstants[20];
        SimulationConstants.K_TAX_HAPPY_PENALTY = savedConstants[21];
        SimulationConstants.K_DEBT_STRESS = savedConstants[22];
        SimulationConstants.K_HAPPY_SMOOTHING = savedConstants[23];
        SimulationConstants.K_INFRA_TO_INFRA = savedConstants[24];
        SimulationConstants.K_INFRA_DECAY = savedConstants[25];
        SimulationConstants.K_INFRA_TO_SUSTAIN = savedConstants[26];
        SimulationConstants.K_ENV_TO_SUSTAIN = savedConstants[27];
        SimulationConstants.K_POP_SUSTAIN_DRAIN = savedConstants[28];
        SimulationConstants.K_SUSTAIN_DECAY = savedConstants[29];
        SimulationConstants.SUSTAIN_MIGRATION_THRESHOLD = savedConstants[30];
        SimulationConstants.K_MIGRATION_RATE = savedConstants[31];
        SimulationConstants.MIN_POPULATION = savedConstants[32];
        SimulationConstants.MAX_POPULATION = savedConstants[33];
        SimulationConstants.GENTRIFY_THRESHOLD = savedConstants[34];
        SimulationConstants.K_GENTRIFY_HAPPY = savedConstants[35];
        SimulationConstants.K_GENTRIFY_POP = savedConstants[36];
        SimulationConstants.K_GENTRIFY_GDP_GAIN = savedConstants[37];
        SimulationConstants.K_GENTRIFY_WEALTHY_HAPPY = savedConstants[38];
        SimulationConstants.POLLUTE_ENV_THRESHOLD = savedConstants[39];
        SimulationConstants.POLLUTE_GDP_THRESHOLD = savedConstants[40];
        SimulationConstants.K_POLLUTION_GENERATE = savedConstants[41];
        SimulationConstants.K_POLLUTION_SUSTAIN = savedConstants[42];
        SimulationConstants.K_POLLUTION_HAPPY = savedConstants[43];
        SimulationConstants.K_POLLUTION_SELF_SUSTAIN = savedConstants[44];
        SimulationConstants.K_POLLUTION_SELF_HAPPY = savedConstants[45];
        SimulationConstants.COMMUTE_GDP_THRESHOLD = savedConstants[46];
        SimulationConstants.COMMUTE_INFRA_THRESHOLD = savedConstants[47];
        SimulationConstants.K_COMMUTE_VOLUME = savedConstants[48];
        SimulationConstants.K_COMMUTE_GDP_GAIN = savedConstants[49];
        SimulationConstants.K_COMMUTE_CONGESTION = savedConstants[50];
        SimulationConstants.K_COMMUTE_GDP_DRAIN = savedConstants[51];
        SimulationConstants.K_COMMUTE_HOME_HAPPY = savedConstants[52];
        SimulationConstants.K_VARIANCE_PENALTY = savedConstants[53];
        SimulationConstants.K_POP_INFLOW_HIGH = savedConstants[54];
        SimulationConstants.K_POP_INFLOW_NORMAL = savedConstants[55];
        SimulationConstants.K_POP_OUTFLOW = savedConstants[56];
        SimulationConstants.K_SHARED_INFRA_GROWTH = savedConstants[57];
        SimulationConstants.K_SHARED_INFRA_DECAY = savedConstants[58];
        SimulationConstants.GRANT_BASE_GREEN = savedConstants[59];
        SimulationConstants.GRANT_BASE_TRANSIT = savedConstants[60];
        SimulationConstants.GRANT_BASE_LIFE = savedConstants[61];
        SimulationConstants.GRANT_BASE_DEV = savedConstants[62];
        SimulationConstants.K_STABILIZATION_RATE = savedConstants[63];
        SimulationConstants.POP_MAX_SCORE = savedConstants[64];
        SimulationConstants.K_CRISIS_PENALTY = savedConstants[65];
    }

    // ══════════════════════════════════════════════
    // STEADY STATE — 576 ticks at default sliders
    // ══════════════════════════════════════════════

    [Test]
    public void SteadyState_576Ticks_MetricsStayNearStartingValues()
    {
        GameState state = GameState.NewGame(4);

        for (int tick = 0; tick < SimulationConstants.TOTAL_TICKS; tick++)
        {
            state = TickProcessor.ResolveTick(state);
        }

        Assert.AreEqual(576, state.currentTick, "Should have completed all 576 ticks");
        Assert.AreEqual(48, state.currentMonth, "Should be month 48");

        // All districts started identical and no one changed sliders.
        // With calibrated constants, metrics should remain in a reasonable band
        // around starting values. We allow generous tolerance (±30) since the
        // TBD constants aren't perfectly calibrated yet — the point is no metric
        // crashes to 0 or spikes to 100.
        for (int i = 0; i < 4; i++)
        {
            var d = state.districts[i];

            Assert.Greater(d.gdp, 5f,
                $"District {i} GDP should not collapse (got {d.gdp:F1})");
            Assert.Less(d.gdp, 95f,
                $"District {i} GDP should not spike to ceiling (got {d.gdp:F1})");

            Assert.Greater(d.happiness, 5f,
                $"District {i} happiness should not collapse (got {d.happiness:F1})");
            Assert.Less(d.happiness, 95f,
                $"District {i} happiness should not spike to ceiling (got {d.happiness:F1})");

            Assert.Greater(d.population, 50f,
                $"District {i} population should not collapse (got {d.population:F1})");
            Assert.Less(d.population, 500f,
                $"District {i} population should not explode (got {d.population:F1})");

            Assert.Greater(d.infrastructure, 5f,
                $"District {i} infrastructure should not collapse (got {d.infrastructure:F1})");
            Assert.Less(d.infrastructure, 95f,
                $"District {i} infrastructure should not spike (got {d.infrastructure:F1})");

            Assert.Greater(d.sustainability, 5f,
                $"District {i} sustainability should not collapse (got {d.sustainability:F1})");
            Assert.Less(d.sustainability, 95f,
                $"District {i} sustainability should not spike (got {d.sustainability:F1})");

            Assert.Less(d.debt, 70f,
                $"District {i} debt should not reach crisis level (got {d.debt:F1})");
        }

        // City-level metrics should also be reasonable
        Assert.Greater(state.cityMetrics.cityReputation, 10f,
            "City reputation should not collapse");
        Assert.Greater(state.cityMetrics.sharedInfraQuality, 5f,
            "Shared infrastructure should not collapse completely");
    }

    [Test]
    public void SteadyState_AllDistrictsRemainIdentical()
    {
        // With identical starting conditions and no slider changes,
        // all districts should stay identical (deterministic, symmetric).
        GameState state = GameState.NewGame(4);

        for (int tick = 0; tick < 100; tick++)
        {
            state = TickProcessor.ResolveTick(state);
        }

        var d0 = state.districts[0];
        for (int i = 1; i < 4; i++)
        {
            var d = state.districts[i];
            Assert.AreEqual(d0.gdp, d.gdp, 0.01f,
                $"District {i} GDP should match district 0");
            Assert.AreEqual(d0.happiness, d.happiness, 0.01f,
                $"District {i} happiness should match district 0");
            Assert.AreEqual(d0.population, d.population, 0.01f,
                $"District {i} population should match district 0");
            Assert.AreEqual(d0.infrastructure, d.infrastructure, 0.01f,
                $"District {i} infrastructure should match district 0");
            Assert.AreEqual(d0.sustainability, d.sustainability, 0.01f,
                $"District {i} sustainability should match district 0");
            Assert.AreEqual(d0.debt, d.debt, 0.01f,
                $"District {i} debt should match district 0");
        }
    }

    [Test]
    public void SteadyState_NoNaNOrInfinity()
    {
        GameState state = GameState.NewGame(4);

        for (int tick = 0; tick < SimulationConstants.TOTAL_TICKS; tick++)
        {
            state = TickProcessor.ResolveTick(state);

            for (int i = 0; i < 4; i++)
            {
                var d = state.districts[i];
                Assert.IsFalse(float.IsNaN(d.gdp), $"GDP NaN at tick {tick}");
                Assert.IsFalse(float.IsNaN(d.happiness), $"Happiness NaN at tick {tick}");
                Assert.IsFalse(float.IsNaN(d.population), $"Population NaN at tick {tick}");
                Assert.IsFalse(float.IsNaN(d.infrastructure), $"Infrastructure NaN at tick {tick}");
                Assert.IsFalse(float.IsNaN(d.sustainability), $"Sustainability NaN at tick {tick}");
                Assert.IsFalse(float.IsNaN(d.debt), $"Debt NaN at tick {tick}");
                Assert.IsFalse(float.IsInfinity(d.gdp), $"GDP Inf at tick {tick}");
                Assert.IsFalse(float.IsInfinity(d.population), $"Population Inf at tick {tick}");
            }

            Assert.IsFalse(float.IsNaN(state.cityMetrics.cityReputation),
                $"City reputation NaN at tick {tick}");
            Assert.IsFalse(float.IsNaN(state.cityMetrics.sharedInfraQuality),
                $"Shared infra NaN at tick {tick}");
        }
    }

    // ══════════════════════════════════════════════
    // SCORING
    // ══════════════════════════════════════════════

    [Test]
    public void Scoring_AfterFullGame_ProducesReasonableScores()
    {
        GameState state = GameState.NewGame(4);

        for (int tick = 0; tick < SimulationConstants.TOTAL_TICKS; tick++)
        {
            state = TickProcessor.ResolveTick(state);
        }

        for (int i = 0; i < 4; i++)
        {
            FinalScore score = ScoringSystem.ComputeFinalScore(
                state.districts[i], state.cityMetrics, state.districts, 4);

            Assert.GreaterOrEqual(score.finalScore, 0f,
                $"District {i} final score should not be negative");
            Assert.LessOrEqual(score.finalScore, 100f,
                $"District {i} final score should not exceed 100");
            Assert.Greater(score.finalScore, 5f,
                $"District {i} final score should be meaningful (got {score.finalScore:F1})");
        }
    }

    [Test]
    public void Scoring_IdenticalDistricts_EqualScores()
    {
        GameState state = GameState.NewGame(4);

        for (int tick = 0; tick < 100; tick++)
        {
            state = TickProcessor.ResolveTick(state);
        }

        FinalScore score0 = ScoringSystem.ComputeFinalScore(
            state.districts[0], state.cityMetrics, state.districts, 4);

        for (int i = 1; i < 4; i++)
        {
            FinalScore scoreI = ScoringSystem.ComputeFinalScore(
                state.districts[i], state.cityMetrics, state.districts, 4);

            Assert.AreEqual(score0.finalScore, scoreI.finalScore, 0.01f,
                $"District {i} score should match district 0 with identical play");
        }
    }

    // ══════════════════════════════════════════════
    // TICK COUNTER
    // ══════════════════════════════════════════════

    [Test]
    public void TickCounter_AdvancesCorrectly()
    {
        GameState state = GameState.NewGame(2);

        Assert.AreEqual(0, state.currentTick);
        Assert.AreEqual(0, state.currentMonth);

        state = TickProcessor.ResolveTick(state);
        Assert.AreEqual(1, state.currentTick);
        Assert.AreEqual(0, state.currentMonth);

        // Run to tick 12 (month 1)
        for (int i = 1; i < 12; i++)
            state = TickProcessor.ResolveTick(state);

        Assert.AreEqual(12, state.currentTick);
        Assert.AreEqual(1, state.currentMonth);
    }
}
