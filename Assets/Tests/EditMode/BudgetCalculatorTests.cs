// Author: Malcolm Bramble

using NUnit.Framework;

[TestFixture]
public class BudgetCalculatorTests
{
    // ──────────────────────────────────────────────
    // Helper: save and restore constants between tests
    // so TBD placeholders don't bleed across tests
    // ──────────────────────────────────────────────

    private float savedK_REV;
    private float savedK_SPEND;
    private float savedK_CITY_WEIGHT;
    private float savedK_DEBT_ACCRUAL;
    private float savedK_DEBT_RECOVERY;
    private float savedK_RESERVE_DECAY;
    private float savedDEBT_CAP;
    private float savedRESERVE_CAP;

    [SetUp]
    public void SaveConstants()
    {
        savedK_REV = SimulationConstants.K_REV;
        savedK_SPEND = SimulationConstants.K_SPEND;
        savedK_CITY_WEIGHT = SimulationConstants.K_CITY_WEIGHT;
        savedK_DEBT_ACCRUAL = SimulationConstants.K_DEBT_ACCRUAL;
        savedK_DEBT_RECOVERY = SimulationConstants.K_DEBT_RECOVERY;
        savedK_RESERVE_DECAY = SimulationConstants.K_RESERVE_DECAY;
        savedDEBT_CAP = SimulationConstants.DEBT_CAP;
        savedRESERVE_CAP = SimulationConstants.RESERVE_CAP;
    }

    [TearDown]
    public void RestoreConstants()
    {
        SimulationConstants.K_REV = savedK_REV;
        SimulationConstants.K_SPEND = savedK_SPEND;
        SimulationConstants.K_CITY_WEIGHT = savedK_CITY_WEIGHT;
        SimulationConstants.K_DEBT_ACCRUAL = savedK_DEBT_ACCRUAL;
        SimulationConstants.K_DEBT_RECOVERY = savedK_DEBT_RECOVERY;
        SimulationConstants.K_RESERVE_DECAY = savedK_RESERVE_DECAY;
        SimulationConstants.DEBT_CAP = savedDEBT_CAP;
        SimulationConstants.RESERVE_CAP = savedRESERVE_CAP;
    }

    // ──────────────────────────────────────────────
    // ComputeRevenue
    // ──────────────────────────────────────────────

    [Test]
    public void ComputeRevenue_AtStartingValues_Returns1125()
    {
        // (15/100) * 50 * 150 * 1.0 = 1125
        float revenue = BudgetCalculator.ComputeRevenue(15f, 50f, 150f);
        Assert.AreEqual(1125f, revenue, 0.01f);
    }

    [Test]
    public void ComputeRevenue_HighTax_ScalesLinearly()
    {
        // Double the tax rate (15 → 30) should double revenue
        float revBase = BudgetCalculator.ComputeRevenue(15f, 50f, 150f);
        float revHigh = BudgetCalculator.ComputeRevenue(30f, 50f, 150f);
        Assert.AreEqual(revBase * 2f, revHigh, 0.01f);
    }

    // ──────────────────────────────────────────────
    // ComputeSpendingDemand
    // ──────────────────────────────────────────────

    [Test]
    public void ComputeSpendingDemand_AtStartingValues_Returns1125()
    {
        PolicySliders sliders = PolicySliders.Default();
        SpendingBreakdown spending = BudgetCalculator.ComputeSpendingDemand(sliders, 150f);
        Assert.AreEqual(1125f, spending.totalSpending, 0.01f);
    }

    [Test]
    public void ComputeSpendingDemand_CityContributionNormalizesBy50()
    {
        // cityContribution at 50 (max) should cost the same as a domestic slider at 100
        PolicySliders sliders = PolicySliders.Default();
        sliders.education = 0f;
        sliders.infrastructure = 0f;
        sliders.housing = 0f;
        sliders.environment = 0f;

        // City at max (50) — normalized to 50/50 = 1.0
        sliders.cityContribution = 50f;
        SpendingBreakdown cityMax = BudgetCalculator.ComputeSpendingDemand(sliders, 150f);

        // Compare: a single domestic slider at 100 — normalized to 100/100 = 1.0
        sliders.cityContribution = 0f;
        sliders.education = 100f;
        SpendingBreakdown eduMax = BudgetCalculator.ComputeSpendingDemand(sliders, 150f);

        // Both should produce same cost (K_CITY_WEIGHT = 1.0)
        Assert.AreEqual(eduMax.totalSpending, cityMax.totalSpending, 0.01f);
    }

    // ──────────────────────────────────────────────
    // Balanced budget at starting values
    // ──────────────────────────────────────────────

    [Test]
    public void BalancedBudget_AtStartingValues_RevenueEqualsSpending()
    {
        float revenue = BudgetCalculator.ComputeRevenue(
            SimulationConstants.TAX_RATE_DEFAULT,
            SimulationConstants.GDP_START,
            SimulationConstants.POPULATION_START);

        SpendingBreakdown spending = BudgetCalculator.ComputeSpendingDemand(
            PolicySliders.Default(),
            SimulationConstants.POPULATION_START);

        Assert.AreEqual(revenue, spending.totalSpending, 0.01f,
            "Revenue and spending must be equal at default starting values");
    }

    // ──────────────────────────────────────────────
    // ComputeDebtCapScaling
    // ──────────────────────────────────────────────

    [Test]
    public void DebtCapScaling_BelowCap_ScaleFactorIsOne()
    {
        SpendingBreakdown spending = new SpendingBreakdown
        {
            eduCost = 400f, infraCost = 400f, housingCost = 400f,
            envCost = 400f, cityCost = 200f, totalSpending = 1800f
        };

        ScaledSpending result = BudgetCalculator.ComputeDebtCapScaling(spending, 1000f, 59f);
        Assert.AreEqual(1.0f, result.scaleFactor, 0.001f);
        Assert.AreEqual(1800f, result.actualTotalSpending, 0.01f);
    }

    [Test]
    public void DebtCapScaling_AtCap_SpendingExceedsRevenue_ScalesDown()
    {
        SpendingBreakdown spending = new SpendingBreakdown
        {
            eduCost = 500f, infraCost = 500f, housingCost = 500f,
            envCost = 500f, cityCost = 0f, totalSpending = 2000f
        };
        float revenue = 1000f;
        float debt = 60f; // exactly at cap

        ScaledSpending result = BudgetCalculator.ComputeDebtCapScaling(spending, revenue, debt);

        Assert.AreEqual(0.5f, result.scaleFactor, 0.001f,
            "scaleFactor should be revenue/totalSpending = 1000/2000 = 0.5");
        Assert.AreEqual(1000f, result.actualTotalSpending, 0.01f);
        Assert.AreEqual(250f, result.actualEduCost, 0.01f);
    }

    [Test]
    public void DebtCapScaling_AtCap_RevenueCoversSpending_ScaleFactorIsOne()
    {
        SpendingBreakdown spending = new SpendingBreakdown
        {
            eduCost = 200f, infraCost = 200f, housingCost = 200f,
            envCost = 200f, cityCost = 0f, totalSpending = 800f
        };

        ScaledSpending result = BudgetCalculator.ComputeDebtCapScaling(spending, 1000f, 65f);
        Assert.AreEqual(1.0f, result.scaleFactor, 0.001f);
    }

    [Test]
    public void DebtCapScaling_PreservesSliderRatios()
    {
        // Education set 2x housing — ratio should be preserved after scaling
        SpendingBreakdown spending = new SpendingBreakdown
        {
            eduCost = 600f, infraCost = 300f, housingCost = 300f,
            envCost = 300f, cityCost = 0f, totalSpending = 1500f
        };

        ScaledSpending result = BudgetCalculator.ComputeDebtCapScaling(spending, 1000f, 60f);

        float ratio = result.actualEduCost / result.actualHousingCost;
        Assert.AreEqual(2.0f, ratio, 0.001f,
            "Education should still be 2x housing after scaling");
    }

    // ──────────────────────────────────────────────
    // ComputeBudgetBalance
    // ──────────────────────────────────────────────

    [Test]
    public void BudgetBalance_Surplus_PaysDebtBeforeReserve()
    {
        SimulationConstants.K_DEBT_RECOVERY = 0.5f;
        SimulationConstants.K_RESERVE_DECAY = 0f; // disable decay for this test

        float debt = 10f;
        float reserve = 0f;
        float revenue = 1200f;
        float spending = 1000f; // surplus = 200

        BudgetCalculator.ComputeBudgetBalance(revenue, spending, ref debt, ref reserve);

        // maxDebtReduction = 200 * 0.5 = 100, but debt is only 10, so reduce by 10
        // surplusUsedForDebt = 10 / 0.5 = 20
        // surplusRemaining = 200 - 20 = 180
        Assert.AreEqual(0f, debt, 0.01f, "Debt should be fully paid off");
        Assert.AreEqual(180f, reserve, 0.01f, "Remaining surplus goes to reserve");
    }

    [Test]
    public void BudgetBalance_Surplus_NoDebt_AllToReserve()
    {
        SimulationConstants.K_RESERVE_DECAY = 0f;

        float debt = 0f;
        float reserve = 0f;
        float revenue = 1200f;
        float spending = 1000f; // surplus = 200

        BudgetCalculator.ComputeBudgetBalance(revenue, spending, ref debt, ref reserve);

        Assert.AreEqual(0f, debt, 0.01f);
        Assert.AreEqual(200f, reserve, 0.01f, "All surplus flows to reserve when no debt");
    }

    [Test]
    public void BudgetBalance_Deficit_ReserveAbsorbsBeforeDebt()
    {
        SimulationConstants.K_RESERVE_DECAY = 0f;
        SimulationConstants.K_DEBT_ACCRUAL = 1.0f;

        float debt = 20f;
        float reserve = 300f;
        float revenue = 800f;
        float spending = 1000f; // deficit = 200

        BudgetCalculator.ComputeBudgetBalance(revenue, spending, ref debt, ref reserve);

        // Reserve absorbs all 200 of the deficit
        Assert.AreEqual(100f, reserve, 0.01f, "Reserve should absorb the deficit");
        Assert.AreEqual(20f, debt, 0.01f, "Debt should not change when reserve covers deficit");
    }

    [Test]
    public void BudgetBalance_Deficit_ReservePartiallyAbsorbs_RemainderAccruesDebt()
    {
        SimulationConstants.K_RESERVE_DECAY = 0f;
        SimulationConstants.K_DEBT_ACCRUAL = 1.0f;

        float debt = 20f;
        float reserve = 50f;
        float revenue = 800f;
        float spending = 1000f; // deficit = 200

        BudgetCalculator.ComputeBudgetBalance(revenue, spending, ref debt, ref reserve);

        // Reserve absorbs 50, remaining deficit = 150
        // debt += 150 * 1.0 = 150, total debt = 170... but clamped to 80
        Assert.AreEqual(0f, reserve, 0.01f, "Reserve fully drained");
        Assert.AreEqual(80f, debt, 0.01f, "Debt clamped to 80 max");
    }

    [Test]
    public void BudgetBalance_Deficit_NoReserve_DebtAccrues()
    {
        SimulationConstants.K_RESERVE_DECAY = 0f;
        SimulationConstants.K_DEBT_ACCRUAL = 1.0f;

        float debt = 20f;
        float reserve = 0f;
        float revenue = 900f;
        float spending = 1000f; // deficit = 100

        BudgetCalculator.ComputeBudgetBalance(revenue, spending, ref debt, ref reserve);

        // debt += 100 * 1.0 = 100, total = 120, clamped to 80
        Assert.AreEqual(0f, reserve, 0.01f);
        Assert.AreEqual(80f, debt, 0.01f);
    }

    [Test]
    public void BudgetBalance_Deficit_SmallDeficit_DebtAccruesWithoutCap()
    {
        SimulationConstants.K_RESERVE_DECAY = 0f;
        SimulationConstants.K_DEBT_ACCRUAL = 1.0f;

        float debt = 20f;
        float reserve = 0f;
        float revenue = 990f;
        float spending = 1000f; // deficit = 10

        BudgetCalculator.ComputeBudgetBalance(revenue, spending, ref debt, ref reserve);

        Assert.AreEqual(30f, debt, 0.01f, "Debt increases by deficit * K_DEBT_ACCRUAL");
    }

    [Test]
    public void BudgetBalance_ReserveDecay_AppliedBeforeBudget()
    {
        SimulationConstants.K_RESERVE_DECAY = 0.005f;

        float debt = 0f;
        float reserve = 1000f;
        float revenue = 1000f;
        float spending = 1000f; // balanced budget

        BudgetCalculator.ComputeBudgetBalance(revenue, spending, ref debt, ref reserve);

        // Reserve decays first: 1000 * (1 - 0.005) = 995
        // Budget is balanced so no further changes
        Assert.AreEqual(995f, reserve, 0.01f, "Reserve should lose 0.5% from decay");
    }

    [Test]
    public void BudgetBalance_ReserveCapped()
    {
        SimulationConstants.K_RESERVE_DECAY = 0f;
        SimulationConstants.RESERVE_CAP = 100f;

        float debt = 0f;
        float reserve = 50f;
        float revenue = 1200f;
        float spending = 1000f; // surplus = 200

        BudgetCalculator.ComputeBudgetBalance(revenue, spending, ref debt, ref reserve);

        Assert.AreEqual(100f, reserve, 0.01f, "Reserve capped at RESERVE_CAP");
    }

    [Test]
    public void BudgetBalance_BalancedBudget_NoChange()
    {
        SimulationConstants.K_RESERVE_DECAY = 0f;

        float debt = 15f;
        float reserve = 0f;
        float revenue = 1125f;
        float spending = 1125f;

        BudgetCalculator.ComputeBudgetBalance(revenue, spending, ref debt, ref reserve);

        // Balanced: budgetBalance = 0 → surplus path → debt > 0
        // maxDebtReduction = 0 * K_DEBT_RECOVERY = 0
        // surplusRemaining = 0
        Assert.AreEqual(15f, debt, 0.01f, "Debt unchanged on balanced budget");
        Assert.AreEqual(0f, reserve, 0.01f, "Reserve unchanged on balanced budget");
    }
}
