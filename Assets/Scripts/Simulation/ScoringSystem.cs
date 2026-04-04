// Author: Malcolm Bramble

using System;

public struct FinalScore
{
    public float neighborhoodScore;
    public float cityContribScore;
    public float finalScore;
}

public static class ScoringSystem
{
    /// <summary>
    /// Computes the final score for a single district at end of game (tick 576).
    /// Neighborhood score (60%) + City contribution score (40%).
    /// </summary>
    public static FinalScore ComputeFinalScore(
        DistrictState district, CityMetrics cityMetrics,
        DistrictState[] allDistricts, int numActivePlayers)
    {
        // ── NEIGHBORHOOD SCORE (60%) ──

        float inverseDebt = 100.0f - (district.debt * 100.0f / 80.0f);

        // Population normalization: population field is in thousands.
        // POP_MAX_SCORE is calibrated to the thousands scale.
        float popScore = Math.Min(district.population / SimulationConstants.POP_MAX_SCORE, 1.0f)
                       * 100.0f;

        float neighborhoodScore = district.gdp * 0.225f
                                + district.happiness * 0.225f
                                + popScore * 0.15f
                                + district.infrastructure * 0.15f
                                + district.sustainability * 0.15f
                                + inverseDebt * 0.10f;

        // ── CITY CONTRIBUTION SCORE (40%) ──

        // City Reputation (50% of city score)
        float cityReputationScore = cityMetrics.cityReputation;

        // Shared Infrastructure Contribution — relative measure
        float totalAllCitySpending = 0f;
        for (int i = 0; i < numActivePlayers; i++)
            totalAllCitySpending += allDistricts[i].totalCitySpending;

        float sharedInfraContrib;
        if (totalAllCitySpending > 0f)
            sharedInfraContrib = (district.totalCitySpending / totalAllCitySpending) * 100.0f;
        else
            sharedInfraContrib = 100.0f / numActivePlayers;

        // Crisis Avoidance
        int crisisTicks = district.ticksAtDebtCap + district.ticksBelowHappiness20;
        float crisisAvoidance = Math.Max(0f,
            100f - crisisTicks * SimulationConstants.K_CRISIS_PENALTY);

        float cityContribScore = cityReputationScore * 0.50f
                               + sharedInfraContrib * 0.25f
                               + crisisAvoidance * 0.25f;

        // ── FINAL SCORE ──
        float finalScore = neighborhoodScore * 0.60f + cityContribScore * 0.40f;

        return new FinalScore
        {
            neighborhoodScore = neighborhoodScore,
            cityContribScore = cityContribScore,
            finalScore = finalScore
        };
    }
}
