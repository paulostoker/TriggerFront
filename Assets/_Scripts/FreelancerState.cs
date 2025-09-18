using System;
using Unity.Netcode;

public struct FreelancerState : INetworkSerializable, IEquatable<FreelancerState>
{
    public int freelancerId;
    public int currentHP;
    public bool isAlive;
    public bool hasMovedThisTurn;
    public bool hasActedThisTurn;
    public bool isPlayer1;
    public int tileX;
    public int tileZ;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref freelancerId);
        serializer.SerializeValue(ref currentHP);
        serializer.SerializeValue(ref isAlive);
        serializer.SerializeValue(ref hasMovedThisTurn);
        serializer.SerializeValue(ref hasActedThisTurn);
        serializer.SerializeValue(ref isPlayer1);
        serializer.SerializeValue(ref tileX);
        serializer.SerializeValue(ref tileZ);
    }

    public bool Equals(FreelancerState other)
    {
        return freelancerId == other.freelancerId
            && currentHP == other.currentHP
            && isAlive == other.isAlive
            && hasMovedThisTurn == other.hasMovedThisTurn
            && hasActedThisTurn == other.hasActedThisTurn
            && isPlayer1 == other.isPlayer1
            && tileX == other.tileX
            && tileZ == other.tileZ;
    }

    public override bool Equals(object obj)
    {
        return obj is FreelancerState other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = freelancerId;
            hash = (hash * 397) ^ currentHP;
            hash = (hash * 397) ^ (isAlive ? 1 : 0);
            hash = (hash * 397) ^ (hasMovedThisTurn ? 1 : 0);
            hash = (hash * 397) ^ (hasActedThisTurn ? 1 : 0);
            hash = (hash * 397) ^ (isPlayer1 ? 1 : 0);
            hash = (hash * 397) ^ tileX;
            hash = (hash * 397) ^ tileZ;
            return hash;
        }
    }
}
