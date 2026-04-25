// Author: Malcolm Bramble

using System;

namespace Simulation
{
    /// <summary>
    /// City-wide metric computation and per-district city effects.
    /// Read-only methods (ComputeCityReputation, ComputeMetroPopulationPool,
    /// UpdateSharedInfrastructure) are deterministic from the snapshot —
    /// all clients produce identical results.
    /// Per-district methods (ComputePopulationDelta, ResolveFederalFunding)
    /// compute effects for one district at a time.
    /// </summary>
    public static class CityMetricsManager
    {
        /// <summary>
        /// Phase 4.1 — City Reputation.
        /// Weighted average of five metrics across active districts, minus variance penalty.
        /// Population excluded to avoid feedback loop.
        /// </summary>
        public static float ComputeCityReputation(DistrictState[] snapshot, int numActivePlayers)
        {
            float sumHappy = 0f, sumSustain = 0f, sumInfra = 0f, sumGdp = 0f, sumInverseDebt = 0f;

            for (int i = 0; i < numActivePlayers; i++)
            {
                sumHappy += snapshot[i].happiness;
                sumSustain += snapshot[i].sustainability;
                sumInfra += snapshot[i].infrastructure;
                sumGdp += snapshot[i].gdp;
                sumInverseDebt += 100f - (snapshot[i].debt * 100f / 80f);
            }

            float n = numActivePlayers;
            float avgHappy = sumHappy / n;
            float avgSustain = sumSustain / n;
            float avgInfra = sumInfra / n;
            float avgGdp = sumGdp / n;
            float avgInverseDebt = sumInverseDebt / n;

            float weightedAvg = avgHappy * 0.25f
                                + avgSustain * 0.25f
                                + avgInfra * 0.20f
                                + avgGdp * 0.15f
                                + avgInverseDebt * 0.15f;

            // Standard deviation for each metric
            float sdHappy = StdDev(snapshot, numActivePlayers, avgHappy, d => d.happiness);
            float sdSustain = StdDev(snapshot, numActivePlayers, avgSustain, d => d.sustainability);
            float sdInfra = StdDev(snapshot, numActivePlayers, avgInfra, d => d.infrastructure);
            float sdGdp = StdDev(snapshot, numActivePlayers, avgGdp, d => d.gdp);
            float sdInverseDebt = StdDevInverseDebt(snapshot, numActivePlayers, avgInverseDebt);

            float meanStdDev = (sdHappy + sdSustain + sdInfra + sdGdp + sdInverseDebt) / 5f;
            float variancePenalty = meanStdDev * SimulationConstants.K_VARIANCE_PENALTY;

            return Math.Min(Math.Max(weightedAvg - variancePenalty, 0f), 100f);
        }

        private static float StdDev(
            DistrictState[] snapshot, int n, float mean, Func<DistrictState, float> selector)
        {
            float sumSqDiff = 0f;
            for (int i = 0; i < n; i++)
            {
                float diff = selector(snapshot[i]) - mean;
                sumSqDiff += diff * diff;
            }
            return (float)Math.Sqrt(sumSqDiff / n);
        }

        private static float StdDevInverseDebt(DistrictState[] snapshot, int n, float mean)
        {
            float sumSqDiff = 0f;
            for (int i = 0; i < n; i++)
            {
                float inverseDebt = 100f - (snapshot[i].debt * 100f / 80f);
                float diff = inverseDebt - mean;
                sumSqDiff += diff * diff;
            }
            return (float)Math.Sqrt(sumSqDiff / n);
        }

        /// <summary>
        /// Compute metro-wide population inflow/outflow based on city reputation.
        /// Deterministic from reputation value — all clients get the same result.
        /// </summary>
        public static float ComputeMetroPopulationPool(float cityReputation)
        {
            // Continuous piecewise — no discontinuity at the rep=70 boundary.
            // Above 70: the normal-band slope continues, plus an additional
            //   (K_POP_INFLOW_HIGH - K_POP_INFLOW_NORMAL) slope on top.
            // 30-70: linear inflow centered at rep=50.
            // Below 30: outflow, anchored to the value at rep=30.
            if (cityReputation > 70f)
                return (cityReputation - 50f) * SimulationConstants.K_POP_INFLOW_NORMAL
                     + (cityReputation - 70f) *
                       (SimulationConstants.K_POP_INFLOW_HIGH - SimulationConstants.K_POP_INFLOW_NORMAL);
            if (cityReputation >= 30f)
                return (cityReputation - 50f) * SimulationConstants.K_POP_INFLOW_NORMAL;
            return (cityReputation - 30f) * SimulationConstants.K_POP_OUTFLOW
                 + (30f - 50f) * SimulationConstants.K_POP_INFLOW_NORMAL;
        }

        /// <summary>
        /// Phase 4.2 — Population Distribution for a single district.
        /// Computes this district's share of the metro population pool based on
        /// attractiveness (happiness 40%, housing 40%, tax inverse 20%).
        /// Returns the population delta to add to this district.
        /// </summary>
        public static float ComputePopulationDelta(
            int districtIndex, DistrictState[] snapshot,
            CityMetrics cityMetrics, int numActivePlayers)
        {
            float newResidents = cityMetrics.metroPopulationPool;

            float totalAttractiveness = 0f;
            float myAttractiveness = 0f;

            for (int i = 0; i < numActivePlayers; i++)
            {
                float a = snapshot[i].happiness * 0.40f
                          + (snapshot[i].policyValues.housing / 100.0f) * 0.40f
                          + (1.0f - snapshot[i].policyValues.taxRate / 30.0f) * 0.20f;
                totalAttractiveness += a;
                if (i == districtIndex)
                    myAttractiveness = a;
            }

            if (totalAttractiveness > 0f)
            {
                float share;
                if (newResidents >= 0f)
                {
                    // Inflow: most attractive district gets the largest share.
                    share = myAttractiveness / totalAttractiveness;
                }
                else
                {
                    // Outflow: least attractive district bleeds most. Invert the share
                    // so a struggling district loses more population than a thriving one.
                    if (numActivePlayers > 1)
                        share = (totalAttractiveness - myAttractiveness)
                                / ((numActivePlayers - 1) * totalAttractiveness);
                    else
                        share = 1.0f;
                }
                return newResidents * share;
            }
            return newResidents / numActivePlayers;
        }

        /// <summary>
        /// Phase 4.3 — Shared Infrastructure Quality.
        /// Grows from collective city contribution spending, decays without it.
        /// Returns the new sharedInfraQuality value.
        /// </summary>
        public static float UpdateSharedInfrastructure(float totalCitySpending, float currentSharedInfra)
        {
            float sharedInfraGrowth = totalCitySpending * SimulationConstants.K_SHARED_INFRA_GROWTH;
            float sharedInfraDecay = currentSharedInfra * SimulationConstants.K_SHARED_INFRA_DECAY;

            return Math.Min(Math.Max(
                currentSharedInfra + sharedInfraGrowth - sharedInfraDecay, 0f), 100f);
        }

        /// <summary>
        /// Phase 4.4 &amp; 4.5 — Federal Funding for a single district.
        /// Competitive grants (4.4): awards bonus revenue if thresholds met.
        /// Stabilization transfers (4.5): reduces debt if debt >= 70.
        /// Returns a new DistrictState with updated revenue, grant streaks,
        /// debt, and grantsEligible.
        /// </summary>
        public static DistrictState ResolveFederalFunding(DistrictState d)
        {
            // ── 4.5 — Stabilization Transfers ──
            // Full rate at debt >= 70 (crisis); half rate in the 60-69 recovery band so
            // a district climbing from debt 75 → 65 is not stranded between the
            // stabilization threshold and the grant threshold.
            if (d.debt >= 70f)
            {
                d.debt -= SimulationConstants.K_STABILIZATION_RATE;
            }
            else if (d.debt >= SimulationConstants.DEBT_CAP)
            {
                d.debt -= SimulationConstants.K_STABILIZATION_RATE * 0.5f;
            }

            // Grants enable once debt drops below the cap.
            d.grantsEligible = (d.debt < SimulationConstants.DEBT_CAP);

            // ── 4.4 — Competitive Grants ──
            float grantRevenue = 0f;

            if (d.debt < SimulationConstants.DEBT_CAP && d.grantsEligible)
            {
                // Green Infrastructure Grant — sustainability > 70
                if (d.sustainability > 70f)
                {
                    float multiplier = Math.Max(0.30f,
                        1.0f - d.greenGrantStreak * 0.15f);
                    grantRevenue += SimulationConstants.GRANT_BASE_GREEN * multiplier;
                    d.greenGrantStreak += 1;
                }
                else
                {
                    d.greenGrantStreak = 0;
                }

                // Federal Transit Grant — population > 300k
                if (d.population > 300.0f)
                {
                    float multiplier = Math.Max(0.30f,
                        1.0f - d.transitGrantStreak * 0.15f);
                    grantRevenue += SimulationConstants.GRANT_BASE_TRANSIT * multiplier;
                    d.transitGrantStreak += 1;
                }
                else
                {
                    d.transitGrantStreak = 0;
                }

                // Quality of Life Grant — happiness > 75
                if (d.happiness > 75f)
                {
                    float multiplier = Math.Max(0.30f,
                        1.0f - d.lifeGrantStreak * 0.15f);
                    grantRevenue += SimulationConstants.GRANT_BASE_LIFE * multiplier;
                    d.lifeGrantStreak += 1;
                }
                else
                {
                    d.lifeGrantStreak = 0;
                }

                // Development Grant — infrastructure > 80
                if (d.infrastructure > 80f)
                {
                    float multiplier = Math.Max(0.30f,
                        1.0f - d.devGrantStreak * 0.15f);
                    grantRevenue += SimulationConstants.GRANT_BASE_DEV * multiplier;
                    d.devGrantStreak += 1;
                }
                else
                {
                    d.devGrantStreak = 0;
                }
            }

            d.revenue += grantRevenue;
            return d;
        }
    }
}
