// Author: Malcolm Bramble

#if UNITY_EDITOR

using System;
using NUnit.Framework;
using Simulation;

[TestFixture]
public class DominantStrategyTests
{
    // These tests use PRODUCTION simulation constants — no overrides — to verify
    // that the live-tuned values produce the intended end-game behaviour.
    // Save/restore is here purely as a safety net so a test that mutates a
    // constant for a specific case doesn't leak into the others.

    private float[] saved;

    [SetUp]
    public void SaveConstants()
    {
        saved = new float[]
        {
            SimulationConstants.K_TAX_GDP_DRAG,
            SimulationConstants.K_HOUSING_TO_HAPPY,
            SimulationConstants.K_DEBT_ACCRUAL,
            SimulationConstants.K_CRISIS_PENALTY,
            SimulationConstants.GDP_MAINTAIN_THRESHOLD,
            SimulationConstants.K_GDP_MAINTAIN,
            SimulationConstants.POLLUTE_GDP_HIGH_THRESHOLD,
            SimulationConstants.K_POLLUTE_ENV_OFFSET,
            SimulationConstants.HAPPINESS_COLLAPSE_THRESHOLD,
            SimulationConstants.K_HAPPINESS_COLLAPSE_RATE,
            SimulationConstants.REPUTATION_PENALTY_THRESHOLD,
            SimulationConstants.K_REPUTATION_PENALTY_FLOOR,
            SimulationConstants.GRANT_GREEN_THRESHOLD,
            SimulationConstants.GRANT_TRANSIT_THRESHOLD,
            SimulationConstants.GRANT_LIFE_THRESHOLD,
            SimulationConstants.GRANT_DEV_THRESHOLD,
            SimulationConstants.K_DEBT_CAP_MIN_SCALE,
        };
    }

    [TearDown]
    public void RestoreConstants()
    {
        SimulationConstants.K_TAX_GDP_DRAG = saved[0];
        SimulationConstants.K_HOUSING_TO_HAPPY = saved[1];
        SimulationConstants.K_DEBT_ACCRUAL = saved[2];
        SimulationConstants.K_CRISIS_PENALTY = saved[3];
        SimulationConstants.GDP_MAINTAIN_THRESHOLD = saved[4];
        SimulationConstants.K_GDP_MAINTAIN = saved[5];
        SimulationConstants.POLLUTE_GDP_HIGH_THRESHOLD = saved[6];
        SimulationConstants.K_POLLUTE_ENV_OFFSET = saved[7];
        SimulationConstants.HAPPINESS_COLLAPSE_THRESHOLD = saved[8];
        SimulationConstants.K_HAPPINESS_COLLAPSE_RATE = saved[9];
        SimulationConstants.REPUTATION_PENALTY_THRESHOLD = saved[10];
        SimulationConstants.K_REPUTATION_PENALTY_FLOOR = saved[11];
        SimulationConstants.GRANT_GREEN_THRESHOLD = saved[12];
        SimulationConstants.GRANT_TRANSIT_THRESHOLD = saved[13];
        SimulationConstants.GRANT_LIFE_THRESHOLD = saved[14];
        SimulationConstants.GRANT_DEV_THRESHOLD = saved[15];
        SimulationConstants.K_DEBT_CAP_MIN_SCALE = saved[16];
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static DistrictState MakeMaxDistrict()
    {
        var d = DistrictState.Default();
        d.policyValues.taxRate = 30f;
        d.policyValues.education = 100f;
        d.policyValues.infrastructure = 100f;
        d.policyValues.housing = 100f;
        d.policyValues.environment = 100f;
        d.policyValues.cityContribution = 50f;
        return d;
    }

    private static DistrictState MakeBalancedDistrict()
    {
        var d = DistrictState.Default();
        // Defaults are slider 50, tax 15, city 25 — exactly the "balanced moderate"
        // strategy the goal section calls out.
        return d;
    }

    private static void RunTicks(DistrictState[] districts, ref CityMetrics cm, int n)
    {
        for (int i = 0; i < n; i++)
            TickProcessor.ResolveFullTick(districts, ref cm);
    }

    // ── 1. Max-everything goes into deficit ──────────────────────────────

    [Test]
    public void MaxEverything_GoesIntoDeficit_Integration()
    {
        var districts = new DistrictState[]
        {
            MakeMaxDistrict(),
            MakeBalancedDistrict(),
            MakeBalancedDistrict(),
            MakeBalancedDistrict(),
        };
        var cm = CityMetrics.Default();

        RunTicks(districts, ref cm, 200);

        // After 200 ticks the max-everything district must NOT be sitting on cap
        // reserve with zero debt — that's the bug. Either it's accrued debt OR
        // its reserve has stopped growing (leaked away faster than it gained).
        bool hasDebt = districts[0].debt > 0f;
        bool reserveBelowCap = districts[0].reserve < SimulationConstants.RESERVE_CAP - 1f;
        Assert.IsTrue(hasDebt || reserveBelowCap,
            $"Max-everything should not coast to cap reserve with zero debt. " +
            $"Got debt={districts[0].debt:F1}, reserve={districts[0].reserve:F1}");
    }

    // ── 2. Balanced beats max — the success criterion ────────────────────

    [Test]
    public void BalancedBeatsMax_Integration()
    {
        var districts = new DistrictState[]
        {
            MakeMaxDistrict(),       // index 0
            MakeBalancedDistrict(),  // index 1
            MakeBalancedDistrict(),
            MakeBalancedDistrict(),
        };
        var cm = CityMetrics.Default();

        RunTicks(districts, ref cm, SimulationConstants.TOTAL_TICKS);

        FinalScore maxScore = ScoringSystem.ComputeFinalScore(
            districts[0], cm, districts, districts.Length);
        FinalScore balancedScore = ScoringSystem.ComputeFinalScore(
            districts[1], cm, districts, districts.Length);

        Assert.Greater(balancedScore.finalScore, maxScore.finalScore,
            $"Balanced player should outscore max-everything. " +
            $"Max={maxScore.finalScore:F1}, Balanced={balancedScore.finalScore:F1}");
    }

    // ── 3. Default-policy AI does not bankrupt ───────────────────────────

    [Test]
    public void BalancedDoesNotBankrupt_Integration()
    {
        var districts = new DistrictState[]
        {
            MakeBalancedDistrict(),
            MakeBalancedDistrict(),
            MakeBalancedDistrict(),
            MakeBalancedDistrict(),
        };
        var cm = CityMetrics.Default();

        RunTicks(districts, ref cm, SimulationConstants.TOTAL_TICKS);

        for (int i = 0; i < districts.Length; i++)
        {
            Assert.Less(districts[i].debt, SimulationConstants.DEBT_CAP,
                $"District {i}: default-policy player must not hit debt cap " +
                $"(got debt={districts[i].debt:F1})");
        }
    }

    // ── 4-6. Pollution paths ─────────────────────────────────────────────

    private static DistrictState[] PollutionSnapshot(float gdp, float env)
    {
        var d = DistrictState.Default();
        d.gdp = gdp;
        d.policyValues.environment = env;
        // Single-district snapshot is fine — ApplyPollution iterates sources.
        return new DistrictState[] { d };
    }

    [Test]
    public void Pollution_LowEnvPath_FiresWhenEnvLow()
    {
        var snapshot = PollutionSnapshot(gdp: 50f, env: 10f);
        var before = snapshot[0];

        var after = SpilloverResolver.ApplyPollution(snapshot[0], 0, snapshot, 1);

        Assert.Less(after.sustainability, before.sustainability,
            "Low env (10) + gdp above POLLUTE_GDP_THRESHOLD (50>40) should damage sustain");
    }

    [Test]
    public void Pollution_HighGdpPath_FiresAtHighGdp()
    {
        // env=70 (above 30) shields the low-env path entirely. gdp=100 triggers
        // the new high-gdp path; envOffset=(70-30)*0.5=20, gdpExcess=25, output=5.
        var snapshot = PollutionSnapshot(gdp: 100f, env: 70f);
        var before = snapshot[0];

        var after = SpilloverResolver.ApplyPollution(snapshot[0], 0, snapshot, 1);

        Assert.Less(after.sustainability, before.sustainability,
            "High GDP should leak pollution even with moderate env spending");
    }

    [Test]
    public void Pollution_BothShielded_AtMaxEnv()
    {
        // env=100 fully shields a maxed economy. Low-env path doesn't fire (env>=30).
        // High-gdp path: envOffset=(100-30)*0.5=35, gdpExcess=25, output=max(0,25-35)=0.
        var snapshot = PollutionSnapshot(gdp: 100f, env: 100f);
        var before = snapshot[0];

        var after = SpilloverResolver.ApplyPollution(snapshot[0], 0, snapshot, 1);

        Assert.AreEqual(before.sustainability, after.sustainability, 0.0001f,
            "Max env should fully shield even at gdp=100");
        Assert.AreEqual(before.happiness, after.happiness, 0.0001f);
    }

    // ── 7-8. Happiness-collapse outmigration ─────────────────────────────

    private static DistrictState MakeOutmigDistrict(
        float sustainability = 55f, float happiness = 55f,
        float infrastructure = 50f, float population = 100f)
    {
        var d = DistrictState.Default();
        d.sustainability = sustainability;
        d.happiness = happiness;
        d.infrastructure = infrastructure;
        d.population = population;
        return d;
    }

    private static ScaledSpending MakeMaxedSpending(float pop)
    {
        // High actual costs so K(d) is high — capacity-based outmig won't fire.
        return new ScaledSpending
        {
            actualHousingCost = 1.0f * pop * 3f,
            actualEnvCost = 1.0f * pop * 3f,
            actualMaintenanceCost = 0f,
            actualTotalSpending = 0f,
            scaleFactor = 1f
        };
    }

    [Test]
    public void Outmigration_HappinessCollapse_FiresBelow30()
    {
        var d = MakeOutmigDistrict(sustainability: 50f, happiness: 20f, infrastructure: 100f);
        var s = MakeMaxedSpending(d.population);
        var cm = new CityMetrics
        {
            cityReputation = 100f,
            sharedInfraQuality = 100f,
            metroPopulationPool = 0f
        };

        float outmig = LocalEffectCalculator.ComputeOutmigration(d, s, cm);

        // (30 - 20) * 0.3 = 3.0/tick from happiness-collapse term;
        // sustain=50 above collapse threshold; pop=100 well below K → no overshoot.
        Assert.AreEqual(3.0f, outmig, 0.01f,
            "Happiness=20 should produce (30-20)*0.3 = 3.0 outmigration/tick");
    }

    [Test]
    public void Outmigration_DoubleCollapse_Stable()
    {
        // sustain=10 AND happiness=20 → both collapse channels fire.
        // Stability check: population should converge (monotonic non-increasing
        // toward MIN_POPULATION), not diverge.
        var d = MakeOutmigDistrict(sustainability: 10f, happiness: 20f);
        d.policyValues.taxRate = 5f;       // minimise extra perturbation
        d.policyValues.education = 0f;
        d.policyValues.infrastructure = 0f;
        d.policyValues.housing = 0f;
        d.policyValues.environment = 0f;
        d.policyValues.cityContribution = 0f;

        var districts = new DistrictState[] { d };
        var cm = CityMetrics.Default();

        float prevPop = d.population;
        for (int i = 0; i < 50; i++)
        {
            TickProcessor.ResolveFullTick(districts, ref cm);
            // Population must not grow under double-collapse (no oscillation).
            Assert.LessOrEqual(districts[0].population, prevPop + 0.5f,
                $"Population oscillated upward at tick {i}: prev={prevPop:F2}, now={districts[0].population:F2}");
            prevPop = districts[0].population;
        }

        // After 50 ticks of double-collapse, pop should have collapsed to floor.
        Assert.LessOrEqual(districts[0].population,
            SimulationConstants.MIN_POPULATION + 1f,
            "Double-collapse should drive pop to the MIN_POPULATION clamp");
    }

    // ── 9-10. Reputation revenue penalty ─────────────────────────────────

    [Test]
    public void Revenue_ReputationPenalty_FloorEnforced()
    {
        // Disable all grants by raising their thresholds above max metric values.
        SimulationConstants.GRANT_GREEN_THRESHOLD = 1000f;
        SimulationConstants.GRANT_TRANSIT_THRESHOLD = 100000f;
        SimulationConstants.GRANT_LIFE_THRESHOLD = 1000f;
        SimulationConstants.GRANT_DEV_THRESHOLD = 1000f;

        var d = DistrictState.Default();
        d.gdp = 80f;
        d.population = 200f;
        d.policyValues.taxRate = 15f;

        var snapshot = new DistrictState[] { d };
        var cm = new CityMetrics
        {
            cityReputation = 0f,            // floor case
            sharedInfraQuality = 50f,
            metroPopulationPool = 0f
        };

        var result = TickProcessor.ResolveDistrictTick(0, snapshot, cm);

        float baseRev = BudgetCalculator.ComputeRevenue(15f, 80f, 200f);
        float expected = baseRev * SimulationConstants.K_REPUTATION_PENALTY_FLOOR;
        Assert.AreEqual(expected, result.revenue, 0.5f,
            "Revenue at rep=0 must be exactly base * K_REPUTATION_PENALTY_FLOOR");
    }

    [Test]
    public void Revenue_ReputationAbove40_NoPenalty()
    {
        SimulationConstants.GRANT_GREEN_THRESHOLD = 1000f;
        SimulationConstants.GRANT_TRANSIT_THRESHOLD = 100000f;
        SimulationConstants.GRANT_LIFE_THRESHOLD = 1000f;
        SimulationConstants.GRANT_DEV_THRESHOLD = 1000f;

        var d = DistrictState.Default();
        d.gdp = 80f;
        d.population = 200f;
        d.policyValues.taxRate = 15f;

        var snapshot = new DistrictState[] { d };
        var cm = new CityMetrics
        {
            cityReputation = 45f,           // above threshold (40), no penalty
            sharedInfraQuality = 50f,
            metroPopulationPool = 0f
        };

        var result = TickProcessor.ResolveDistrictTick(0, snapshot, cm);

        float baseRev = BudgetCalculator.ComputeRevenue(15f, 80f, 200f);
        Assert.AreEqual(baseRev, result.revenue, 0.5f,
            "Revenue at rep=45 (above threshold) must equal base ComputeRevenue");
    }

    // ── 11-14. Debt-cap scaleFactor floor (revenue=0 trap fix) ───────────

    [Test]
    public void DebtCapScale_Floored_AtZeroRevenue()
    {
        // debt at cap, revenue=0, demand>0 → raw scaleFactor would be 0;
        // the floor must clamp it to K_DEBT_CAP_MIN_SCALE (0.20).
        var spending = new SpendingBreakdown
        {
            eduCost = 25f,
            infraCost = 25f,
            housingCost = 25f,
            envCost = 25f,
            cityCost = 0f,
            maintenanceCost = 0f,
            totalSpending = 100f,
        };

        var scaled = BudgetCalculator.ComputeDebtCapScaling(
            spending, revenue: 0f, debt: 70f);

        Assert.AreEqual(SimulationConstants.K_DEBT_CAP_MIN_SCALE, scaled.scaleFactor, 0.001f,
            "scaleFactor must be floored to K_DEBT_CAP_MIN_SCALE when revenue is zero");
        // Each line item gets the floored scaleFactor applied.
        Assert.AreEqual(25f * 0.20f, scaled.actualEduCost, 0.01f);
        Assert.AreEqual(100f * 0.20f, scaled.actualTotalSpending, 0.01f);
    }

    [Test]
    public void DebtCapScale_NaturalAboveFloor_NoChange()
    {
        // debt at cap, revenue=50, demand=100 → raw scaleFactor=0.5 (above floor);
        // Math.Max picks the natural value, not the floor.
        var spending = new SpendingBreakdown
        {
            eduCost = 25f,
            infraCost = 25f,
            housingCost = 25f,
            envCost = 25f,
            cityCost = 0f,
            maintenanceCost = 0f,
            totalSpending = 100f,
        };

        var scaled = BudgetCalculator.ComputeDebtCapScaling(
            spending, revenue: 50f, debt: 70f);

        Assert.AreEqual(0.5f, scaled.scaleFactor, 0.001f,
            "When natural ratio is above floor, the natural ratio wins");
    }

    [Test]
    public void DistrictRecoversFromZeroRevenueTrap_Integration()
    {
        // Start partially collapsed: low GDP, deep debt, no reserve, moderate
        // sliders. Without the floor, the district would lock at GDP→0 forever.
        // With the floor, education/infra spending continues at minimum 20%, so
        // metrics can creep back up over time.
        var d = DistrictState.Default();
        d.gdp = 5f;
        d.infrastructure = 30f;
        d.sustainability = 30f;
        d.happiness = 35f;
        d.debt = 80f;       // clamped at the maximum
        d.reserve = 0f;
        d.population = 50f; // reduced from default 150
        d.policyValues.taxRate = 5f;
        d.policyValues.education = 50f;
        d.policyValues.infrastructure = 50f;
        d.policyValues.housing = 50f;
        d.policyValues.environment = 50f;
        d.policyValues.cityContribution = 25f;

        var districts = new DistrictState[] { d };
        var cm = CityMetrics.Default();

        for (int i = 0; i < 500; i++)
            TickProcessor.ResolveFullTick(districts, ref cm);

        // The trap was: GDP collapses to 0 and stays there. With the floor, GDP
        // should not be at the clamp floor (0). We don't assert full recovery —
        // recovery from infra=0 takes thousands of ticks — but the simulation
        // must remain alive and producing some economic activity.
        Assert.Greater(districts[0].gdp, 0f,
            $"GDP must not collapse to 0 — the floor should keep economic activity " +
            $"alive. Got gdp={districts[0].gdp:F2}, infra={districts[0].infrastructure:F1}, " +
            $"sustain={districts[0].sustainability:F1}, debt={districts[0].debt:F1}");
        Assert.IsFalse(float.IsNaN(districts[0].gdp), "GDP must not be NaN");
        Assert.IsFalse(float.IsInfinity(districts[0].gdp), "GDP must not be infinite");
    }

    [Test]
    public void MaxEverythingDoesNotAbuseFloor_Integration()
    {
        // The floor is intended to break the death-spiral lockout, not to rescue
        // max-everything strategies. Max-everything should still end up in
        // deficit / not at cap reserve.
        var districts = new DistrictState[]
        {
            MakeMaxDistrict(),
            MakeBalancedDistrict(),
            MakeBalancedDistrict(),
            MakeBalancedDistrict(),
        };
        var cm = CityMetrics.Default();

        for (int i = 0; i < 500; i++)
            TickProcessor.ResolveFullTick(districts, ref cm);

        bool hasDebt = districts[0].debt > 0f;
        bool reserveBelowCap = districts[0].reserve < SimulationConstants.RESERVE_CAP - 1f;
        Assert.IsTrue(hasDebt || reserveBelowCap,
            $"The debt-cap floor must not accidentally rescue max-everything. " +
            $"After 500 ticks: debt={districts[0].debt:F1}, reserve={districts[0].reserve:F1}");
    }
}

#endif
