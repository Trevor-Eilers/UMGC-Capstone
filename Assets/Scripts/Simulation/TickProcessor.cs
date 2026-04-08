// Author: Malcolm Bramble

using System;

namespace Simulation
{
    /// <summary>
    /// Per-district tick resolution for distributed authority.
    /// Each client calls ResolveDistrictTick for their own district,
    /// reading other districts from a shared snapshot (last tick's final state).
    /// City-wide metrics are computed once from the snapshot — all clients
    /// produce identical results (deterministic).
    /// </summary>
    public static class TickProcessor
    {
        /// <summary>
        /// Compute city-wide metrics from the snapshot. Deterministic: all clients
        /// produce the same result from the same snapshot.
        /// Call this ONCE per tick before ResolveDistrictTick.
        /// </summary>
        public static CityMetrics ResolveCityMetrics(
            DistrictState[] snapshot, CityMetrics current, int numActivePlayers)
        {
            CityMetrics cm = current;

            // 4.1 — City Reputation
            cm.cityReputation = CityMetricsManager.ComputeCityReputation(
                snapshot, numActivePlayers);
            cm.cityReputation = Math.Min(Math.Max(cm.cityReputation, 0f), 100f);

            // 4.3 — Shared Infrastructure (from snapshot spending)
            float totalCityCost = ComputeTotalCityCost(snapshot, numActivePlayers);
            cm.sharedInfraQuality = CityMetricsManager.UpdateSharedInfrastructure(
                totalCityCost, cm.sharedInfraQuality);
            cm.sharedInfraQuality = Math.Min(Math.Max(cm.sharedInfraQuality, 0f), 100f);

            // Metro population pool for district-level distribution
            cm.metroPopulationPool = CityMetricsManager.ComputeMetroPopulationPool(
                cm.cityReputation);

            return cm;
        }

        /// <summary>
        /// Resolve a single tick for one district. Each client calls this for
        /// their own district using the shared snapshot and pre-computed city metrics.
        /// </summary>
        public static DistrictState ResolveDistrictTick(
            int districtIndex,
            DistrictState[] snapshot,
            CityMetrics cityMetrics,
            int numActivePlayers)
        {
            DistrictState d = snapshot[districtIndex];

            // ══════════════════════════════════════════
            // PHASE 1: Budget Resolution (purely local)
            // ══════════════════════════════════════════

            d.revenue = BudgetCalculator.ComputeRevenue(
                d.policyValues.taxRate, d.gdp, d.population);

            SpendingBreakdown spending = BudgetCalculator.ComputeSpendingDemand(
                d.policyValues, d.population);

            ScaledSpending scaledSpending = BudgetCalculator.ComputeDebtCapScaling(
                spending, d.revenue, d.debt);
            d.scaleFactor = scaledSpending.scaleFactor;
            d.totalSpending = scaledSpending.actualTotalSpending;

            float debt = d.debt;
            float reserve = d.reserve;
            BudgetCalculator.ComputeBudgetBalance(
                d.revenue, scaledSpending.actualTotalSpending,
                ref debt, ref reserve);
            d.debt = debt;
            d.reserve = reserve;

            // ══════════════════════════════════════════
            // PHASE 2: Local Effects (purely local)
            // ══════════════════════════════════════════

            d.gdp += LocalEffectCalculator.ComputeGdpDelta(d, scaledSpending);
            d.happiness = LocalEffectCalculator.ComputeHappiness(d, scaledSpending);
            d.infrastructure += LocalEffectCalculator.ComputeInfrastructureDelta(d, scaledSpending);
            d.sustainability += LocalEffectCalculator.ComputeSustainabilityDelta(d, scaledSpending);
            d.population -= LocalEffectCalculator.ComputeOutmigration(d);

            // ══════════════════════════════════════════
            // PHASE 3: Spillover (reads others from snapshot)
            // ══════════════════════════════════════════

            SpilloverResolver.ApplyGentrification(
                ref d, districtIndex, snapshot, numActivePlayers);
            SpilloverResolver.ApplyPollution(
                ref d, districtIndex, snapshot, numActivePlayers);
            SpilloverResolver.ApplyCommuting(
                ref d, districtIndex, snapshot, cityMetrics, numActivePlayers);

            // ══════════════════════════════════════════
            // PHASE 4: Per-district city effects
            // ══════════════════════════════════════════

            // 4.2 — Population Distribution (our share of metro inflow)
            d.population += CityMetricsManager.ComputePopulationDelta(
                districtIndex, snapshot, cityMetrics, numActivePlayers);

            // 4.4 & 4.5 — Federal Funding (grants + stabilization)
            CityMetricsManager.ResolveFederalFunding(ref d);

            // ══════════════════════════════════════════
            // PHASE 5: Clamp and Commit
            // ══════════════════════════════════════════

            d.gdp = Math.Min(Math.Max(d.gdp, 0f), 100f);
            d.happiness = Math.Min(Math.Max(d.happiness, 0f), 100f);
            d.population = Math.Min(Math.Max(d.population,
                SimulationConstants.MIN_POPULATION), SimulationConstants.MAX_POPULATION);
            d.infrastructure = Math.Min(Math.Max(d.infrastructure, 0f), 100f);
            d.sustainability = Math.Min(Math.Max(d.sustainability, 0f), 100f);
            d.debt = Math.Min(Math.Max(d.debt, 0f), 80f);
            d.reserve = Math.Min(Math.Max(d.reserve, 0f), SimulationConstants.RESERVE_CAP);

            if (d.debt >= SimulationConstants.DEBT_CAP)
                d.ticksAtDebtCap += 1;
            if (d.happiness < 20f)
                d.ticksBelowHappiness20 += 1;
            d.totalCitySpending += scaledSpending.actualCityCost;

            return d;
        }

        /// <summary>
        /// Compute total city contribution spending across all districts from snapshot.
        /// Deterministic — all clients compute the same value.
        /// </summary>
        private static float ComputeTotalCityCost(
            DistrictState[] snapshot, int numActivePlayers)
        {
            float total = 0f;
            for (int i = 0; i < numActivePlayers; i++)
            {
                SpendingBreakdown spending = BudgetCalculator.ComputeSpendingDemand(
                    snapshot[i].policyValues, snapshot[i].population);
                float revenue = BudgetCalculator.ComputeRevenue(
                    snapshot[i].policyValues.taxRate, snapshot[i].gdp, snapshot[i].population);
                ScaledSpending scaled = BudgetCalculator.ComputeDebtCapScaling(
                    spending, revenue, snapshot[i].debt);
                total += scaled.actualCityCost;
            }
            return total;
        }

        /// <summary>
        /// Test convenience: resolves a full tick for all districts simultaneously.
        /// Simulates all clients computing from the same snapshot.
        /// </summary>
        public static void ResolveFullTick(
            DistrictState[] districts, ref CityMetrics cityMetrics, int numActivePlayers)
        {
            var snapshot = (DistrictState[])districts.Clone();
            cityMetrics = ResolveCityMetrics(snapshot, cityMetrics, numActivePlayers);
            for (int i = 0; i < numActivePlayers; i++)
                districts[i] = ResolveDistrictTick(i, snapshot, cityMetrics, numActivePlayers);
        }
    }
}
