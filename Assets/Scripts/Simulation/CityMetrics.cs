// Author: Malcolm Bramble

[System.Serializable]
public struct CityMetrics
{
    public float cityReputation;      // 0-100
    public float sharedInfraQuality;  // 0-100
    public float metroPopulationPool; // per-tick flow, can be negative

    public static CityMetrics Default()
    {
        return new CityMetrics
        {
            cityReputation = 0f,
            sharedInfraQuality = SimulationConstants.SHARED_INFRA_START,
            metroPopulationPool = 0f
        };
    }
}
