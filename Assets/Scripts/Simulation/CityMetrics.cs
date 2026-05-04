// Author: Malcolm Bramble

using Unity.Netcode;

[System.Serializable]
public struct CityMetrics : INetworkSerializable
{
    public float cityReputation;      // 0-100
    public float sharedInfraQuality;  // 0-100
    public float metroPopulationPool; // per-tick flow, can be negative

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref cityReputation);
        serializer.SerializeValue(ref sharedInfraQuality);
        serializer.SerializeValue(ref metroPopulationPool);
    }

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
