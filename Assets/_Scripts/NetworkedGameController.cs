using Unity.Netcode;
using UnityEngine;
using System;

public class NetworkedGameController : NetworkBehaviour
{
    public static NetworkedGameController Instance;

    public static event Action<int,int,ulong> OnRequestMoveServer;
    public static event Action<int,int,ulong> OnRequestAttackServer;
    public static event Action<ulong> OnRequestEndTurnServer;
    public static event Action<int,int,ulong> OnRequestRollServer;
    public static event Action<int,int,int,int,ulong> OnRequestUseCardServer;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    #region RPCs
    [ServerRpc(RequireOwnership=false)]
    public void RequestMoveServerRpc(int freelancerId, int targetTileIndex, ServerRpcParams rpcParams = default)
    {
        OnRequestMoveServer?.Invoke(freelancerId, targetTileIndex, rpcParams.Receive.SenderClientId);
    }

    [ServerRpc(RequireOwnership=false)]
    public void RequestAttackServerRpc(int attackerId, int targetId, ServerRpcParams rpcParams = default)
    {
        OnRequestAttackServer?.Invoke(attackerId, targetId, rpcParams.Receive.SenderClientId);
    }

    [ServerRpc(RequireOwnership=false)]
    public void RequestEndTurnServerRpc(ServerRpcParams rpcParams = default)
    {
        OnRequestEndTurnServer?.Invoke(rpcParams.Receive.SenderClientId);
    }

    [ServerRpc(RequireOwnership=false)]
    public void RequestRollServerRpc(int requesterId, int rollType, ServerRpcParams rpcParams = default)
    {
        OnRequestRollServer?.Invoke(requesterId, rollType, rpcParams.Receive.SenderClientId);
    }

    [ServerRpc(RequireOwnership=false)]
    public void RequestUseCardServerRpc(int ownerFreelancerId, int cardId, int targetId, int aux, ServerRpcParams rpcParams = default)
    {
        OnRequestUseCardServer?.Invoke(ownerFreelancerId, cardId, targetId, aux, rpcParams.Receive.SenderClientId);
    }
    #endregion
}
