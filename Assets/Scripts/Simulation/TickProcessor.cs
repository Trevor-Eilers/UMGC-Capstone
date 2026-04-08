// Author: Malcolm Bramble

using System;

namespace Simulation
{
    /// <summary>
    /// Main simulation entry point. Resolves one tick across all five phases.
    /// Stateless pure function: receives GameState, returns updated GameState.
    /// </summary>
    public static class TickProcessor
    {
        /// <summary>
        /// Resolve a single tick. Executes all five phases in order:
        /// Phase 1: Budget Resolution
        /// Phase 2: Local Effects
        /// Phase 3: Spillover
        /// Phase 4: City Metrics
        /// Phase 5: Clamp and Commit
        /// </summary>
        public static GameState ResolveTick(GameState gameState)
        {
            int n = gameState.districts.Length;

            // Store per-district scaled spending for use across phases
            ScaledSpending[] scaledSpending = new ScaledSpending[n];
            float totalActualCityCost = 0f;

            // ══════════════════════════════════════════
            // PHASE 1: Budget Resolution
            // ══════════════════════════════════════════

            for (int i = 0; i < n; i++)
            {
                DistrictState d = gameState.districts[i].state.Value;

                // Step 1.1 — Revenue
                d.revenue = BudgetCalculator.ComputeRevenue(
                    d.policyValues.taxRate, d.gdp, d.population);

                // Step 1.2 — Spending Demand
                SpendingBreakdown spending = BudgetCalculator.ComputeSpendingDemand(
                    d.policyValues, d.population);

                // Step 1.3 — Debt Cap Scaling
                scaledSpending[i] = BudgetCalculator.ComputeDebtCapScaling(
                    spending, d.revenue, d.debt);
                d.scaleFactor = scaledSpending[i].scaleFactor;
                d.totalSpending = scaledSpending[i].actualTotalSpending;

                // Step 1.4 — Budget Balance → Reserve → Debt
                float debt = d.debt;
                float reserve = d.reserve;
                BudgetCalculator.ComputeBudgetBalance(
                    d.revenue, scaledSpending[i].actualTotalSpending,
                    ref debt, ref reserve);
                d.debt = debt;
                d.reserve = reserve;

                totalActualCityCost += scaledSpending[i].actualCityCost;
                
                gameState.districts[i].state.Value = d;
            }

            // ══════════════════════════════════════════
            // PHASE 2: Local Effects
            // ══════════════════════════════════════════

            for (int i = 0; i < n; i++)
            {
                DistrictState d = gameState.districts[i].state.Value;
                ScaledSpending s = scaledSpending[i];

                // 2.1 — GDP
                d.gdp += LocalEffectCalculator.ComputeGdpDelta(d, s);

                // 2.2 — Happiness (recomputed from scratch)
                d.happiness = LocalEffectCalculator.ComputeHappiness(d, s);

                // 2.3 — Infrastructure
                d.infrastructure += LocalEffectCalculator.ComputeInfrastructureDelta(d, s);

                // 2.4 — Sustainability
                d.sustainability += LocalEffectCalculator.ComputeSustainabilityDelta(d, s);

                // 2.4 (continued) — Outmigration (uses updated sustainability)
                d.population -= LocalEffectCalculator.ComputeOutmigration(d);
                
                gameState.districts[i].state.Value = d;
            }

            // ══════════════════════════════════════════
            // PHASE 3: Spillover
            // ══════════════════════════════════════════

            SpilloverResolver.ResolveGentrification(gameState.districts, n);
            SpilloverResolver.ResolvePollution(gameState.districts, n);
            SpilloverResolver.ResolveCommuting(gameState.districts, gameState.cityMetrics, n);

            // ══════════════════════════════════════════
            // PHASE 4: City Metrics
            // ══════════════════════════════════════════

            // 4.1 — City Reputation
            gameState.cityMetrics.cityReputation = CityMetricsManager.ComputeCityReputation(
                gameState.districts, n);

            // 4.2 — Population Distribution
            CityMetricsManager.DistributePopulation(
                gameState.districts, gameState.cityMetrics.cityReputation, n);

            // 4.3 — Shared Infrastructure
            gameState.cityMetrics.sharedInfraQuality = CityMetricsManager.UpdateSharedInfrastructure(
                totalActualCityCost, gameState.cityMetrics.sharedInfraQuality);

            // 4.4 & 4.5 — Federal Funding (grants + stabilization)
            CityMetricsManager.ResolveFederalFunding(gameState.districts, n);

            // ══════════════════════════════════════════
            // PHASE 5: Clamp and Commit
            // ══════════════════════════════════════════

            for (int i = 0; i < n; i++)
            {
                ref DistrictState d = ref gameState.districts[i];

                d.gdp = Math.Min(Math.Max(d.gdp, 0f), 100f);
                d.happiness = Math.Min(Math.Max(d.happiness, 0f), 100f);
                d.population = Math.Min(Math.Max(d.population,
                    SimulationConstants.MIN_POPULATION), SimulationConstants.MAX_POPULATION);
                d.infrastructure = Math.Min(Math.Max(d.infrastructure, 0f), 100f);
                d.sustainability = Math.Min(Math.Max(d.sustainability, 0f), 100f);
                d.debt = Math.Min(Math.Max(d.debt, 0f), 80f);
                d.reserve = Math.Min(Math.Max(d.reserve, 0f), SimulationConstants.RESERVE_CAP);

                // Cumulative tracking for scoring
                if (d.debt >= SimulationConstants.DEBT_CAP)
                    d.ticksAtDebtCap += 1;
                if (d.happiness < 20f)
                    d.ticksBelowHappiness20 += 1;
                d.totalCitySpending += scaledSpending[i].actualCityCost;
            }

            // City-level clamps
            gameState.cityMetrics.cityReputation = Math.Min(
                Math.Max(gameState.cityMetrics.cityReputation, 0f), 100f);
            gameState.cityMetrics.sharedInfraQuality = Math.Min(
                Math.Max(gameState.cityMetrics.sharedInfraQuality, 0f), 100f);

            // Advance tick
            gameState.currentTick += 1;
            gameState.currentMonth = gameState.currentTick / SimulationConstants.TICKS_PER_MONTH;

            return gameState;
        }
    }
}
