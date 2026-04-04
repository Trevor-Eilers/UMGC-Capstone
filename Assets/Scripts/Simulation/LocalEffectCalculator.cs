// Author: Malcolm Bramble

using System;

public static class LocalEffectCalculator
{
    /// <summary>
    /// Phase 2.1 — GDP Delta.
    /// Four growth inputs, two drags, natural decay, diminishing returns on positive growth.
    /// Returns the delta to apply to GDP (caller clamps to 0-100).
    /// </summary>
    public static float ComputeGdpDelta(DistrictState d, ScaledSpending s)
    {
        // ── GROWTH INPUTS ──
        float gdpGrowth_edu = s.actualEduCost * SimulationConstants.K_EDU_TO_GDP;
        float gdpGrowth_infra = (d.infrastructure - 50f) * SimulationConstants.K_INFRA_TO_GDP;
        float gdpGrowth_pop = (float)Math.Log(Math.Max(d.population, 1.0f))
                              * SimulationConstants.K_POP_TO_GDP;
        float gdpGrowth_sustain = (d.sustainability - 50f) * SimulationConstants.K_SUSTAIN_TO_GDP;

        // ── DRAGS ──
        float gdpDrag_tax = -(d.sliders.taxRate / 100f) * d.gdp
                            * SimulationConstants.K_TAX_GDP_DRAG;
        float gdpDrag_env = -s.actualEnvCost * SimulationConstants.K_ENV_GDP_DRAG;

        // ── DECAY ──
        float gdpDecay = d.gdp * SimulationConstants.K_GDP_DECAY;

        // ── AGGREGATE ──
        float totalGdpDelta = gdpGrowth_edu
                            + gdpGrowth_infra
                            + gdpGrowth_pop
                            + gdpGrowth_sustain
                            + gdpDrag_tax
                            + gdpDrag_env
                            - gdpDecay;

        // Diminishing returns on POSITIVE growth only
        if (totalGdpDelta > 0f)
            totalGdpDelta = totalGdpDelta * (1.0f - d.gdp / 100.0f);

        return totalGdpDelta;
    }

    /// <summary>
    /// Phase 2.2 — Happiness.
    /// Recomputed from scratch each tick (not delta-based).
    /// Returns the new happiness value, clamped to 0-100.
    /// </summary>
    public static float ComputeHappiness(DistrictState d, ScaledSpending s)
    {
        // ── METRIC BASELINE ──
        float inverseDebt = 100.0f - (d.debt * 100.0f / 80.0f);

        float metricBaseline = d.gdp * SimulationConstants.W_HAPPY_GDP
                             + d.infrastructure * SimulationConstants.W_HAPPY_INFRA
                             + d.sustainability * SimulationConstants.W_HAPPY_SUSTAIN
                             + inverseDebt * SimulationConstants.W_HAPPY_DEBT;

        // ── DIRECT EFFECTS ──
        float happinessDelta_housing = s.actualHousingCost * SimulationConstants.K_HOUSING_TO_HAPPY;
        float happinessDelta_tax = -(d.sliders.taxRate / 100.0f)
                                   * SimulationConstants.K_TAX_HAPPY_PENALTY;
        float debtStress = Math.Max(0f, d.debt - 40f) * SimulationConstants.K_DEBT_STRESS;

        // ── COMBINED ──
        float targetHappiness = metricBaseline * SimulationConstants.K_BASELINE_WEIGHT
                              + happinessDelta_housing
                              + happinessDelta_tax
                              - debtStress;

        // Smoothing: K_HAPPY_SMOOTHING=1.0 means instant (no smoothing)
        float happiness = d.happiness
                        + (targetHappiness - d.happiness) * SimulationConstants.K_HAPPY_SMOOTHING;

        return Math.Min(Math.Max(happiness, 0f), 100f);
    }

    /// <summary>
    /// Phase 2.3 — Infrastructure Delta.
    /// Growth from spending with diminishing returns at high levels.
    /// Natural decay without investment.
    /// Returns the delta to apply to infrastructure (caller clamps to 0-100).
    /// </summary>
    public static float ComputeInfrastructureDelta(DistrictState d, ScaledSpending s)
    {
        float infraGrowth = s.actualInfraCost * SimulationConstants.K_INFRA_TO_INFRA
                          * (1.0f - d.infrastructure / 100.0f);
        float infraDecay = d.infrastructure * SimulationConstants.K_INFRA_DECAY;

        return infraGrowth - infraDecay;
    }

    /// <summary>
    /// Phase 2.4 — Sustainability Delta.
    /// Infrastructure (primary) and environment (secondary) inputs.
    /// Population pressure and natural decay as drains.
    /// Returns the delta to apply to sustainability (caller clamps to 0-100).
    /// </summary>
    public static float ComputeSustainabilityDelta(DistrictState d, ScaledSpending s)
    {
        // ── INPUTS ──
        float sustainDelta_infra = (d.infrastructure - 50f)
                                   * SimulationConstants.K_INFRA_TO_SUSTAIN;
        float sustainDelta_env = s.actualEnvCost * SimulationConstants.K_ENV_TO_SUSTAIN;

        // ── DRAINS ──
        // Population field is already in thousands; spec's /1000 converts absolute
        // to thousands, so we use population directly (same convention as BudgetCalculator).
        float popDrain = d.population * SimulationConstants.K_POP_SUSTAIN_DRAIN;
        float sustainDecay = d.sustainability * SimulationConstants.K_SUSTAIN_DECAY;

        return sustainDelta_infra + sustainDelta_env - popDrain - sustainDecay;
    }

    /// <summary>
    /// Phase 2.4 (continued) — Outmigration.
    /// When sustainability drops below threshold 30, residents leave.
    /// Returns population loss as a positive number (caller subtracts and clamps).
    /// Returns 0 if sustainability is at or above threshold.
    /// </summary>
    public static float ComputeOutmigration(DistrictState d)
    {
        if (d.sustainability < SimulationConstants.SUSTAIN_MIGRATION_THRESHOLD)
        {
            return (SimulationConstants.SUSTAIN_MIGRATION_THRESHOLD - d.sustainability)
                   * SimulationConstants.K_MIGRATION_RATE;
        }
        return 0f;
    }
}
