// Author: Malcolm Bramble

using System;

public struct SpendingBreakdown
{
    public float eduCost;
    public float infraCost;
    public float housingCost;
    public float envCost;
    public float cityCost;
    public float totalSpending;
}

public struct ScaledSpending
{
    public float actualEduCost;
    public float actualInfraCost;
    public float actualHousingCost;
    public float actualEnvCost;
    public float actualCityCost;
    public float actualTotalSpending;
    public float scaleFactor;
}

public static class BudgetCalculator
{
    /// <summary>
    /// Step 1.1 — Revenue.
    /// revenue = (taxRate / 100) * GDP * population * K_REV
    /// Population field is already in thousands (150.0 = 150k residents).
    /// The spec's (population / 1000) converts absolute to thousands;
    /// since our field is already in thousands, we use it directly.
    /// At defaults: (15/100) * 50 * 150 * 1.0 = 1125
    /// </summary>
    public static float ComputeRevenue(float taxRate, float gdp, float population)
    {
        return (taxRate / 100f) * gdp * population * SimulationConstants.K_REV;
    }

    /// <summary>
    /// Step 1.2 — Spending Demand.
    /// Each slider generates cost proportional to slider position and population.
    /// City contribution divides by 50 (its max) instead of 100.
    /// Population field is already in thousands — used directly (see ComputeRevenue).
    /// At defaults: (0.5+0.5+0.5+0.5)*150*3.0 + (25/50)*150*3.0*1.0 = 900+225 = 1125
    /// </summary>
    public static SpendingBreakdown ComputeSpendingDemand(PolicySliders sliders, float population)
    {
        float eduCost = (sliders.education / 100f) * population * SimulationConstants.K_SPEND;
        float infraCost = (sliders.infrastructure / 100f) * population * SimulationConstants.K_SPEND;
        float housingCost = (sliders.housing / 100f) * population * SimulationConstants.K_SPEND;
        float envCost = (sliders.environment / 100f) * population * SimulationConstants.K_SPEND;
        float cityCost = (sliders.cityContribution / 50f) * population
                         * SimulationConstants.K_SPEND * SimulationConstants.K_CITY_WEIGHT;

        return new SpendingBreakdown
        {
            eduCost = eduCost,
            infraCost = infraCost,
            housingCost = housingCost,
            envCost = envCost,
            cityCost = cityCost,
            totalSpending = eduCost + infraCost + housingCost + envCost + cityCost
        };
    }

    /// <summary>
    /// Step 1.3 — Debt Cap Scaling.
    /// At debt >= DEBT_CAP, if spending exceeds revenue, all spending is scaled
    /// down proportionally so actual spend = revenue. Slider ratios preserved.
    /// </summary>
    public static ScaledSpending ComputeDebtCapScaling(
        SpendingBreakdown spending, float revenue, float debt)
    {
        float scaleFactor;

        if (debt >= SimulationConstants.DEBT_CAP)
        {
            if (spending.totalSpending > revenue && spending.totalSpending > 0f)
                scaleFactor = revenue / spending.totalSpending;
            else
                scaleFactor = 1.0f;
        }
        else
        {
            scaleFactor = 1.0f;
        }

        return new ScaledSpending
        {
            actualEduCost = spending.eduCost * scaleFactor,
            actualInfraCost = spending.infraCost * scaleFactor,
            actualHousingCost = spending.housingCost * scaleFactor,
            actualEnvCost = spending.envCost * scaleFactor,
            actualCityCost = spending.cityCost * scaleFactor,
            actualTotalSpending = spending.totalSpending * scaleFactor,
            scaleFactor = scaleFactor
        };
    }

    /// <summary>
    /// Step 1.4 — Budget Balance → Reserve → Debt.
    /// Reserve decay applied first. Then surplus pays debt before reserve;
    /// deficit drains reserve before accruing debt. 3:1 asymmetry via K constants.
    /// </summary>
    public static void ComputeBudgetBalance(
        float revenue, float actualTotalSpending,
        ref float debt, ref float reserve)
    {
        // Reserve decay — applied each tick before budget balance
        reserve = reserve * (1.0f - SimulationConstants.K_RESERVE_DECAY);

        float budgetBalance = revenue - actualTotalSpending;

        if (budgetBalance >= 0f)
        {
            // ── SURPLUS ──
            if (debt > 0f)
            {
                // Pay down debt first
                float maxDebtReduction = budgetBalance * SimulationConstants.K_DEBT_RECOVERY;
                float debtReduction = Math.Min(maxDebtReduction, debt);
                debt -= debtReduction;

                // Whatever surplus remains after debt service fills the reserve
                float surplusUsedForDebt = debtReduction / SimulationConstants.K_DEBT_RECOVERY;
                float surplusRemaining = budgetBalance - surplusUsedForDebt;
                reserve = Math.Min(reserve + surplusRemaining, SimulationConstants.RESERVE_CAP);
            }
            else
            {
                // No debt — all surplus flows into reserve
                reserve = Math.Min(reserve + budgetBalance, SimulationConstants.RESERVE_CAP);
            }
        }
        else
        {
            // ── DEFICIT ──
            float deficit = Math.Abs(budgetBalance);

            if (reserve > 0f)
            {
                // Drain reserve before touching debt
                float absorbed = Math.Min(deficit, reserve);
                reserve -= absorbed;
                deficit -= absorbed;
            }

            if (deficit > 0f)
            {
                // Remaining deficit accrues as debt
                debt = Math.Min(Math.Max(debt + deficit * SimulationConstants.K_DEBT_ACCRUAL, 0f), 80f);
            }
        }
    }
}
