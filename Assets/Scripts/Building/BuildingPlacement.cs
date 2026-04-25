// Author: Trevor Eilers

using System;
using Unity.Netcode;

namespace Building
{
    public struct BuildingPlacement : INetworkSerializable, IEquatable<BuildingPlacement>
    {
        public byte typeId;
        public byte tierId;
        public byte squareIndex;
        public byte plotIndex;
        public ushort prefabIndex;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref typeId);
            serializer.SerializeValue(ref tierId);
            serializer.SerializeValue(ref squareIndex);
            serializer.SerializeValue(ref plotIndex);
            serializer.SerializeValue(ref prefabIndex);
        }

        public bool Equals(BuildingPlacement other)
        {
            return typeId == other.typeId
                && tierId == other.tierId
                && squareIndex == other.squareIndex
                && plotIndex == other.plotIndex
                && prefabIndex == other.prefabIndex;
        }
    }
}
