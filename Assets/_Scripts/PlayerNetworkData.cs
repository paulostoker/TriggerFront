using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PlayerNetworkData : NetworkBehaviour
{
    public static PlayerNetworkData Instance;
    public static event System.Action OnRosterApplied;

    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        if (IsOwner) Instance = this;
        Debug.Log($"[PND][OnNetworkSpawn] owner={IsOwner} server={IsServer} clientId={OwnerClientId}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitSelectedFreelancersServerRpc(FixedString512Bytes freelancerNamesCsv, ServerRpcParams rpcParams = default)
    {
        Debug.Log($"[PND][SubmitSelectedFreelancersServerRpc] from={rpcParams.Receive.SenderClientId} payloadLen={freelancerNamesCsv.Length}");
        var csm = CharacterSelectManager.Instance != null ? CharacterSelectManager.Instance : Object.FindFirstObjectByType<CharacterSelectManager>();
        if (csm == null) { Debug.Log("[PND] CharacterSelectManager not found"); return; }
        ulong sender = rpcParams.Receive.SenderClientId;
        csm.StorePlayerSelection(sender, freelancerNamesCsv.ToString());
        isReady.Value = true;
    }

    [ClientRpc]
    public void ApplyRosterClientRpc(int[] p1Idx, int[] p2Idx, bool isP1Attacker, ClientRpcParams rpcParams = default)
    {

        Debug.Log($"[PND][ApplyRosterClientRpc] recv on client. p1={p1Idx?.Length} p2={p2Idx?.Length} atkP1={isP1Attacker}");
        var session = GameSessionManager.Instance;
        if (session == null) { Debug.Log("[PND] GameSessionManager null on client"); return; }
        session.ResetSelections();
        if (p1Idx != null) session.SelectedAttackersIdx.AddRange(p1Idx);
        if (p2Idx != null) session.SelectedDefendersIdx.AddRange(p2Idx);
        session.IsPlayer1Attacker = isP1Attacker;
        session.SelectionsReady = true;
        Debug.Log($"[PND] Session after ApplyRoster: atkIdx={session.SelectedAttackersIdx.Count} defIdx={session.SelectedDefendersIdx.Count} ready={session.SelectionsReady}");
        OnRosterApplied?.Invoke();

    }

    [ServerRpc(RequireOwnership = false)]
public void RequestRosterServerRpc(ServerRpcParams rpcParams = default)
{
    Debug.Log($"[PND][RequestRosterServerRpc] from={rpcParams.Receive.SenderClientId}");
    var session = GameSessionManager.Instance;
    if (session == null) { Debug.Log("[PND] GameSessionManager null on server"); return; }

    var p1Idx = session.SelectedAttackersIdx != null ? session.SelectedAttackersIdx.ToArray() : new int[0];
    var p2Idx = session.SelectedDefendersIdx != null ? session.SelectedDefendersIdx.ToArray() : new int[0];

    Debug.Log($"[PND] Server sending roster to {rpcParams.Receive.SenderClientId}: p1={p1Idx.Length} p2={p2Idx.Length} atkP1={session.IsPlayer1Attacker}");
    var target = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { rpcParams.Receive.SenderClientId } } };
    ApplyRosterClientRpc(p1Idx, p2Idx, session.IsPlayer1Attacker, target);
}

}
