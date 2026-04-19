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
            if (cityReputation > 70f)
                return (cityReputation - 70f) * SimulationConstants.K_POP_INFLOW_HIGH;
            if (cityReputation >= 30f)
                return (cityReputation - 50f) * SimulationConstants.K_POP_INFLOW_NORMAL;
            return (cityReputation - 30f) * SimulationConstants.K_POP_OUTFLOW;
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
                return newResidents * (myAttractiveness / totalAttractiveness);
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
            // ── 4.5 — Stabilization Transfers (check first, sets grantsEligible) ──
            if (d.debt >= 70f)
            {
                d.debt -= SimulationConstants.K_STABILIZATION_RATE;
                d.grantsEligible = false;
            }
            else
            {
                if (d.debt < SimulationConstants.DEBT_CAP)
                    d.grantsEligible = true;
            }

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
