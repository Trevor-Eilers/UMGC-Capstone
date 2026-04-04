// Author: Malcolm Bramble

using System;

public static class SpilloverResolver
{
    /// <summary>
    /// Phase 3.1 — Gentrification.
    /// Triggers when GDP differential between adjacent districts exceeds GENTRIFY_THRESHOLD (8).
    /// Wealthy district gains GDP, loses happiness. Poor district loses happiness and population.
    /// Only processes pairs where both districts are active (index &lt; numActivePlayers).
    /// </summary>
    public static void ResolveGentrification(DistrictState[] districts, int numActivePlayers)
    {
        for (int i = 0; i < AdjacencyMap.AllPairs.Length; i++)
        {
            var pair = AdjacencyMap.AllPairs[i];
            if (pair.indexA >= numActivePlayers || pair.indexB >= numActivePlayers)
                continue;

            float gdpDiff = districts[pair.indexA].gdp - districts[pair.indexB].gdp;

            if (Math.Abs(gdpDiff) > SimulationConstants.GENTRIFY_THRESHOLD)
            {
                int wealthyIdx = gdpDiff > 0 ? pair.indexA : pair.indexB;
                int poorIdx = gdpDiff > 0 ? pair.indexB : pair.indexA;

                float magnitude = (Math.Abs(gdpDiff) - SimulationConstants.GENTRIFY_THRESHOLD)
                                  * pair.weight;

                // Poor district: displacement stress and population loss
                districts[poorIdx].happiness -= magnitude * SimulationConstants.K_GENTRIFY_HAPPY;
                districts[poorIdx].population -= magnitude * SimulationConstants.K_GENTRIFY_POP;

                // Wealthy district: economic expansion but social friction
                districts[wealthyIdx].gdp += magnitude * SimulationConstants.K_GENTRIFY_GDP_GAIN;
                districts[wealthyIdx].happiness -= magnitude
                    * SimulationConstants.K_GENTRIFY_WEALTHY_HAPPY;
            }
        }
    }

    /// <summary>
    /// Phase 3.2 — Pollution Drift.
    /// Triggers when a district has environment slider below POLLUTE_ENV_THRESHOLD (30)
    /// AND GDP above POLLUTE_GDP_THRESHOLD (40). Both conditions required.
    /// Polluter damages neighbors' sustainability and happiness; also takes self-damage.
    /// Only processes active districts (index &lt; numActivePlayers).
    /// </summary>
    public static void ResolvePollution(DistrictState[] districts, int numActivePlayers)
    {
        for (int d = 0; d < numActivePlayers; d++)
        {
            if (districts[d].sliders.environment >= SimulationConstants.POLLUTE_ENV_THRESHOLD
                || districts[d].gdp <= SimulationConstants.POLLUTE_GDP_THRESHOLD)
                continue;

            // Pollution output — additive formula
            float envShortfall = Math.Max(0f,
                SimulationConstants.POLLUTE_ENV_THRESHOLD - districts[d].sliders.environment);
            float gdpExcess = Math.Max(0f,
                districts[d].gdp - SimulationConstants.POLLUTE_GDP_THRESHOLD);
            float pollutionOutput = (envShortfall + gdpExcess)
                                    * SimulationConstants.K_POLLUTION_GENERATE;

            // Damage all neighbors
            int[] neighbors = AdjacencyMap.GetNeighbors(d);
            for (int n = 0; n < neighbors.Length; n++)
            {
                int neighborIdx = neighbors[n];
                if (neighborIdx >= numActivePlayers)
                    continue;

                float weight = AdjacencyMap.GetWeight(d, neighborIdx);
                districts[neighborIdx].sustainability -=
                    pollutionOutput * SimulationConstants.K_POLLUTION_SUSTAIN * weight;
                districts[neighborIdx].happiness -=
                    pollutionOutput * SimulationConstants.K_POLLUTION_HAPPY * weight;
            }

            // Self-damage (lower than neighbor damage)
            districts[d].sustainability -=
                pollutionOutput * SimulationConstants.K_POLLUTION_SELF_SUSTAIN;
            districts[d].happiness -=
                pollutionOutput * SimulationConstants.K_POLLUTION_SELF_HAPPY;
        }
    }

    /// <summary>
    /// Phase 3.3 — Commuter Flows.
    /// Triggers when GDP differential exceeds COMMUTE_GDP_THRESHOLD (5)
    /// AND sharedInfraQuality exceeds COMMUTE_INFRA_THRESHOLD (25). Both required.
    /// Work district gains GDP, loses happiness. Home district loses GDP, gains happiness.
    /// Only processes pairs where both districts are active.
    /// </summary>
    public static void ResolveCommuting(
        DistrictState[] districts, CityMetrics cityMetrics, int numActivePlayers)
    {
        if (cityMetrics.sharedInfraQuality <= SimulationConstants.COMMUTE_INFRA_THRESHOLD)
            return;

        float infraFactor = cityMetrics.sharedInfraQuality / 100.0f;

        for (int i = 0; i < AdjacencyMap.AllPairs.Length; i++)
        {
            var pair = AdjacencyMap.AllPairs[i];
            if (pair.indexA >= numActivePlayers || pair.indexB >= numActivePlayers)
                continue;

            float gdpDiff = districts[pair.indexA].gdp - districts[pair.indexB].gdp;

            if (Math.Abs(gdpDiff) > SimulationConstants.COMMUTE_GDP_THRESHOLD)
            {
                int workIdx = gdpDiff > 0 ? pair.indexA : pair.indexB;
                int homeIdx = gdpDiff > 0 ? pair.indexB : pair.indexA;

                float magnitude = (Math.Abs(gdpDiff) - SimulationConstants.COMMUTE_GDP_THRESHOLD)
                                  * pair.weight;
                float commuters = magnitude * infraFactor * SimulationConstants.K_COMMUTE_VOLUME;

                // Work district: GDP gain + congestion
                districts[workIdx].gdp += commuters * SimulationConstants.K_COMMUTE_GDP_GAIN;
                districts[workIdx].happiness -= commuters * SimulationConstants.K_COMMUTE_CONGESTION;

                // Home district: GDP drain + employed happiness
                districts[homeIdx].gdp -= commuters * SimulationConstants.K_COMMUTE_GDP_DRAIN;
                districts[homeIdx].happiness += commuters * SimulationConstants.K_COMMUTE_HOME_HAPPY;
            }
        }
    }
}
