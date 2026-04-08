// Author: Malcolm Bramble

using NUnit.Framework;
using Simulation;

namespace Tests.EditMode
{
    [TestFixture]
    public class CityMetricsManagerTests
    {
        // ──────────────────────────────────────────────
        // Save/restore mutable constants
        // ──────────────────────────────────────────────

        private float saved_K_VARIANCE_PENALTY;
        private float saved_K_POP_INFLOW_HIGH, saved_K_POP_INFLOW_NORMAL, saved_K_POP_OUTFLOW;
        private float saved_K_SHARED_INFRA_GROWTH, saved_K_SHARED_INFRA_DECAY;
        private float saved_GRANT_BASE_GREEN, saved_GRANT_BASE_TRANSIT;
        private float saved_GRANT_BASE_LIFE, saved_GRANT_BASE_DEV;
        private float saved_K_STABILIZATION_RATE;
        private float saved_DEBT_CAP;

        [SetUp]
        public void SaveConstants()
        {
            saved_K_VARIANCE_PENALTY = SimulationConstants.K_VARIANCE_PENALTY;
            saved_K_POP_INFLOW_HIGH = SimulationConstants.K_POP_INFLOW_HIGH;
            saved_K_POP_INFLOW_NORMAL = SimulationConstants.K_POP_INFLOW_NORMAL;
            saved_K_POP_OUTFLOW = SimulationConstants.K_POP_OUTFLOW;
            saved_K_SHARED_INFRA_GROWTH = SimulationConstants.K_SHARED_INFRA_GROWTH;
            saved_K_SHARED_INFRA_DECAY = SimulationConstants.K_SHARED_INFRA_DECAY;
            saved_GRANT_BASE_GREEN = SimulationConstants.GRANT_BASE_GREEN;
            saved_GRANT_BASE_TRANSIT = SimulationConstants.GRANT_BASE_TRANSIT;
            saved_GRANT_BASE_LIFE = SimulationConstants.GRANT_BASE_LIFE;
            saved_GRANT_BASE_DEV = SimulationConstants.GRANT_BASE_DEV;
            saved_K_STABILIZATION_RATE = SimulationConstants.K_STABILIZATION_RATE;
            saved_DEBT_CAP = SimulationConstants.DEBT_CAP;
        }

        [TearDown]
        public void RestoreConstants()
        {
            SimulationConstants.K_VARIANCE_PENALTY = saved_K_VARIANCE_PENALTY;
            SimulationConstants.K_POP_INFLOW_HIGH = saved_K_POP_INFLOW_HIGH;
            SimulationConstants.K_POP_INFLOW_NORMAL = saved_K_POP_INFLOW_NORMAL;
            SimulationConstants.K_POP_OUTFLOW = saved_K_POP_OUTFLOW;
            SimulationConstants.K_SHARED_INFRA_GROWTH = saved_K_SHARED_INFRA_GROWTH;
            SimulationConstants.K_SHARED_INFRA_DECAY = saved_K_SHARED_INFRA_DECAY;
            SimulationConstants.GRANT_BASE_GREEN = saved_GRANT_BASE_GREEN;
            SimulationConstants.GRANT_BASE_TRANSIT = saved_GRANT_BASE_TRANSIT;
            SimulationConstants.GRANT_BASE_LIFE = saved_GRANT_BASE_LIFE;
            SimulationConstants.GRANT_BASE_DEV = saved_GRANT_BASE_DEV;
            SimulationConstants.K_STABILIZATION_RATE = saved_K_STABILIZATION_RATE;
            SimulationConstants.DEBT_CAP = saved_DEBT_CAP;
        }

        // ──────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────

        private static DistrictState[] MakeUniformDistricts(
            int count, float gdp = 50f, float happiness = 55f,
            float infrastructure = 50f, float sustainability = 55f,
            float debt = 15f, float population = 150f,
            float housing = 50f, float taxRate = 15f)
        {
            var districts = new DistrictState[4];
            for (int i = 0; i < 4; i++)
            {
                districts[i] = DistrictState.Default(i);
                districts[i].gdp = gdp;
                districts[i].happiness = happiness;
                districts[i].infrastructure = infrastructure;
                districts[i].sustainability = sustainability;
                districts[i].debt = debt;
                districts[i].population = population;
                districts[i].policyValues.housing = housing;
                districts[i].policyValues.taxRate = taxRate;
            }
            return districts;
        }

        // ══════════════════════════════════════════════
        // CITY REPUTATION — VARIANCE PENALTY
        // ══════════════════════════════════════════════

        [Test]
        public void CityReputation_EqualDistricts_NoVariancePenalty()
        {
            SimulationConstants.K_VARIANCE_PENALTY = 1.0f;

            var districts = MakeUniformDistricts(2);
            float rep = CityMetricsManager.ComputeCityReputation(districts, 2);

            float inverseDebt = 100f - (15f * 100f / 80f);
            float expected = 55f * 0.25f + 55f * 0.25f + 50f * 0.20f
                             + 50f * 0.15f + inverseDebt * 0.15f;

            Assert.AreEqual(expected, rep, 0.01f,
                "With identical districts, reputation should equal the weighted average");
        }

        [Test]
        public void CityReputation_UnequalDistricts_VariancePenaltyApplied()
        {
            SimulationConstants.K_VARIANCE_PENALTY = 1.0f;

            var districts = MakeUniformDistricts(2);
            float repEqual = CityMetricsManager.ComputeCityReputation(districts, 2);

            districts[0].gdp = 80f;
            districts[1].gdp = 20f;
            float repUnequal = CityMetricsManager.ComputeCityReputation(districts, 2);

            Assert.Less(repUnequal, repEqual,
                "Unequal districts should produce lower reputation due to variance penalty");
        }

        [Test]
        public void CityReputation_MoreInequality_LargerPenalty()
        {
            SimulationConstants.K_VARIANCE_PENALTY = 1.0f;

            var districtsMild = MakeUniformDistricts(2);
            districtsMild[0].gdp = 60f;
            districtsMild[1].gdp = 40f;
            float repMild = CityMetricsManager.ComputeCityReputation(districtsMild, 2);

            var districtsSevere = MakeUniformDistricts(2);
            districtsSevere[0].gdp = 90f;
            districtsSevere[1].gdp = 10f;
            float repSevere = CityMetricsManager.ComputeCityReputation(districtsSevere, 2);

            Assert.Less(repSevere, repMild,
                "Greater inequality should produce larger variance penalty");
        }

        [Test]
        public void CityReputation_ClampedTo0_100()
        {
            SimulationConstants.K_VARIANCE_PENALTY = 100f;

            var districts = MakeUniformDistricts(2);
            districts[0].gdp = 100f;
            districts[1].gdp = 0f;
            float rep = CityMetricsManager.ComputeCityReputation(districts, 2);

            Assert.GreaterOrEqual(rep, 0f, "Reputation must not go below 0");
            Assert.LessOrEqual(rep, 100f, "Reputation must not exceed 100");
        }

        // ══════════════════════════════════════════════
        // POPULATION DISTRIBUTION — PROPORTIONAL TO ATTRACTIVENESS
        // ══════════════════════════════════════════════

        [Test]
        public void DistributePopulation_ProportionalToAttractiveness()
        {
            SimulationConstants.K_POP_INFLOW_HIGH = 1.0f;

            var districts = MakeUniformDistricts(2);
            districts[0].happiness = 80f;
            districts[0].policyValues.housing = 80f;
            districts[0].policyValues.taxRate = 10f;
            districts[1].happiness = 20f;
            districts[1].policyValues.housing = 20f;
            districts[1].policyValues.taxRate = 25f;

            float pop0Before = districts[0].population;
            float pop1Before = districts[1].population;

            var snapshot = (DistrictState[])districts.Clone();
            var cm = CityMetrics.Default();
            cm.cityReputation = 80f;
            cm.metroPopulationPool = CityMetricsManager.ComputeMetroPopulationPool(80f);

            float delta0 = CityMetricsManager.ComputePopulationDelta(0, snapshot, cm, 2);
            float delta1 = CityMetricsManager.ComputePopulationDelta(1, snapshot, cm, 2);

            Assert.Greater(delta0, delta1,
                "More attractive district should receive more population");
            Assert.Greater(delta0, 0f, "Attractive district should gain population");
        }

        [Test]
        public void DistributePopulation_EqualAttractiveness_EqualDistribution()
        {
            SimulationConstants.K_POP_INFLOW_HIGH = 1.0f;

            var districts = MakeUniformDistricts(2);

            var snapshot = (DistrictState[])districts.Clone();
            var cm = CityMetrics.Default();
            cm.cityReputation = 80f;
            cm.metroPopulationPool = CityMetricsManager.ComputeMetroPopulationPool(80f);

            float delta0 = CityMetricsManager.ComputePopulationDelta(0, snapshot, cm, 2);
            float delta1 = CityMetricsManager.ComputePopulationDelta(1, snapshot, cm, 2);

            Assert.AreEqual(delta0, delta1, 0.01f,
                "Identical districts should receive equal population");
        }

        [Test]
        public void DistributePopulation_Outflow_DistributedProportionally()
        {
            SimulationConstants.K_POP_OUTFLOW = 1.0f;

            var districts = MakeUniformDistricts(2);
            districts[0].happiness = 80f;
            districts[1].happiness = 20f;

            var snapshot = (DistrictState[])districts.Clone();
            var cm = CityMetrics.Default();
            cm.cityReputation = 20f;
            cm.metroPopulationPool = CityMetricsManager.ComputeMetroPopulationPool(20f);

            float delta0 = CityMetricsManager.ComputePopulationDelta(0, snapshot, cm, 2);
            float delta1 = CityMetricsManager.ComputePopulationDelta(1, snapshot, cm, 2);

            // Both deltas negative (outflow). More attractive gets larger magnitude share.
            Assert.Less(delta0, delta1,
                "More attractive district gets larger share of outflow (proportional distribution)");
        }

        // ══════════════════════════════════════════════
        // SHARED INFRASTRUCTURE
        // ══════════════════════════════════════════════

        [Test]
        public void SharedInfra_GrowthFromSpending()
        {
            SimulationConstants.K_SHARED_INFRA_GROWTH = 0.1f;
            SimulationConstants.K_SHARED_INFRA_DECAY = 0f;

            float result = CityMetricsManager.UpdateSharedInfrastructure(100f, 50f);
            Assert.AreEqual(60f, result, 0.01f);
        }

        [Test]
        public void SharedInfra_DecayWithoutSpending()
        {
            SimulationConstants.K_SHARED_INFRA_GROWTH = 0f;
            SimulationConstants.K_SHARED_INFRA_DECAY = 0.1f;

            float result = CityMetricsManager.UpdateSharedInfrastructure(0f, 50f);
            Assert.AreEqual(45f, result, 0.01f);
        }

        [Test]
        public void SharedInfra_ClampedTo0_100()
        {
            SimulationConstants.K_SHARED_INFRA_GROWTH = 10f;
            SimulationConstants.K_SHARED_INFRA_DECAY = 0f;

            float result = CityMetricsManager.UpdateSharedInfrastructure(100f, 95f);
            Assert.LessOrEqual(result, 100f);

            SimulationConstants.K_SHARED_INFRA_GROWTH = 0f;
            SimulationConstants.K_SHARED_INFRA_DECAY = 10f;
            float result2 = CityMetricsManager.UpdateSharedInfrastructure(0f, 5f);
            Assert.GreaterOrEqual(result2, 0f);
        }

        // ══════════════════════════════════════════════
        // FEDERAL FUNDING — GRANT DIMINISHING RETURNS
        // ══════════════════════════════════════════════

        [Test]
        public void Grants_DiminishBy15PercentPerConsecutiveTick()
        {
            SimulationConstants.GRANT_BASE_GREEN = 100f;
            SimulationConstants.GRANT_BASE_TRANSIT = 0f;
            SimulationConstants.GRANT_BASE_LIFE = 0f;
            SimulationConstants.GRANT_BASE_DEV = 0f;
            SimulationConstants.K_STABILIZATION_RATE = 0f;

            var districts = MakeUniformDistricts(1, sustainability: 80f, debt: 10f);

            // Tick 1: streak=0, multiplier = 1.00
            float revBefore = districts[0].revenue;
            CityMetricsManager.ResolveFederalFunding(ref districts[0]);
            float grant1 = districts[0].revenue - revBefore;
            Assert.AreEqual(100f, grant1, 0.01f, "Tick 1: 100% of base");

            // Tick 2: streak=1, multiplier = 0.85
            revBefore = districts[0].revenue;
            CityMetricsManager.ResolveFederalFunding(ref districts[0]);
            float grant2 = districts[0].revenue - revBefore;
            Assert.AreEqual(85f, grant2, 0.01f, "Tick 2: 85% of base");

            // Tick 3: streak=2, multiplier = 0.70
            revBefore = districts[0].revenue;
            CityMetricsManager.ResolveFederalFunding(ref districts[0]);
            float grant3 = districts[0].revenue - revBefore;
            Assert.AreEqual(70f, grant3, 0.01f, "Tick 3: 70% of base");
        }

        [Test]
        public void Grants_FloorAt30Percent()
        {
            SimulationConstants.GRANT_BASE_GREEN = 100f;
            SimulationConstants.GRANT_BASE_TRANSIT = 0f;
            SimulationConstants.GRANT_BASE_LIFE = 0f;
            SimulationConstants.GRANT_BASE_DEV = 0f;
            SimulationConstants.K_STABILIZATION_RATE = 0f;

            var districts = MakeUniformDistricts(1, sustainability: 80f, debt: 10f);
            districts[0].greenGrantStreak = 10;

            float revBefore = districts[0].revenue;
            CityMetricsManager.ResolveFederalFunding(ref districts[0]);
            float grant = districts[0].revenue - revBefore;

            Assert.AreEqual(30f, grant, 0.01f, "Grant should floor at 30% of base");
        }

        [Test]
        public void Grants_StreakResets_WhenThresholdNotMet()
        {
            SimulationConstants.GRANT_BASE_GREEN = 100f;
            SimulationConstants.GRANT_BASE_TRANSIT = 0f;
            SimulationConstants.GRANT_BASE_LIFE = 0f;
            SimulationConstants.GRANT_BASE_DEV = 0f;
            SimulationConstants.K_STABILIZATION_RATE = 0f;

            var districts = MakeUniformDistricts(1, sustainability: 80f, debt: 10f);

            CityMetricsManager.ResolveFederalFunding(ref districts[0]);
            CityMetricsManager.ResolveFederalFunding(ref districts[0]);
            Assert.AreEqual(2, districts[0].greenGrantStreak);

            districts[0].sustainability = 60f;
            CityMetricsManager.ResolveFederalFunding(ref districts[0]);
            Assert.AreEqual(0, districts[0].greenGrantStreak, "Streak should reset to 0");
        }

        [Test]
        public void Grants_NotAwarded_AtDebtCap()
        {
            SimulationConstants.GRANT_BASE_GREEN = 100f;
            SimulationConstants.K_STABILIZATION_RATE = 0f;

            var districts = MakeUniformDistricts(1, sustainability: 80f, debt: 60f);

            float revBefore = districts[0].revenue;
            CityMetricsManager.ResolveFederalFunding(ref districts[0]);
            float grant = districts[0].revenue - revBefore;

            Assert.AreEqual(0f, grant, 0.01f, "No grants when debt >= DEBT_CAP");
        }

        [Test]
        public void Grants_NotAwarded_WhenReceivingStabilization()
        {
            SimulationConstants.GRANT_BASE_GREEN = 100f;
            SimulationConstants.K_STABILIZATION_RATE = 1.0f;

            var districts = MakeUniformDistricts(1, sustainability: 80f, debt: 72f);

            float revBefore = districts[0].revenue;
            CityMetricsManager.ResolveFederalFunding(ref districts[0]);
            float grant = districts[0].revenue - revBefore;

            Assert.AreEqual(0f, grant, 0.01f,
                "No grants while receiving stabilization transfers");
            Assert.AreEqual(71f, districts[0].debt, 0.01f,
                "Stabilization should reduce debt");
        }

        [Test]
        public void Stabilization_ReducesDebt_AtDebt70Plus()
        {
            SimulationConstants.K_STABILIZATION_RATE = 2.0f;

            var districts = MakeUniformDistricts(1, debt: 75f);

            CityMetricsManager.ResolveFederalFunding(ref districts[0]);

            Assert.AreEqual(73f, districts[0].debt, 0.01f,
                "Debt should decrease by K_STABILIZATION_RATE");
            Assert.IsFalse(districts[0].grantsEligible,
                "Grants should be disabled during stabilization");
        }

        [Test]
        public void Stabilization_ReenablesGrants_WhenDebtDropsBelowCap()
        {
            SimulationConstants.K_STABILIZATION_RATE = 0f;

            var districts = MakeUniformDistricts(1, debt: 50f);
            districts[0].grantsEligible = false;

            CityMetricsManager.ResolveFederalFunding(ref districts[0]);

            Assert.IsTrue(districts[0].grantsEligible,
                "Grants should re-enable once debt < DEBT_CAP");
        }
    }
}
