// Author: Malcolm Bramble

[System.Serializable]
public struct DistrictState
{
    public int playerId;                // 0-3
    public PolicySliders sliders;

    // ── Metrics ──
    public float gdp;                   // 0-100
    public float happiness;             // 0-100
    public float population;            // absolute count in thousands
    public float infrastructure;        // 0-100
    public float sustainability;        // 0-100

    // ── Fiscal ──
    public float debt;                  // 0-80, cap at 60
    public float reserve;              // 0-RESERVE_CAP
    public float revenue;              // computed each tick
    public float totalSpending;        // computed each tick
    public float scaleFactor;          // 0.0-1.0, 1.0 unless at debt cap

    // ── Grant Streaks ──
    public int greenGrantStreak;
    public int transitGrantStreak;
    public int lifeGrantStreak;
    public int devGrantStreak;
    public bool grantsEligible;

    // ── Cumulative Tracking (scoring) ──
    public int ticksAtDebtCap;
    public int ticksBelowHappiness20;
    public float totalCitySpending;

    public static DistrictState Default(int playerId)
    {
        return new DistrictState
        {
            playerId = playerId,
            sliders = PolicySliders.Default(),
            gdp = SimulationConstants.GDP_START,
            happiness = SimulationConstants.HAPPINESS_START,
            population = SimulationConstants.POPULATION_START,
            infrastructure = SimulationConstants.INFRASTRUCTURE_START,
            sustainability = SimulationConstants.SUSTAINABILITY_START,
            debt = SimulationConstants.DEBT_START,
            reserve = SimulationConstants.RESERVE_START,
            revenue = 0f,
            totalSpending = 0f,
            scaleFactor = 1.0f,
            greenGrantStreak = 0,
            transitGrantStreak = 0,
            lifeGrantStreak = 0,
            devGrantStreak = 0,
            grantsEligible = true,
            ticksAtDebtCap = 0,
            ticksBelowHappiness20 = 0,
            totalCitySpending = 0f
        };
    }
}
