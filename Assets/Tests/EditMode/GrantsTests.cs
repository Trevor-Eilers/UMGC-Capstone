// Author: Malcolm Bramble

#if UNITY_EDITOR

using NUnit.Framework;
using Simulation;

[TestFixture]
public class GrantsTests
{
    private float saved_GRANT_BASE_GREEN, saved_GRANT_BASE_TRANSIT;
    private float saved_GRANT_BASE_LIFE, saved_GRANT_BASE_DEV;
    private float saved_GRANT_GREEN_THRESHOLD, saved_GRANT_TRANSIT_THRESHOLD;
    private float saved_GRANT_LIFE_THRESHOLD, saved_GRANT_DEV_THRESHOLD;
    private float saved_RESERVE_CAP, saved_K_STABILIZATION_RATE;

    [SetUp]
    public void SaveConstants()
    {
        saved_GRANT_BASE_GREEN = SimulationConstants.GRANT_BASE_GREEN;
        saved_GRANT_BASE_TRANSIT = SimulationConstants.GRANT_BASE_TRANSIT;
        saved_GRANT_BASE_LIFE = SimulationConstants.GRANT_BASE_LIFE;
        saved_GRANT_BASE_DEV = SimulationConstants.GRANT_BASE_DEV;
        saved_GRANT_GREEN_THRESHOLD = SimulationConstants.GRANT_GREEN_THRESHOLD;
        saved_GRANT_TRANSIT_THRESHOLD = SimulationConstants.GRANT_TRANSIT_THRESHOLD;
        saved_GRANT_LIFE_THRESHOLD = SimulationConstants.GRANT_LIFE_THRESHOLD;
        saved_GRANT_DEV_THRESHOLD = SimulationConstants.GRANT_DEV_THRESHOLD;
        saved_RESERVE_CAP = SimulationConstants.RESERVE_CAP;
        saved_K_STABILIZATION_RATE = SimulationConstants.K_STABILIZATION_RATE;
    }

    [TearDown]
    public void RestoreConstants()
    {
        SimulationConstants.GRANT_BASE_GREEN = saved_GRANT_BASE_GREEN;
        SimulationConstants.GRANT_BASE_TRANSIT = saved_GRANT_BASE_TRANSIT;
        SimulationConstants.GRANT_BASE_LIFE = saved_GRANT_BASE_LIFE;
        SimulationConstants.GRANT_BASE_DEV = saved_GRANT_BASE_DEV;
        SimulationConstants.GRANT_GREEN_THRESHOLD = saved_GRANT_GREEN_THRESHOLD;
        SimulationConstants.GRANT_TRANSIT_THRESHOLD = saved_GRANT_TRANSIT_THRESHOLD;
        SimulationConstants.GRANT_LIFE_THRESHOLD = saved_GRANT_LIFE_THRESHOLD;
        SimulationConstants.GRANT_DEV_THRESHOLD = saved_GRANT_DEV_THRESHOLD;
        SimulationConstants.RESERVE_CAP = saved_RESERVE_CAP;
        SimulationConstants.K_STABILIZATION_RATE = saved_K_STABILIZATION_RATE;
    }

    private static DistrictState MakeEligibleDistrict(
        float debt = 0f, float reserve = 0f,
        float sustainability = 70f, float population = 100f,
        float happiness = 50f, float infrastructure = 50f)
    {
        var d = DistrictState.Default();
        d.debt = debt;
        d.reserve = reserve;
        d.sustainability = sustainability;
        d.population = population;
        d.happiness = happiness;
        d.infrastructure = infrastructure;
        d.grantsEligible = true;
        d.greenGrantStreak = 0;
        d.transitGrantStreak = 0;
        d.lifeGrantStreak = 0;
        d.devGrantStreak = 0;
        d.revenue = 0f;
        return d;
    }

    [Test]
    public void Grants_PayDownDebt_BeforeReserve()
    {
        // Only the green grant fires (sustain=70 > 65, others below threshold).
        // Grant = $20, multiplier 1.0 (first tick) → $20.
        var d = MakeEligibleDistrict(debt: 20f, reserve: 0f, sustainability: 70f);

        d = CityMetricsManager.ResolveFederalFunding(d);

        Assert.AreEqual(0f, d.debt, 0.01f, "Grant should pay debt down to zero");
        Assert.AreEqual(0f, d.reserve, 0.01f, "No leftover for reserve when debt absorbs full grant");
        Assert.AreEqual(20f, d.revenue, 0.01f, "Grant still visible on revenue chip");
        Assert.AreEqual(1, d.greenGrantStreak);
    }

    [Test]
    public void Grants_OverflowFillsReserve()
    {
        var d = MakeEligibleDistrict(debt: 5f, reserve: 0f, sustainability: 70f);

        d = CityMetricsManager.ResolveFederalFunding(d);

        Assert.AreEqual(0f, d.debt, 0.01f);
        Assert.AreEqual(15f, d.reserve, 0.01f, "Remainder of grant after debt should fill reserve");
    }

    [Test]
    public void Grants_NoDebt_GoesToReserve()
    {
        var d = MakeEligibleDistrict(debt: 0f, reserve: 10f, sustainability: 70f);

        d = CityMetricsManager.ResolveFederalFunding(d);

        Assert.AreEqual(0f, d.debt, 0.01f);
        Assert.AreEqual(30f, d.reserve, 0.01f, "Full grant adds to reserve when no debt");
    }

    [Test]
    public void Grants_ReserveCapEnforced()
    {
        SimulationConstants.RESERVE_CAP = 100f;
        var d = MakeEligibleDistrict(debt: 0f, reserve: 95f, sustainability: 70f);

        d = CityMetricsManager.ResolveFederalFunding(d);

        Assert.AreEqual(100f, d.reserve, 0.01f, "Reserve must not exceed RESERVE_CAP");
    }

    [Test]
    public void Grants_PersistAcrossTicks_Integration()
    {
        // Repeated calls should keep paying down debt across ticks. The streak
        // multiplier decays (1.0 → 0.85 → 0.70 → ...) but the debt reductions
        // are cumulative because debt is not recomputed-from-scratch each tick.
        var d = MakeEligibleDistrict(debt: 30f, reserve: 0f, sustainability: 70f);

        float prevDebt = d.debt;
        for (int i = 0; i < 3; i++)
        {
            d = CityMetricsManager.ResolveFederalFunding(d);
            Assert.Less(d.debt, prevDebt + 0.01f, $"Debt should not increase across grant ticks (iteration {i})");
            prevDebt = d.debt;
        }
        Assert.Less(d.debt, 30f, "Debt should be strictly lower after 3 ticks of grants");
    }

    [Test]
    public void Grants_AllFourThresholds_FireWhenMet()
    {
        // Above all four thresholds simultaneously.
        var d = MakeEligibleDistrict(
            debt: 0f, reserve: 0f,
            sustainability: 70f,    // > 65
            population: 280f,       // > 250
            happiness: 80f,         // > 70
            infrastructure: 80f);   // > 70

        d = CityMetricsManager.ResolveFederalFunding(d);

        Assert.AreEqual(1, d.greenGrantStreak);
        Assert.AreEqual(1, d.transitGrantStreak);
        Assert.AreEqual(1, d.lifeGrantStreak);
        Assert.AreEqual(1, d.devGrantStreak);

        // First-tick multiplier is 1.0 for each grant. Total = 4 * GRANT_BASE = 80.
        float expected = SimulationConstants.GRANT_BASE_GREEN
                       + SimulationConstants.GRANT_BASE_TRANSIT
                       + SimulationConstants.GRANT_BASE_LIFE
                       + SimulationConstants.GRANT_BASE_DEV;
        Assert.AreEqual(expected, d.revenue, 0.01f, "All four grants visible on revenue chip");
        Assert.AreEqual(expected, d.reserve, 0.01f, "Full grant pool flows into reserve when debt is zero");
    }

    [Test]
    public void Grants_DebtCapDisabled()
    {
        // Debt above the cap even after stabilization → grantsEligible becomes false.
        // K_STABILIZATION_RATE = 2.5 (half rate 1.25 in the 60-69 band). Set debt=65
        // so post-stabilization debt = 63.75, still >= DEBT_CAP, so grants stay disabled.
        var d = MakeEligibleDistrict(
            debt: 65f, reserve: 0f,
            sustainability: 70f,
            population: 280f,
            happiness: 80f,
            infrastructure: 80f);

        d = CityMetricsManager.ResolveFederalFunding(d);

        Assert.IsFalse(d.grantsEligible, "Debt above cap should disable grants");
        Assert.AreEqual(0f, d.revenue, 0.01f, "No grant revenue when ineligible");
        Assert.AreEqual(0f, d.reserve, 0.01f, "Reserve unchanged when no grant fires");
        Assert.AreEqual(0, d.greenGrantStreak, "Streak unchanged inside the if-block-skipped path");
    }
}

#endif
