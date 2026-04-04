// Author: Malcolm Bramble

using System;
using NUnit.Framework;

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
        districts[0].sliders.environment = envSliderA;

        districts[1] = DistrictState.Default(1);
        districts[1].gdp = gdpB;
        districts[1].happiness = happyB;
        districts[1].population = popB;
        districts[1].sustainability = sustainB;
        districts[1].sliders.environment = envSliderB;

        // Inactive districts (defaults, won't be processed with numActive=2)
        districts[2] = DistrictState.Default(2);
        districts[3] = DistrictState.Default(3);
        return districts;
    }

    // ══════════════════════════════════════════════
    // GENTRIFICATION
    // ══════════════════════════════════════════════

    [Test]
    public void Gentrification_GdpDiff9_Fires()
    {
        // District 0 and 1 are direct neighbors (NW↔NE), weight 1.0
        SimulationConstants.K_GENTRIFY_HAPPY = 1.0f;
        SimulationConstants.K_GENTRIFY_POP = 1.0f;
        SimulationConstants.K_GENTRIFY_GDP_GAIN = 1.0f;
        SimulationConstants.K_GENTRIFY_WEALTHY_HAPPY = 1.0f;

        var districts = MakeTwoDistricts(gdpA: 59f, gdpB: 50f);

        SpilloverResolver.ResolveGentrification(districts, 2);

        // GDP diff = 9, threshold = 8, magnitude = (9-8)*1.0 = 1.0
        // Poor (B=1): happiness -= 1.0, population -= 1.0
        // Wealthy (A=0): gdp += 1.0, happiness -= 1.0
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

        SpilloverResolver.ResolveGentrification(districts, 2);

        // GDP diff = 7, below threshold 8 — no effects
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

        SpilloverResolver.ResolveGentrification(districts, 2);

        // GDP diff = 8, threshold is 8, condition is > (not >=)
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

        // Verify diagonal weight by comparing border vs diagonal GDP gain.
        // Use 2-player game: districts 0 and 1 are border neighbors (weight 1.0).
        var borderDistricts = MakeTwoDistricts(gdpA: 59f, gdpB: 50f);
        SpilloverResolver.ResolveGentrification(borderDistricts, 2);
        float borderGdpGain = borderDistricts[0].gdp - 59f;

        // Adjacency weight for 0↔1 is 1.0, diff=9, magnitude=(9-8)*1.0=1.0
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

        // District 0: env=10 (< 30), gdp=60 (> 40) → pollutes
        var districts = MakeTwoDistricts(
            gdpA: 60f, envSliderA: 10f,
            gdpB: 50f, envSliderB: 50f);

        float origSustainB = districts[1].sustainability;
        float origSustainA = districts[0].sustainability;

        SpilloverResolver.ResolvePollution(districts, 2);

        // pollutionOutput = (max(0, 30-10) + max(0, 60-40)) * 1.0 = (20+20)*1.0 = 40
        // Neighbor (B=1, weight 1.0): sustain -= 40*1.0*1.0, happy -= 40*1.0*1.0
        // Self (A=0): sustain -= 40*0.5, happy -= 40*0.5
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

        // District 0: env=10 (< 30) but gdp=30 (<= 40) → no pollution
        var districts = MakeTwoDistricts(
            gdpA: 30f, envSliderA: 10f,
            gdpB: 50f, envSliderB: 50f);

        float origSustainA = districts[0].sustainability;
        float origSustainB = districts[1].sustainability;

        SpilloverResolver.ResolvePollution(districts, 2);

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

        // District 0: env=50 (>= 30) and gdp=60 (> 40) → no pollution (env too high)
        var districts = MakeTwoDistricts(
            gdpA: 60f, envSliderA: 50f,
            gdpB: 50f, envSliderB: 50f);

        float origSustainB = districts[1].sustainability;

        SpilloverResolver.ResolvePollution(districts, 2);

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

        SpilloverResolver.ResolvePollution(districts, 2);

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

        SpilloverResolver.ResolveCommuting(districts, cityMetrics, 2);

        // GDP diff = 10, threshold = 5, magnitude = (10-5)*1.0 = 5
        // infraFactor = 50/100 = 0.5
        // commuters = 5 * 0.5 * 1.0 = 2.5
        Assert.Greater(districts[0].gdp, 60f, "Work district GDP should increase");
        Assert.Less(districts[1].gdp, 50f, "Home district GDP should decrease");
    }

    [Test]
    public void Commuting_GdpDiffBelowThreshold_DoesNotFire()
    {
        SimulationConstants.K_COMMUTE_VOLUME = 1.0f;
        SimulationConstants.K_COMMUTE_GDP_GAIN = 1.0f;
        SimulationConstants.K_COMMUTE_GDP_DRAIN = 1.0f;

        // GDP diff = 4, below threshold 5
        var districts = MakeTwoDistricts(gdpA: 54f, gdpB: 50f);
        var cityMetrics = CityMetrics.Default();

        SpilloverResolver.ResolveCommuting(districts, cityMetrics, 2);

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
        cityMetrics.sharedInfraQuality = 20f; // below threshold 25

        SpilloverResolver.ResolveCommuting(districts, cityMetrics, 2);

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
        cityMetrics.sharedInfraQuality = 25f; // exactly at threshold

        SpilloverResolver.ResolveCommuting(districts, cityMetrics, 2);

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
        SpilloverResolver.ResolveCommuting(districts50, city50, 2);
        float gdpGain50 = districts50[0].gdp - 60f;

        // Run with sharedInfra=100
        var districts100 = MakeTwoDistricts(gdpA: 60f, gdpB: 50f);
        var city100 = CityMetrics.Default();
        city100.sharedInfraQuality = 100f;
        SpilloverResolver.ResolveCommuting(districts100, city100, 2);
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
        // Weight should be the same regardless of order
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
