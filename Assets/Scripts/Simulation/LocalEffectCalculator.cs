// Author: Malcolm Bramble

using System;
using UnityEngine;

namespace Simulation
{
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
            float gdpDrag_tax = -(d.policyValues.taxRate / 100f) * d.gdp
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

            // Diminishing returns on POSITIVE growth only. Squared so the
            // top third of the 0-100 range is genuinely hard to reach: at
            // GDP 80 positive growth runs at 4% effectiveness (was 20%).
            if (totalGdpDelta > 0f)
            {
                float factor = (100.0f - d.gdp) / 100.0f;
                totalGdpDelta *= factor * factor;
            }

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
            // Housing effect is normalized by population and fed through a
            // sqrt curve so doubling spend doesn't double happiness. Without
            // this, the raw budget units (hundreds) would saturate happiness
            // within a few ticks.
            float housingPerCap = s.actualHousingCost / Math.Max(d.population, 1f);
            float happinessDelta_housing = SimulationConstants.K_HOUSING_TO_HAPPY
                                           * (float)Math.Sqrt(housingPerCap);
            float happinessDelta_tax = -(d.policyValues.taxRate / 100.0f)
                                       * SimulationConstants.K_TAX_HAPPY_PENALTY;
            float debtStress = Math.Max(0f, d.debt - 40f) * SimulationConstants.K_DEBT_STRESS;

            // ── COMBINED ──
            float targetHappiness = metricBaseline * SimulationConstants.K_BASELINE_WEIGHT
                                    + happinessDelta_housing
                                    + happinessDelta_tax
                                    - debtStress;

            // Smoothing: K_HAPPY_SMOOTHING=1.0 means instant (no smoothing).
            // No clamp here — Phase 5 owns clamping. Returning unclamped lets dev see
            // overshoots; spillover (Phase 3) may still drive the value out of range
            // before commit.
            float happiness = d.happiness
                              + (targetHappiness - d.happiness) * SimulationConstants.K_HAPPY_SMOOTHING;

            return happiness;
        }

        /// <summary>
        /// Phase 2.3 — Infrastructure Delta.
        /// Growth from spending with diminishing returns at high levels.
        /// Natural decay without investment.
        /// Returns the delta to apply to infrastructure (caller clamps to 0-100).
        /// </summary>
        public static float ComputeInfrastructureDelta(DistrictState d, ScaledSpending s)
        {
            // Linear diminishing returns. Squared DR (like on GDP) was too
            // harsh here because infra has only one growth input, so the
            // metric capped around 52 even at max slider — tier-2 industrial
            // buildings and the development grant were unreachable.
            float factor = (100.0f - d.infrastructure) / 100.0f;
            float infraGrowth = s.actualInfraCost * SimulationConstants.K_INFRA_TO_INFRA * factor;
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
            // Sustainability uses the drains (pop pressure + entropy) to bound
            // its equilibrium — no DR factor, which was too aggressive and
            // pushed sustain below the outmigration threshold at defaults.
            float popDrain = d.population * SimulationConstants.K_POP_SUSTAIN_DRAIN;
            float sustainDecay = d.sustainability * SimulationConstants.K_SUSTAIN_DECAY;

            return sustainDelta_infra + sustainDelta_env - popDrain - sustainDecay;
        }

        /// <summary>
        /// Phase 2.4 (continued) — Carrying capacity.
        /// Returns the population (in thousands) the district can support given its
        /// investments (housing/env/infra), local infrastructure quality, and the
        /// city-wide signals (shared infra, reputation). Used by ComputeOutmigration
        /// and exposed for tests and UI tooltips.
        ///
        /// actualHousingCost and actualEnvCost are already debt-cap-scaled by
        /// BudgetCalculator.ComputeDebtCapScaling, so this formula does NOT multiply
        /// by scaleFactor again — debt throttling naturally collapses these terms.
        /// </summary>
        public static float ComputeCarryingCapacity(DistrictState d, ScaledSpending s, in CityMetrics cm)
        {
            return SimulationConstants.K_BASE
                 + SimulationConstants.K_HOUSING_CAP    * (float)Math.Sqrt(Math.Max(0f, s.actualHousingCost))
                 + SimulationConstants.K_INFRA_CAP      * d.infrastructure
                 + SimulationConstants.K_ENV_CAP        * (float)Math.Sqrt(Math.Max(0f, s.actualEnvCost))
                 + SimulationConstants.K_SHARED_CAP     * cm.sharedInfraQuality
                 + SimulationConstants.K_REPUTATION_CAP * cm.cityReputation;
        }

        /// <summary>
        /// Phase 2.4 (continued) — Outmigration.
        /// Two channels: (1) capacity overshoot (linear + quadratic ramp) when
        /// population exceeds the carrying capacity K(d); (2) a sustain-collapse
        /// floor that still evicts residents when sustainability drops below 15
        /// even if capacity is fine — prevents "high-housing slum with zero
        /// environment" from being a stable strategy.
        /// Returns population loss as a positive number (caller subtracts and clamps).
        /// </summary>
        public static float ComputeOutmigration(DistrictState d, ScaledSpending s, in CityMetrics cm)
        {
            float K = ComputeCarryingCapacity(d, s, cm);
            float overshoot = Math.Max(0f, d.population - K);
            float outmig = SimulationConstants.K_OVERSHOOT_LINEAR * overshoot
                         + SimulationConstants.K_OVERSHOOT_QUAD   * overshoot * overshoot;

            if (d.sustainability < SimulationConstants.SUSTAIN_COLLAPSE_THRESHOLD)
            {
                outmig += (SimulationConstants.SUSTAIN_COLLAPSE_THRESHOLD - d.sustainability)
                        * SimulationConstants.K_SUSTAIN_COLLAPSE_RATE;
            }
            // Mirror channel: happiness collapse drives residents out even when
            // capacity and sustainability are fine. Prevents "max-tax + cheap
            // housing slum" from being viable.
            if (d.happiness < SimulationConstants.HAPPINESS_COLLAPSE_THRESHOLD)
            {
                outmig += (SimulationConstants.HAPPINESS_COLLAPSE_THRESHOLD - d.happiness)
                        * SimulationConstants.K_HAPPINESS_COLLAPSE_RATE;
            }
            return outmig;
        }

        /// <summary>
        /// Phase 2.5 — Pollution Index (0-100 display metric).
        /// Mirrors the §3.2 spillover formula scaled for HUD readability.
        /// Returns 0 when env >= 30 or GDP <= 40 (shortfalls zero out naturally).
        /// </summary>
        public static float ComputePollutionIndex(DistrictState d)
        {
            bool pollutionActive = d.policyValues.environment < SimulationConstants.POLLUTE_ENV_THRESHOLD
                                   && d.gdp > SimulationConstants.POLLUTE_GDP_THRESHOLD;
            if (!pollutionActive) return 0f;
            
            float envShortfall = SimulationConstants.POLLUTE_ENV_THRESHOLD - d.policyValues.environment;
            float gdpExcess    = d.gdp - SimulationConstants.POLLUTE_GDP_THRESHOLD;
            // Raw max is 30 (env 0) + 60 (gdp 100) = 90. Map to 0-100.
            return Mathf.Clamp((envShortfall + gdpExcess) * 100f / 90f, 0f, 100f);
        }
    }
}