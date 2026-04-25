// Author: Tyson

using NUnit.Framework;
using Simulation;

[TestFixture]
public class AIStrategyTests
{
    // ══════════════════════════════════════════════
    // BASE STRATEGY PROFILES
    // ══════════════════════════════════════════════

    [Test]
    public void Balanced_ReturnsCorrectValues()
    {
        var policies = AIStrategy.GetBasePolicies(AIStrategy.Profile.Balanced);

        Assert.AreEqual(15f, policies.taxRate, 0.01f);
        Assert.AreEqual(50f, policies.education, 0.01f);
        Assert.AreEqual(50f, policies.infrastructure, 0.01f);
        Assert.AreEqual(50f, policies.housing, 0.01f);
        Assert.AreEqual(50f, policies.environment, 0.01f);
        Assert.AreEqual(25f, policies.cityContribution, 0.01f);
    }

    [Test]
    public void EducationHeavy_PrioritizesEducation()
    {
        var policies = AIStrategy.GetBasePolicies(AIStrategy.Profile.EducationHeavy);

        Assert.AreEqual(80f, policies.education, 0.01f);
        Assert.AreEqual(40f, policies.infrastructure, 0.01f);
    }

    [Test]
    public void FreeRider_ZeroCityContribution()
    {
        var policies = AIStrategy.GetBasePolicies(AIStrategy.Profile.FreeRider);

        Assert.AreEqual(0f, policies.cityContribution, 0.01f);
    }

    // ══════════════════════════════════════════════
    // REACTIVE ADJUSTMENTS — DEBT
    // ══════════════════════════════════════════════

    [Test]
    public void Reactive_HighDebt_RaisesTaxes()
    {
        var basePolicies = AIStrategy.GetBasePolicies(AIStrategy.Profile.Balanced);
        var state = new DistrictState { policyValues = PolicyValues.Default() };
        state.debt = 55f;

        var result = AIStrategy.ApplyReactiveAdjustments(basePolicies, state);

        Assert.AreEqual(20f, result.taxRate, 0.01f,
            "Tax should increase by 5 when debt > 50");
    }

    [Test]
    public void Reactive_HighDebt_CutsEducationAndCity()
    {
        var basePolicies = AIStrategy.GetBasePolicies(AIStrategy.Profile.Balanced);
        var state = new DistrictState { policyValues = PolicyValues.Default() };
        state.debt = 55f;

        var result = AIStrategy.ApplyReactiveAdjustments(basePolicies, state);

        Assert.AreEqual(40f, result.education, 0.01f,
            "Education should decrease by 10 when debt > 50");
        Assert.AreEqual(15f, result.cityContribution, 0.01f,
            "City contribution should decrease by 10 when debt > 50");
    }

    [Test]
    public void Reactive_ModerateDebt_SmallTaxIncrease()
    {
        var basePolicies = AIStrategy.GetBasePolicies(AIStrategy.Profile.Balanced);
        var state = new DistrictState { policyValues = PolicyValues.Default() };
        state.debt = 40f;

        var result = AIStrategy.ApplyReactiveAdjustments(basePolicies, state);

        Assert.AreEqual(17f, result.taxRate, 0.01f,
            "Tax should increase by 2 when debt > 35");
    }

    [Test]
    public void Reactive_NormalDebt_NoChange()
    {
        var basePolicies = AIStrategy.GetBasePolicies(AIStrategy.Profile.Balanced);
        var state = new DistrictState { policyValues = PolicyValues.Default() };
        state.debt = 15f;

        var result = AIStrategy.ApplyReactiveAdjustments(basePolicies, state);

        Assert.AreEqual(15f, result.taxRate, 0.01f,
            "Tax should stay at base when debt is normal");
    }

    // ══════════════════════════════════════════════
    // REACTIVE ADJUSTMENTS — HAPPINESS
    // ══════════════════════════════════════════════

    [Test]
    public void Reactive_LowHappiness_BoostsHousing()
    {
        var basePolicies = AIStrategy.GetBasePolicies(AIStrategy.Profile.Balanced);
        var state = new DistrictState { policyValues = PolicyValues.Default() };
        state.happiness = 25f;

        var result = AIStrategy.ApplyReactiveAdjustments(basePolicies, state);

        Assert.AreEqual(65f, result.housing, 0.01f,
            "Housing should increase by 15 when happiness < 30");
    }

    [Test]
    public void Reactive_ModerateHappiness_SmallHousingBoost()
    {
        var basePolicies = AIStrategy.GetBasePolicies(AIStrategy.Profile.Balanced);
        var state = new DistrictState { policyValues = PolicyValues.Default() };
        state.happiness = 40f;

        var result = AIStrategy.ApplyReactiveAdjustments(basePolicies, state);

        Assert.AreEqual(55f, result.housing, 0.01f,
            "Housing should increase by 5 when happiness < 45");
    }

    // ══════════════════════════════════════════════
    // REACTIVE ADJUSTMENTS — SUSTAINABILITY
    // ══════════════════════════════════════════════

    [Test]
    public void Reactive_LowSustainability_BoostsEnvironmentAndInfra()
    {
        var basePolicies = AIStrategy.GetBasePolicies(AIStrategy.Profile.Balanced);
        var state = new DistrictState { policyValues = PolicyValues.Default() };
        state.sustainability = 30f;
        state.infrastructure = 50f;

        var result = AIStrategy.ApplyReactiveAdjustments(basePolicies, state);

        Assert.AreEqual(65f, result.environment, 0.01f,
            "Environment should increase by 15 when sustainability < 35");
        Assert.AreEqual(60f, result.infrastructure, 0.01f,
            "Infrastructure should increase by 10 when sustainability < 35");
    }

    // ══════════════════════════════════════════════
    // REACTIVE ADJUSTMENTS — INFRASTRUCTURE
    // ══════════════════════════════════════════════

    [Test]
    public void Reactive_LowInfrastructure_BoostsInfraSpending()
    {
        var basePolicies = AIStrategy.GetBasePolicies(AIStrategy.Profile.Balanced);
        var state = new DistrictState { policyValues = PolicyValues.Default() };
        state.infrastructure = 25f;
        state.sustainability = 50f;

        var result = AIStrategy.ApplyReactiveAdjustments(basePolicies, state);

        Assert.AreEqual(65f, result.infrastructure, 0.01f,
            "Infrastructure should increase by 15 when infra < 30");
    }

    // ══════════════════════════════════════════════
    // MULTIPLE CRISES
    // ══════════════════════════════════════════════

    [Test]
    public void Reactive_MultipleCrises_AllAdjustmentsFire()
    {
        var basePolicies = AIStrategy.GetBasePolicies(AIStrategy.Profile.Balanced);
        var state = new DistrictState { policyValues = PolicyValues.Default() };
        state.debt = 55f;
        state.happiness = 25f;
        state.sustainability = 30f;

        var result = AIStrategy.ApplyReactiveAdjustments(basePolicies, state);

        Assert.AreEqual(20f, result.taxRate, 0.01f,
            "Tax should increase from debt crisis");
        Assert.AreEqual(65f, result.housing, 0.01f,
            "Housing should increase from happiness emergency");
        Assert.AreEqual(65f, result.environment, 0.01f,
            "Environment should increase from sustainability warning");
    }

    // ══════════════════════════════════════════════
    // COMPUTE POLICIES — FULL PIPELINE
    // ══════════════════════════════════════════════

    [Test]
    public void ComputePolicies_ReactiveDisabled_ReturnsBaseOnly()
    {
        var state = new DistrictState { policyValues = PolicyValues.Default() };
        state.debt = 55f;

        var result = AIStrategy.ComputePolicies(
            AIStrategy.Profile.Balanced, state, false);

        Assert.AreEqual(15f, result.taxRate, 0.01f,
            "With reactive disabled, should return base values even during crisis");
    }

    [Test]
    public void ComputePolicies_ReactiveEnabled_AdjustsForCrisis()
    {
        var state = new DistrictState { policyValues = PolicyValues.Default() };
        state.debt = 55f;

        var result = AIStrategy.ComputePolicies(
            AIStrategy.Profile.Balanced, state, true);

        Assert.AreEqual(20f, result.taxRate, 0.01f,
            "With reactive enabled, should adjust for debt crisis");
    }

    // ══════════════════════════════════════════════
    // SLIDER CLAMPING
    // ══════════════════════════════════════════════

    [Test]
    public void Reactive_TaxNeverExceeds30()
    {
        var basePolicies = AIStrategy.GetBasePolicies(AIStrategy.Profile.HighTaxSaver);
        var state = new DistrictState { policyValues = PolicyValues.Default() };
        state.debt = 55f;

        var result = AIStrategy.ApplyReactiveAdjustments(basePolicies, state);

        Assert.LessOrEqual(result.taxRate, 30f,
            "Tax rate must never exceed 30 even with reactive increase");
    }
}