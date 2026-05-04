// Author: Malcolm Bramble

using System;
using Simulation;

/// <summary>
/// Per-district spillover resolution for distributed authority.
/// Each method computes effects ON a single district (districtIndex)
/// by reading other districts from the snapshot and returns an updated
/// DistrictState.
/// </summary>
public static class SpilloverResolver
{
    /// <summary>
    /// Phase 3.1 — Gentrification effects ON districtIndex.
    /// For each pair containing this district, if GDP gap exceeds threshold,
    /// applies wealthy-side or poor-side effects. Returns the updated district.
    /// </summary>
    public static DistrictState ApplyGentrification(
        DistrictState d, int districtIndex,
        DistrictState[] snapshot, int numActivePlayers)
    {
        for (int i = 0; i < AdjacencyMap.AllPairs.Length; i++)
        {
            var pair = AdjacencyMap.AllPairs[i];
            if (pair.indexA >= numActivePlayers || pair.indexB >= numActivePlayers)
                continue;
            if (pair.indexA != districtIndex && pair.indexB != districtIndex)
                continue;

            float gdpDiff = snapshot[pair.indexA].gdp - snapshot[pair.indexB].gdp;

            if (Math.Abs(gdpDiff) > SimulationConstants.GENTRIFY_THRESHOLD)
            {
                float magnitude = (Math.Abs(gdpDiff) - SimulationConstants.GENTRIFY_THRESHOLD)
                                  * pair.weight;

                bool weAreWealthy = (gdpDiff > 0 && pair.indexA == districtIndex)
                                 || (gdpDiff < 0 && pair.indexB == districtIndex);

                if (weAreWealthy)
                {
                    // DR cap matches LocalEffectCalculator.cs:104 convention so
                    // spillover GDP can't push a wealthy district past 100 when
                    // a peer crashes (uncapped, three pairs of +5/tick saturated
                    // healthy districts in ~6 ticks).
                    float gentryDrFactor = Math.Max(0f, (100f - d.gdp) / 100f);
                    d.gdp += magnitude * SimulationConstants.K_GENTRIFY_GDP_GAIN * gentryDrFactor;
                    d.happiness -= magnitude * SimulationConstants.K_GENTRIFY_WEALTHY_HAPPY;
                }
                else
                {
                    d.happiness -= magnitude * SimulationConstants.K_GENTRIFY_HAPPY;
                    d.population -= magnitude * SimulationConstants.K_GENTRIFY_POP;
                }
            }
        }
        return d;
    }

    /// <summary>
    /// Phase 3.2 — Pollution effects ON districtIndex.
    /// Checks all districts in the snapshot for pollution sources.
    /// Applies neighbor damage or self-damage as appropriate and returns
    /// the updated district.
    /// </summary>
    public static DistrictState ApplyPollution(
        DistrictState d, int districtIndex,
        DistrictState[] snapshot, int numActivePlayers)
    {
        for (int src = 0; src < numActivePlayers; src++)
        {
            float srcEnv = snapshot[src].policyValues.environment;
            float srcGdp = snapshot[src].gdp;

            // Path 1: low-environment pollution (existing model — unregulated industry).
            float lowEnvOutput = 0f;
            if (srcEnv < SimulationConstants.POLLUTE_ENV_THRESHOLD
                && srcGdp > SimulationConstants.POLLUTE_GDP_THRESHOLD)
            {
                float envShortfall = SimulationConstants.POLLUTE_ENV_THRESHOLD - srcEnv;
                float gdpExcess = srcGdp - SimulationConstants.POLLUTE_GDP_THRESHOLD;
                lowEnvOutput = (envShortfall + gdpExcess)
                               * SimulationConstants.K_POLLUTION_GENERATE;
            }

            // Path 2: high-GDP pollution (sheer scale produces emissions that env
            // spending mitigates but cannot fully shield at extreme GDP).
            float highGdpOutput = 0f;
            if (srcGdp > SimulationConstants.POLLUTE_GDP_HIGH_THRESHOLD)
            {
                float gdpExcess = srcGdp - SimulationConstants.POLLUTE_GDP_HIGH_THRESHOLD;
                float envOffset = Math.Max(0f, srcEnv - SimulationConstants.POLLUTE_ENV_THRESHOLD)
                                  * SimulationConstants.K_POLLUTE_ENV_OFFSET;
                highGdpOutput = Math.Max(0f, gdpExcess - envOffset)
                                * SimulationConstants.K_POLLUTION_GENERATE;
            }

            float pollutionOutput = Math.Max(lowEnvOutput, highGdpOutput);
            if (pollutionOutput <= 0f) continue;

            d.pollution += pollutionOutput;

            if (src == districtIndex)
            {
                // Self-pollution (lower than neighbor damage)
                d.sustainability -= pollutionOutput * SimulationConstants.K_POLLUTION_SELF_SUSTAIN;
                d.happiness -= pollutionOutput * SimulationConstants.K_POLLUTION_SELF_HAPPY;
            }
            else
            {
                float weight = AdjacencyMap.GetWeight(src, districtIndex);
                if (weight > 0f)
                {
                    d.sustainability -= pollutionOutput
                        * SimulationConstants.K_POLLUTION_SUSTAIN * weight;
                    d.happiness -= pollutionOutput
                        * SimulationConstants.K_POLLUTION_HAPPY * weight;
                }
            }
        }
        return d;
    }

    /// <summary>
    /// Phase 3.3 — Commuting effects ON districtIndex.
    /// For each pair containing this district, if GDP gap and shared infra
    /// thresholds are met, applies work-side or home-side effects. Returns
    /// the updated district.
    /// </summary>
    public static DistrictState ApplyCommuting(
        DistrictState d, int districtIndex,
        DistrictState[] snapshot, CityMetrics cityMetrics,
        int numActivePlayers)
    {
        if (cityMetrics.sharedInfraQuality <= SimulationConstants.COMMUTE_INFRA_THRESHOLD)
            return d;

        float infraFactor = cityMetrics.sharedInfraQuality / 100.0f;

        for (int i = 0; i < AdjacencyMap.AllPairs.Length; i++)
        {
            var pair = AdjacencyMap.AllPairs[i];
            if (pair.indexA >= numActivePlayers || pair.indexB >= numActivePlayers)
                continue;
            if (pair.indexA != districtIndex && pair.indexB != districtIndex)
                continue;

            float gdpDiff = snapshot[pair.indexA].gdp - snapshot[pair.indexB].gdp;

            if (Math.Abs(gdpDiff) > SimulationConstants.COMMUTE_GDP_THRESHOLD)
            {
                float magnitude = (Math.Abs(gdpDiff) - SimulationConstants.COMMUTE_GDP_THRESHOLD)
                                  * pair.weight;
                float commuters = magnitude * infraFactor * SimulationConstants.K_COMMUTE_VOLUME;

                bool weAreWork = (gdpDiff > 0 && pair.indexA == districtIndex)
                              || (gdpDiff < 0 && pair.indexB == districtIndex);

                if (weAreWork)
                {
                    float commuteDrFactor = Math.Max(0f, (100f - d.gdp) / 100f);
                    d.gdp += commuters * SimulationConstants.K_COMMUTE_GDP_GAIN * commuteDrFactor;
                    d.happiness -= commuters * SimulationConstants.K_COMMUTE_CONGESTION;
                }
                else
                {
                    d.gdp -= commuters * SimulationConstants.K_COMMUTE_GDP_DRAIN;
                    d.happiness += commuters * SimulationConstants.K_COMMUTE_HOME_HAPPY;
                }
            }
        }
        return d;
    }
}
