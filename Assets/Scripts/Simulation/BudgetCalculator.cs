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

public struct BudgetResult
{
    public float debt;
    public float reserve;
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
    public static SpendingBreakdown ComputeSpendingDemand(PolicyValues values, float population)
    {
        float eduCost = (values.education / 100f) * population * SimulationConstants.K_SPEND;
        float infraCost = (values.infrastructure / 100f) * population * SimulationConstants.K_SPEND;
        float housingCost = (values.housing / 100f) * population * SimulationConstants.K_SPEND;
        float envCost = (values.environment / 100f) * population * SimulationConstants.K_SPEND;
        float cityCost = (values.cityContribution / 50f) * population
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
    /// Returns new debt and reserve values as a BudgetResult.
    /// </summary>
    public static BudgetResult ComputeBudgetBalance(
        float revenue, float totalSpending,
        float debt, float reserve)
    {
        // Reserve decay — applied each tick before budget balance
        reserve *= (1.0f - SimulationConstants.K_RESERVE_DECAY);

        // Use the UNSCALED commitment (totalSpending), not actualTotalSpending. The debt cap
        // throttles delivered effect, but the player still owes what they tried to spend, so
        // debt accrues unboundedly past the cap. Without this, deficit collapses to ~0 the
        // moment debt hits 60 and the player is permanently throttled-but-not-punished.
        float budgetBalance = revenue - totalSpending;

        if (budgetBalance >= 0f)
        {
            // ── SURPLUS ──
            if (debt > 0f)
            {
                // K_DEBT_RECOVERY is the FRACTION of surplus going to debt service.
                // The remaining (1 - K_DEBT_RECOVERY) fills reserve. Clean 1:1 dollar
                // accounting — the previous formulation cancelled itself out and left
                // reserve empty whenever the player carried any debt.
                float debtServiceShare = budgetBalance * SimulationConstants.K_DEBT_RECOVERY;
                float debtReduction = Math.Min(debtServiceShare, debt);
                debt -= debtReduction;

                float surplusRemaining = budgetBalance - debtReduction;
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
                // Remaining deficit accrues as debt. Clamp belongs to Phase 5; just
                // accumulate here so upstream overshoots remain visible during dev.
                debt += deficit * SimulationConstants.K_DEBT_ACCRUAL;
            }
        }

        return new BudgetResult { debt = debt, reserve = reserve };
    }
}
