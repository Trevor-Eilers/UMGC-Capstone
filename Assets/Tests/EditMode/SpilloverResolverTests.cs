// Author: Malcolm Bramble

using System;
using NUnit.Framework;
using Simulation;

[TestFixture]
public class SpilloverResolverTests
{
    // ──────────────────────────────────────────────
    // Save/restore mutable constants
    // ──────────────────────────────────────────────

    private float saved_GENTRIFY_THRESHOLD;
    private float saved_K_GENTRIFY_HAPPY, saved_K_GENTRIFY_POP;
    private float saved_K_GENTRIFY_GDP_GAIN, saved_K_GENTRIFY_WEALTHY_HAPPY;
    private float saved_POLLUTE_ENV_THRESHOLD, saved_POLLUTE_GDP_THRESHOLD;
    private float saved_K_POLLUTION_GENERATE, saved_K_POLLUTION_SUSTAIN, saved_K_POLLUTION_HAPPY;
    private float saved_K_POLLUTION_SELF_SUSTAIN, saved_K_POLLUTION_SELF_HAPPY;
    private float saved_COMMUTE_GDP_THRESHOLD, saved_COMMUTE_INFRA_THRESHOLD;
    private float saved_K_COMMUTE_VOLUME, saved_K_COMMUTE_GDP_GAIN;
    private float saved_K_COMMUTE_CONGESTION, saved_K_COMMUTE_GDP_DRAIN;
    private float saved_K_COMMUTE_HOME_HAPPY;

    [SetUp]
    public void SaveConstants()
    {
        saved_GENTRIFY_THRESHOLD = SimulationConstants.GENTRIFY_THRESHOLD;
        saved_K_GENTRIFY_HAPPY = SimulationConstants.K_GENTRIFY_HAPPY;
        saved_K_GENTRIFY_POP = SimulationConstants.K_GENTRIFY_POP;
        saved_K_GENTRIFY_GDP_GAIN = SimulationConstants.K_GENTRIFY_GDP_GAIN;
        saved_K_GENTRIFY_WEALTHY_HAPPY = SimulationConstants.K_GENTRIFY_WEALTHY_HAPPY;
        saved_POLLUTE_ENV_THRESHOLD = SimulationConstants.POLLUTE_ENV_THRESHOLD;
        saved_POLLUTE_GDP_THRESHOLD = SimulationConstants.POLLUTE_GDP_THRESHOLD;
        saved_K_POLLUTION_GENERATE = SimulationConstants.K_POLLUTION_GENERATE;
        saved_K_POLLUTION_SUSTAIN = SimulationConstants.K_POLLUTION_SUSTAIN;
        saved_K_POLLUTION_HAPPY = SimulationConstants.K_POLLUTION_HAPPY;
        saved_K_POLLUTION_SELF_SUSTAIN = SimulationConstants.K_POLLUTION_SELF_SUSTAIN;
        saved_K_POLLUTION_SELF_HAPPY = SimulationConstants.K_POLLUTION_SELF_HAPPY;
        saved_COMMUTE_GDP_THRESHOLD = SimulationConstants.COMMUTE_GDP_THRESHOLD;
        saved_COMMUTE_INFRA_THRESHOLD = SimulationConstants.COMMUTE_INFRA_THRESHOLD;
        saved_K_COMMUTE_VOLUME = SimulationConstants.K_COMMUTE_VOLUME;
        saved_K_COMMUTE_GDP_GAIN = SimulationConstants.K_COMMUTE_GDP_GAIN;
        saved_K_COMMUTE_CONGESTION = SimulationConstants.K_COMMUTE_CONGESTION;
        saved_K_COMMUTE_GDP_DRAIN = SimulationConstants.K_COMMUTE_GDP_DRAIN;
        saved_K_COMMUTE_HOME_HAPPY = SimulationConstants.K_COMMUTE_HOME_HAPPY;
    }

    [TearDown]
    public void RestoreConstants()
    {
        SimulationConstants.GENTRIFY_THRESHOLD = saved_GENTRIFY_THRESHOLD;
        SimulationConstants.K_GENTRIFY_HAPPY = saved_K_GENTRIFY_HAPPY;
        SimulationConstants.K_GENTRIFY_POP = saved_K_GENTRIFY_POP;
        SimulationConstants.K_GENTRIFY_GDP_GAIN = saved_K_GENTRIFY_GDP_GAIN;
        SimulationConstants.K_GENTRIFY_WEALTHY_HAPPY = saved_K_GENTRIFY_WEALTHY_HAPPY;
        SimulationConstants.POLLUTE_ENV_THRESHOLD = saved_POLLUTE_ENV_THRESHOLD;
        SimulationConstants.POLLUTE_GDP_THRESHOLD = saved_POLLUTE_GDP_THRESHOLD;
        SimulationConstants.K_POLLUTION_GENERATE = saved_K_POLLUTION_GENERATE;
        SimulationConstants.K_POLLUTION_SUSTAIN = saved_K_POLLUTION_SUSTAIN;
        SimulationConstants.K_POLLUTION_HAPPY = saved_K_POLLUTION_HAPPY;
        SimulationConstants.K_POLLUTION_SELF_SUSTAIN = saved_K_POLLUTION_SELF_SUSTAIN;
        SimulationConstants.K_POLLUTION_SELF_HAPPY = saved_K_POLLUTION_SELF_HAPPY;
        SimulationConstants.COMMUTE_GDP_THRESHOLD = saved_COMMUTE_GDP_THRESHOLD;
        SimulationConstants.COMMUTE_INFRA_THRESHOLD = saved_COMMUTE_INFRA_THRESHOLD;
        SimulationConstants.K_COMMUTE_VOLUME = saved_K_COMMUTE_VOLUME;
        SimulationConstants.K_COMMUTE_GDP_GAIN = saved_K_COMMUTE_GDP_GAIN;
        SimulationConstants.K_COMMUTE_CONGESTION = saved_K_COMMUTE_CONGESTION;
        SimulationConstants.K_COMMUTE_GDP_DRAIN = saved_K_COMMUTE_GDP_DRAIN;
        SimulationConstants.K_COMMUTE_HOME_HAPPY = saved_K_COMMUTE_HOME_HAPPY;
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static DistrictState[] MakeTwoDistricts(
        float gdpA = 50f, float gdpB = 50f,
        float happyA = 55f, float happyB = 55f,
        float popA = 150f, float popB = 150f,
        float sustainA = 55f, float sustainB = 55f,
        float envSliderA = 50f, float envSliderB = 50f)
    {
        var districts = new DistrictState[4];
        districts[0] = DistrictState.Default(0);
        districts[0].gdp = gdpA;
        districts[0].happiness = happyA;
        districts[0].population = popA;
        districts[0].sustainability = sustainA;
        districts[0].policyValues.environment = envSliderA;

        districts[1] = DistrictState.Default(1);
        districts[1].gdp = gdpB;
        districts[1].happiness = happyB;
        districts[1].population = popB;
        districts[1].sustainability = sustainB;
        districts[1].policyValues.environment = envSliderB;

        // Inactive districts (defaults, won't be processed with numActive=2)
        districts[2] = DistrictState.Default(2);
        districts[3] = DistrictState.Default(3);
        return districts;
    }

    /// <summary>
    /// Apply per-district spillover to all active districts from the same snapshot,
    /// simulating distributed clients computing simultaneously.
    /// </summary>
    private static void ApplyToAll(
        DistrictState[] districts, int numActive,
        Action<DistrictState[], int> applyFn)
    {
        var snapshot = (DistrictState[])districts.Clone();
        for (int i = 0; i < numActive; i++)
        {
            applyFn(snapshot, i);
        }
    }

    // ══════════════════════════════════════════════
    // GENTRIFICATION
    // ══════════════════════════════════════════════

    [Test]
    public void Gentrification_GdpDiff9_Fires()
    {
        SimulationConstants.K_GENTRIFY_HAPPY = 1.0f;
        SimulationConstants.K_GENTRIFY_POP = 1.0f;
        SimulationConstants.K_GENTRIFY_GDP_GAIN = 1.0f;
        SimulationConstants.K_GENTRIFY_WEALTHY_HAPPY = 1.0f;

        var districts = MakeTwoDistricts(gdpA: 59f, gdpB: 50f);
        var snapshot = (DistrictState[])districts.Clone();

        SpilloverResolver.ApplyGentrification(ref districts[0], 0, snapshot, 2);
        SpilloverResolver.ApplyGentrification(ref districts[1], 1, snapshot, 2);

        // GDP diff = 9, threshold = 8, magnitude = (9-8)*1.0 = 1.0
        // Wealthy (A=0): gdp += 1.0, happiness -= 1.0
        // Poor (B=1): happiness -= 1.0, population -= 1.0
        Assert.AreEqual(60f, districts[0].gdp, 0.01f, "Wealthy GDP should increase");
        Assert.AreEqual(54f, districts[0].happiness, 0.01f, "Wealthy happiness should decrease");
        Assert.AreEqual(54f, districts[1].happiness, 0.01f, "Poor happiness should decrease");
        Assert.AreEqual(149f, districts[1].population, 0.01f, "Poor population should decrease");
    }

    [Test]
    public void Gentrification_GdpDiff7_DoesNotFire()
    {
        SimulationConstants.K_GENTRIFY_HAPPY = 1.0f;
        SimulationConstants.K_GENTRIFY_POP = 1.0f;
        SimulationConstants.K_GENTRIFY_GDP_GAIN = 1.0f;
        SimulationConstants.K_GENTRIFY_WEALTHY_HAPPY = 1.0f;

        var districts = MakeTwoDistricts(gdpA: 57f, gdpB: 50f);
        float origHappyA = districts[0].happiness;
        float origHappyB = districts[1].happiness;
        float origPopB = districts[1].population;

        var snapshot = (DistrictState[])districts.Clone();
        SpilloverResolver.ApplyGentrification(ref districts[0], 0, snapshot, 2);
        SpilloverResolver.ApplyGentrification(ref districts[1], 1, snapshot, 2);

        Assert.AreEqual(57f, districts[0].gdp, 0.01f, "GDP should not change");
        Assert.AreEqual(origHappyA, districts[0].happiness, 0.01f);
        Assert.AreEqual(origHappyB, districts[1].happiness, 0.01f);
        Assert.AreEqual(origPopB, districts[1].population, 0.01f);
    }

    [Test]
    public void Gentrification_ExactlyAtThreshold8_DoesNotFire()
    {
        SimulationConstants.K_GENTRIFY_HAPPY = 1.0f;
        SimulationConstants.K_GENTRIFY_POP = 1.0f;

        var districts = MakeTwoDistricts(gdpA: 58f, gdpB: 50f);
        float origHappyB = districts[1].happiness;

        var snapshot = (DistrictState[])districts.Clone();
        SpilloverResolver.ApplyGentrification(ref districts[1], 1, snapshot, 2);

        Assert.AreEqual(origHappyB, districts[1].happiness, 0.01f,
            "At exactly threshold, gentrification should not fire");
    }

    [Test]
    public void Gentrification_DiagonalPair_HalfWeight()
    {
        SimulationConstants.K_GENTRIFY_HAPPY = 1.0f;
        SimulationConstants.K_GENTRIFY_POP = 1.0f;
        SimulationConstants.K_GENTRIFY_GDP_GAIN = 1.0f;
        SimulationConstants.K_GENTRIFY_WEALTHY_HAPPY = 1.0f;

        // 2-player game: districts 0 and 1 are border neighbors (weight 1.0).
        var borderDistricts = MakeTwoDistricts(gdpA: 59f, gdpB: 50f);
        var snapshot = (DistrictState[])borderDistricts.Clone();
        SpilloverResolver.ApplyGentrification(ref borderDistricts[0], 0, snapshot, 2);
        float borderGdpGain = borderDistricts[0].gdp - 59f;

        Assert.AreEqual(1.0f, borderGdpGain, 0.01f, "Border pair: full weight");

        // Verify diagonal weight is 0.5 via the AdjacencyMap directly
        Assert.AreEqual(0.5f, AdjacencyMap.GetWeight(0, 3), 0.001f,
            "NW↔SE diagonal should have weight 0.5");
        Assert.AreEqual(1.0f, AdjacencyMap.GetWeight(0, 1), 0.001f,
            "NW↔NE border should have weight 1.0");
    }

    // ══════════════════════════════════════════════
    // POLLUTION — REQUIRES BOTH CONDITIONS
    // ══════════════════════════════════════════════

    [Test]
    public void Pollution_BothConditionsMet_Fires()
    {
        SimulationConstants.K_POLLUTION_GENERATE = 1.0f;
        SimulationConstants.K_POLLUTION_SUSTAIN = 1.0f;
        SimulationConstants.K_POLLUTION_HAPPY = 1.0f;
        SimulationConstants.K_POLLUTION_SELF_SUSTAIN = 0.5f;
        SimulationConstants.K_POLLUTION_SELF_HAPPY = 0.5f;

        var districts = MakeTwoDistricts(
            gdpA: 60f, envSliderA: 10f,
            gdpB: 50f, envSliderB: 50f);

        float origSustainB = districts[1].sustainability;
        float origSustainA = districts[0].sustainability;

        var snapshot = (DistrictState[])districts.Clone();
        SpilloverResolver.ApplyPollution(ref districts[0], 0, snapshot, 2);
        SpilloverResolver.ApplyPollution(ref districts[1], 1, snapshot, 2);

        Assert.Less(districts[1].sustainability, origSustainB,
            "Neighbor sustainability should decrease from pollution");
        Assert.Less(districts[0].sustainability, origSustainA,
            "Polluter self-damage should occur");
    }

    [Test]
    public void Pollution_LowEnvButLowGdp_DoesNotFire()
    {
        SimulationConstants.K_POLLUTION_GENERATE = 1.0f;
        SimulationConstants.K_POLLUTION_SUSTAIN = 1.0f;
        SimulationConstants.K_POLLUTION_HAPPY = 1.0f;
        SimulationConstants.K_POLLUTION_SELF_SUSTAIN = 0.5f;
        SimulationConstants.K_POLLUTION_SELF_HAPPY = 0.5f;

        var districts = MakeTwoDistricts(
            gdpA: 30f, envSliderA: 10f,
            gdpB: 50f, envSliderB: 50f);

        float origSustainA = districts[0].sustainability;
        float origSustainB = districts[1].sustainability;

        var snapshot = (DistrictState[])districts.Clone();
        SpilloverResolver.ApplyPollution(ref districts[0], 0, snapshot, 2);
        SpilloverResolver.ApplyPollution(ref districts[1], 1, snapshot, 2);

        Assert.AreEqual(origSustainA, districts[0].sustainability, 0.01f,
            "No pollution when GDP is at or below threshold");
        Assert.AreEqual(origSustainB, districts[1].sustainability, 0.01f);
    }

    [Test]
    public void Pollution_HighGdpButHighEnv_DoesNotFire()
    {
        SimulationConstants.K_POLLUTION_GENERATE = 1.0f;
        SimulationConstants.K_POLLUTION_SUSTAIN = 1.0f;
        SimulationConstants.K_POLLUTION_HAPPY = 1.0f;
        SimulationConstants.K_POLLUTION_SELF_SUSTAIN = 0.5f;
        SimulationConstants.K_POLLUTION_SELF_HAPPY = 0.5f;

        var districts = MakeTwoDistricts(
            gdpA: 60f, envSliderA: 50f,
            gdpB: 50f, envSliderB: 50f);

        float origSustainB = districts[1].sustainability;

        var snapshot = (DistrictState[])districts.Clone();
        SpilloverResolver.ApplyPollution(ref districts[1], 1, snapshot, 2);

        Assert.AreEqual(origSustainB, districts[1].sustainability, 0.01f,
            "No pollution when environment slider is at or above threshold");
    }

    [Test]
    public void Pollution_SelfDamageLowerThanNeighborDamage()
    {
        SimulationConstants.K_POLLUTION_GENERATE = 1.0f;
        SimulationConstants.K_POLLUTION_SUSTAIN = 1.0f;
        SimulationConstants.K_POLLUTION_SELF_SUSTAIN = 0.5f;
        SimulationConstants.K_POLLUTION_HAPPY = 0f;
        SimulationConstants.K_POLLUTION_SELF_HAPPY = 0f;

        var districts = MakeTwoDistricts(
            gdpA: 60f, envSliderA: 10f, sustainA: 80f,
            gdpB: 50f, envSliderB: 50f, sustainB: 80f);

        var snapshot = (DistrictState[])districts.Clone();
        SpilloverResolver.ApplyPollution(ref districts[0], 0, snapshot, 2);
        SpilloverResolver.ApplyPollution(ref districts[1], 1, snapshot, 2);

        float selfLoss = 80f - districts[0].sustainability;
        float neighborLoss = 80f - districts[1].sustainability;

        Assert.Greater(neighborLoss, selfLoss,
            "Self-damage should be less than neighbor damage");
    }

    // ══════════════════════════════════════════════
    // COMMUTING — REQUIRES BOTH CONDITIONS
    // ══════════════════════════════════════════════

    [Test]
    public void Commuting_BothConditionsMet_Fires()
    {
        SimulationConstants.K_COMMUTE_VOLUME = 1.0f;
        SimulationConstants.K_COMMUTE_GDP_GAIN = 1.0f;
        SimulationConstants.K_COMMUTE_CONGESTION = 1.0f;
        SimulationConstants.K_COMMUTE_GDP_DRAIN = 1.0f;
        SimulationConstants.K_COMMUTE_HOME_HAPPY = 1.0f;

        var districts = MakeTwoDistricts(gdpA: 60f, gdpB: 50f);
        var cityMetrics = CityMetrics.Default(); // sharedInfra = 50 (> 25)

        var snapshot = (DistrictState[])districts.Clone();
        SpilloverResolver.ApplyCommuting(ref districts[0], 0, snapshot, cityMetrics, 2);
        SpilloverResolver.ApplyCommuting(ref districts[1], 1, snapshot, cityMetrics, 2);

        Assert.Greater(districts[0].gdp, 60f, "Work district GDP should increase");
        Assert.Less(districts[1].gdp, 50f, "Home district GDP should decrease");
    }

    [Test]
    public void Commuting_GdpDiffBelowThreshold_DoesNotFire()
    {
        SimulationConstants.K_COMMUTE_VOLUME = 1.0f;
        SimulationConstants.K_COMMUTE_GDP_GAIN = 1.0f;
        SimulationConstants.K_COMMUTE_GDP_DRAIN = 1.0f;

        var districts = MakeTwoDistricts(gdpA: 54f, gdpB: 50f);
        var cityMetrics = CityMetrics.Default();

        var snapshot = (DistrictState[])districts.Clone();
        SpilloverResolver.ApplyCommuting(ref districts[0], 0, snapshot, cityMetrics, 2);
        SpilloverResolver.ApplyCommuting(ref districts[1], 1, snapshot, cityMetrics, 2);

        Assert.AreEqual(54f, districts[0].gdp, 0.01f, "No commuting below GDP threshold");
        Assert.AreEqual(50f, districts[1].gdp, 0.01f);
    }

    [Test]
    public void Commuting_LowSharedInfra_DoesNotFire()
    {
        SimulationConstants.K_COMMUTE_VOLUME = 1.0f;
        SimulationConstants.K_COMMUTE_GDP_GAIN = 1.0f;
        SimulationConstants.K_COMMUTE_GDP_DRAIN = 1.0f;

        var districts = MakeTwoDistricts(gdpA: 60f, gdpB: 50f);
        var cityMetrics = CityMetrics.Default();
        cityMetrics.sharedInfraQuality = 20f;

        var snapshot = (DistrictState[])districts.Clone();
        SpilloverResolver.ApplyCommuting(ref districts[0], 0, snapshot, cityMetrics, 2);
        SpilloverResolver.ApplyCommuting(ref districts[1], 1, snapshot, cityMetrics, 2);

        Assert.AreEqual(60f, districts[0].gdp, 0.01f,
            "No commuting when shared infra is at or below threshold");
        Assert.AreEqual(50f, districts[1].gdp, 0.01f);
    }

    [Test]
    public void Commuting_ExactlyAtInfraThreshold_DoesNotFire()
    {
        SimulationConstants.K_COMMUTE_VOLUME = 1.0f;
        SimulationConstants.K_COMMUTE_GDP_GAIN = 1.0f;
        SimulationConstants.K_COMMUTE_GDP_DRAIN = 1.0f;

        var districts = MakeTwoDistricts(gdpA: 60f, gdpB: 50f);
        var cityMetrics = CityMetrics.Default();
        cityMetrics.sharedInfraQuality = 25f;

        var snapshot = (DistrictState[])districts.Clone();
        SpilloverResolver.ApplyCommuting(ref districts[0], 0, snapshot, cityMetrics, 2);
        SpilloverResolver.ApplyCommuting(ref districts[1], 1, snapshot, cityMetrics, 2);

        Assert.AreEqual(60f, districts[0].gdp, 0.01f,
            "Commuting requires sharedInfra > threshold, not >=");
    }

    [Test]
    public void Commuting_HigherSharedInfra_MoreCommuters()
    {
        SimulationConstants.K_COMMUTE_VOLUME = 1.0f;
        SimulationConstants.K_COMMUTE_GDP_GAIN = 1.0f;
        SimulationConstants.K_COMMUTE_CONGESTION = 0f;
        SimulationConstants.K_COMMUTE_GDP_DRAIN = 0f;
        SimulationConstants.K_COMMUTE_HOME_HAPPY = 0f;

        // Run with sharedInfra=50
        var districts50 = MakeTwoDistricts(gdpA: 60f, gdpB: 50f);
        var city50 = CityMetrics.Default();
        city50.sharedInfraQuality = 50f;
        var snapshot50 = (DistrictState[])districts50.Clone();
        SpilloverResolver.ApplyCommuting(ref districts50[0], 0, snapshot50, city50, 2);
        float gdpGain50 = districts50[0].gdp - 60f;

        // Run with sharedInfra=100
        var districts100 = MakeTwoDistricts(gdpA: 60f, gdpB: 50f);
        var city100 = CityMetrics.Default();
        city100.sharedInfraQuality = 100f;
        var snapshot100 = (DistrictState[])districts100.Clone();
        SpilloverResolver.ApplyCommuting(ref districts100[0], 0, snapshot100, city100, 2);
        float gdpGain100 = districts100[0].gdp - 60f;

        Assert.Greater(gdpGain100, gdpGain50,
            "Higher shared infra should produce more commuters and more GDP gain");
        Assert.AreEqual(gdpGain100, gdpGain50 * 2f, 0.01f,
            "Double infra should produce double commuters (linear scaling)");
    }

    // ══════════════════════════════════════════════
    // ADJACENCY MAP
    // ══════════════════════════════════════════════

    [Test]
    public void AdjacencyMap_DirectNeighbors_Weight1()
    {
        Assert.AreEqual(1.0f, AdjacencyMap.GetWeight(0, 1), 0.001f, "NW↔NE border");
        Assert.AreEqual(1.0f, AdjacencyMap.GetWeight(0, 2), 0.001f, "NW↔SW border");
        Assert.AreEqual(1.0f, AdjacencyMap.GetWeight(1, 3), 0.001f, "NE↔SE border");
        Assert.AreEqual(1.0f, AdjacencyMap.GetWeight(2, 3), 0.001f, "SW↔SE border");
    }

    [Test]
    public void AdjacencyMap_DiagonalNeighbors_WeightHalf()
    {
        Assert.AreEqual(0.5f, AdjacencyMap.GetWeight(0, 3), 0.001f, "NW↔SE diagonal");
        Assert.AreEqual(0.5f, AdjacencyMap.GetWeight(1, 2), 0.001f, "NE↔SW diagonal");
    }

    [Test]
    public void AdjacencyMap_Symmetric()
    {
        for (int i = 0; i < AdjacencyMap.AllPairs.Length; i++)
        {
            var p = AdjacencyMap.AllPairs[i];
            Assert.AreEqual(
                AdjacencyMap.GetWeight(p.indexA, p.indexB),
                AdjacencyMap.GetWeight(p.indexB, p.indexA),
                0.001f, $"Weight should be symmetric for pair ({p.indexA},{p.indexB})");
        }
    }
}
