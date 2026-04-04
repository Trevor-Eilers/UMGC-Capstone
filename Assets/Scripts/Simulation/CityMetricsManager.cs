// Author: Malcolm Bramble

using System;

public static class CityMetricsManager
{
    /// <summary>
    /// Phase 4.1 — City Reputation.
    /// Weighted average of five metrics across active districts, minus variance penalty.
    /// Population is excluded to avoid feedback loop.
    /// </summary>
    public static float ComputeCityReputation(DistrictState[] districts, int numActivePlayers)
    {
        float sumHappy = 0f, sumSustain = 0f, sumInfra = 0f, sumGdp = 0f, sumInverseDebt = 0f;

        for (int i = 0; i < numActivePlayers; i++)
        {
            sumHappy += districts[i].happiness;
            sumSustain += districts[i].sustainability;
            sumInfra += districts[i].infrastructure;
            sumGdp += districts[i].gdp;
            sumInverseDebt += 100f - (districts[i].debt * 100f / 80f);
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
        float sdHappy = StdDev(districts, numActivePlayers, avgHappy, d => d.happiness);
        float sdSustain = StdDev(districts, numActivePlayers, avgSustain, d => d.sustainability);
        float sdInfra = StdDev(districts, numActivePlayers, avgInfra, d => d.infrastructure);
        float sdGdp = StdDev(districts, numActivePlayers, avgGdp, d => d.gdp);
        float sdInverseDebt = StdDevInverseDebt(districts, numActivePlayers, avgInverseDebt);

        float meanStdDev = (sdHappy + sdSustain + sdInfra + sdGdp + sdInverseDebt) / 5f;
        float variancePenalty = meanStdDev * SimulationConstants.K_VARIANCE_PENALTY;

        return Math.Min(Math.Max(weightedAvg - variancePenalty, 0f), 100f);
    }

    private static float StdDev(
        DistrictState[] districts, int n, float mean, Func<DistrictState, float> selector)
    {
        float sumSqDiff = 0f;
        for (int i = 0; i < n; i++)
        {
            float diff = selector(districts[i]) - mean;
            sumSqDiff += diff * diff;
        }
        return (float)Math.Sqrt(sumSqDiff / n);
    }

    private static float StdDevInverseDebt(DistrictState[] districts, int n, float mean)
    {
        float sumSqDiff = 0f;
        for (int i = 0; i < n; i++)
        {
            float inverseDebt = 100f - (districts[i].debt * 100f / 80f);
            float diff = inverseDebt - mean;
            sumSqDiff += diff * diff;
        }
        return (float)Math.Sqrt(sumSqDiff / n);
    }

    /// <summary>
    /// Phase 4.2 — Metro Population Pool and Distribution.
    /// Computes new residents based on city reputation, then distributes to districts
    /// based on attractiveness (happiness 40%, housing 40%, tax inverse 20%).
    /// Modifies district population in place.
    /// </summary>
    public static void DistributePopulation(
        DistrictState[] districts, float cityReputation, int numActivePlayers)
    {
        // ── INFLOW CALCULATION ──
        float newResidents;

        if (cityReputation > 70f)
            newResidents = (cityReputation - 70f) * SimulationConstants.K_POP_INFLOW_HIGH;
        else if (cityReputation >= 30f)
            newResidents = (cityReputation - 50f) * SimulationConstants.K_POP_INFLOW_NORMAL;
        else
            newResidents = (cityReputation - 30f) * SimulationConstants.K_POP_OUTFLOW;

        // ── DISTRIBUTION TO DISTRICTS ──
        float totalAttractiveness = 0f;
        float[] attractiveness = new float[numActivePlayers];

        for (int i = 0; i < numActivePlayers; i++)
        {
            attractiveness[i] = districts[i].happiness * 0.40f
                              + (districts[i].sliders.housing / 100.0f) * 0.40f
                              + (1.0f - districts[i].sliders.taxRate / 30.0f) * 0.20f;
            totalAttractiveness += attractiveness[i];
        }

        if (totalAttractiveness > 0f)
        {
            for (int i = 0; i < numActivePlayers; i++)
            {
                float share = attractiveness[i] / totalAttractiveness;
                districts[i].population += newResidents * share;
            }
        }
        else
        {
            for (int i = 0; i < numActivePlayers; i++)
            {
                districts[i].population += newResidents / numActivePlayers;
            }
        }
    }

    /// <summary>
    /// Phase 4.3 — Shared Infrastructure Quality.
    /// Grows from collective city contribution spending, decays without it.
    /// Caller passes the sum of all actualCityCost from Phase 1 (DistrictState
    /// doesn't store per-category costs; ScaledSpending does).
    /// Returns the new sharedInfraQuality value, clamped to 0-100.
    /// </summary>
    public static float UpdateSharedInfrastructure(
        float totalCitySpending, float currentSharedInfra)
    {
        float sharedInfraGrowth = totalCitySpending * SimulationConstants.K_SHARED_INFRA_GROWTH;
        float sharedInfraDecay = currentSharedInfra * SimulationConstants.K_SHARED_INFRA_DECAY;

        return Math.Min(Math.Max(
            currentSharedInfra + sharedInfraGrowth - sharedInfraDecay, 0f), 100f);
    }

    /// <summary>
    /// Phase 4.4 &amp; 4.5 — Federal Funding.
    /// Competitive grants (4.4): awards bonus revenue to districts meeting thresholds.
    /// Stabilization transfers (4.5): reduces debt for districts at debt >= 70.
    /// Modifies districts in place (revenue, grant streaks, debt, grantsEligible).
    /// </summary>
    public static void ResolveFederalFunding(DistrictState[] districts, int numActivePlayers)
    {
        for (int i = 0; i < numActivePlayers; i++)
        {
            // ── 4.5 — Stabilization Transfers (check first, sets grantsEligible) ──
            if (districts[i].debt >= 70f)
            {
                districts[i].debt -= SimulationConstants.K_STABILIZATION_RATE;
                districts[i].grantsEligible = false;
            }
            else
            {
                if (districts[i].debt < SimulationConstants.DEBT_CAP)
                    districts[i].grantsEligible = true;
            }

            // ── 4.4 — Competitive Grants ──
            float grantRevenue = 0f;

            if (districts[i].debt < SimulationConstants.DEBT_CAP && districts[i].grantsEligible)
            {
                // Green Infrastructure Grant — sustainability > 70
                if (districts[i].sustainability > 70f)
                {
                    float multiplier = Math.Max(0.30f,
                        1.0f - districts[i].greenGrantStreak * 0.15f);
                    grantRevenue += SimulationConstants.GRANT_BASE_GREEN * multiplier;
                    districts[i].greenGrantStreak += 1;
                }
                else
                {
                    districts[i].greenGrantStreak = 0;
                }

                // Federal Transit Grant — population > 300k
                if (districts[i].population > 300.0f)
                {
                    float multiplier = Math.Max(0.30f,
                        1.0f - districts[i].transitGrantStreak * 0.15f);
                    grantRevenue += SimulationConstants.GRANT_BASE_TRANSIT * multiplier;
                    districts[i].transitGrantStreak += 1;
                }
                else
                {
                    districts[i].transitGrantStreak = 0;
                }

                // Quality of Life Grant — happiness > 75
                if (districts[i].happiness > 75f)
                {
                    float multiplier = Math.Max(0.30f,
                        1.0f - districts[i].lifeGrantStreak * 0.15f);
                    grantRevenue += SimulationConstants.GRANT_BASE_LIFE * multiplier;
                    districts[i].lifeGrantStreak += 1;
                }
                else
                {
                    districts[i].lifeGrantStreak = 0;
                }

                // Development Grant — infrastructure > 80
                if (districts[i].infrastructure > 80f)
                {
                    float multiplier = Math.Max(0.30f,
                        1.0f - districts[i].devGrantStreak * 0.15f);
                    grantRevenue += SimulationConstants.GRANT_BASE_DEV * multiplier;
                    districts[i].devGrantStreak += 1;
                }
                else
                {
                    districts[i].devGrantStreak = 0;
                }
            }

            districts[i].revenue += grantRevenue;
        }
    }
}
