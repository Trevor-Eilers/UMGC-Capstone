// Authors: Malcolm Bramble, Trevor Eilers


using Unity.Netcode;
using UnityEngine.UIElements;

namespace Simulation
{
    [System.Serializable]
    public struct DistrictState : INetworkSerializable
    {
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

        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            policyValues.NetworkSerialize(serializer);
            serializer.SerializeValue(ref population);
            serializer.SerializeValue(ref happiness);
            serializer.SerializeValue(ref sustainability);
            serializer.SerializeValue(ref infrastructure);
            serializer.SerializeValue(ref gdp);
            serializer.SerializeValue(ref debt);
            serializer.SerializeValue(ref reserve);
            serializer.SerializeValue(ref revenue);
            serializer.SerializeValue(ref totalSpending);
            serializer.SerializeValue(ref scaleFactor);
            serializer.SerializeValue(ref greenGrantStreak);
            serializer.SerializeValue(ref transitGrantStreak);
            serializer.SerializeValue(ref lifeGrantStreak);
            serializer.SerializeValue(ref devGrantStreak);
            serializer.SerializeValue(ref grantsEligible);
            serializer.SerializeValue(ref ticksAtDebtCap);
            serializer.SerializeValue(ref ticksBelowHappiness20);
            serializer.SerializeValue(ref totalCitySpending);
        }
        
        public static DistrictState Default()
        {
            return new DistrictState
            {
                policyValues = PolicyValues.Default(),
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
}
