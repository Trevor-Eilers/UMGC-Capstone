// Authors: Malcolm Bramble, Trevor Eilers

using Unity.Netcode;

namespace Simulation
{
    [System.Serializable]
    public struct GameState : INetworkSerializable
    {
        public CityMetrics cityMetrics;     // computed each tick from snapshot
        public int currentTick;             // 0-575
        public int currentMonth;            // 0-47 (currentTick / 12)
        public int gameSpeed;             // 0 (paused), 1, 2, or 3
        public bool isPaused;
    
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref cityMetrics);
            serializer.SerializeValue(ref currentTick);
            serializer.SerializeValue(ref currentMonth);
            serializer.SerializeValue(ref gameSpeed);
            serializer.SerializeValue(ref isPaused);
        }

        public void Default()
        {
            cityMetrics = CityMetrics.Default();
            currentTick = 0;
            currentMonth = 0;
            gameSpeed = 1;
            isPaused = true;
        }
    }
}
