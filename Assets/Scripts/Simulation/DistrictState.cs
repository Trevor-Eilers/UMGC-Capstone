// Author: Malcolm Bramble



namespace Simulation
{
    [System.Serializable]
    public struct DistrictState
    {
        public int playerId; // 0-3
        
        public PolicyValues policyValues;

        public float population;
        public float happiness;
        public float sustainability;
        public float infrastructure;
        
        // ── Fiscal ──
        public float gdp;
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
                policyValues = PolicyValues.Default(),
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
}
