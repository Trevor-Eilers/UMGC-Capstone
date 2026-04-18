// Author: Tyson

using System;

namespace Simulation
{
    /// <summary>
    /// Pure C# AI strategy logic. No Unity dependencies.
    /// Provides policy values based on strategy profiles with
    /// reactive adjustments when district metrics hit warning thresholds.
    /// </summary>
    public static class AIStrategy
    {
        public enum Profile
        {
            Balanced,
            EducationHeavy,
            InfraNeglect,
            HighTaxSaver,
            FreeRider
        }

        /// <summary>
        /// Returns base policy values for the given strategy profile.
        /// Values match the tested profiles in SimulationHarness.
        /// </summary>
        public static PolicyValues GetBasePolicies(Profile profile)
        {
            switch (profile)
            {
                case Profile.EducationHeavy:
                    return MakePolicies(15f, 80f, 40f, 40f, 40f, 25f);
                case Profile.InfraNeglect:
                    return MakePolicies(15f, 70f, 10f, 60f, 50f, 25f);
                case Profile.HighTaxSaver:
                    return MakePolicies(25f, 30f, 30f, 30f, 30f, 25f);
                case Profile.FreeRider:
                    return MakePolicies(15f, 60f, 60f, 60f, 60f, 0f);
                default:
                    return MakePolicies(15f, 50f, 50f, 50f, 50f, 25f);
            }
        }

        /// <summary>
        /// Takes base policies and adjusts them based on current district state.
        /// Returns modified policies without changing the input.
        /// </summary>
        public static PolicyValues ApplyReactiveAdjustments(
            PolicyValues policies, DistrictState state)
        {
            // ── DEBT CRISIS — raise taxes and cut spending ──
            if (state.debt > 50f)
            {
                policies.taxRate = Math.Min(policies.taxRate + 5f, 30f);
                policies.education = Math.Max(policies.education - 10f, 10f);
                policies.cityContribution = Math.Max(
                    policies.cityContribution - 10f, 0f);
            }
            else if (state.debt > 35f)
            {
                policies.taxRate = Math.Min(policies.taxRate + 2f, 25f);
            }

            // ── HAPPINESS EMERGENCY — boost housing ──
            if (state.happiness < 30f)
            {
                policies.housing = Math.Min(policies.housing + 15f, 100f);
            }
            else if (state.happiness < 45f)
            {
                policies.housing = Math.Min(policies.housing + 5f, 90f);
            }

            // ── SUSTAINABILITY WARNING — boost environment and infra ──
            if (state.sustainability < 35f)
            {
                policies.environment = Math.Min(policies.environment + 15f, 100f);
                policies.infrastructure = Math.Min(
                    policies.infrastructure + 10f, 90f);
            }
            else if (state.sustainability < 45f)
            {
                policies.environment = Math.Min(policies.environment + 5f, 80f);
            }

            // ── INFRASTRUCTURE CRUMBLING — boost infra spending ──
            if (state.infrastructure < 30f)
            {
                policies.infrastructure = Math.Min(
                    policies.infrastructure + 15f, 100f);
            }

            return policies;
        }

        /// <summary>
        /// Computes final policy values for one tick: base strategy plus
        /// optional reactive adjustments.
        /// </summary>
        public static PolicyValues ComputePolicies(
            Profile profile, DistrictState state, bool reactiveEnabled)
        {
            var policies = GetBasePolicies(profile);

            if (reactiveEnabled)
            {
                policies = ApplyReactiveAdjustments(policies, state);
            }

            return policies;
        }

        private static PolicyValues MakePolicies(
            float tax, float edu, float infra,
            float housing, float env, float city)
        {
            return new PolicyValues
            {
                taxRate = tax,
                education = edu,
                infrastructure = infra,
                housing = housing,
                environment = env,
                cityContribution = city
            };
        }
    }
}