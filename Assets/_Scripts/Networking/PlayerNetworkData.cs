// _Scripts/Networking/PlayerNetworkData.cs (VERSÃO SIMPLIFICADA E CORRIGIDA)
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkData : NetworkBehaviour
{
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // O ServerRpc permanece o mesmo
    [ServerRpc]
    public void SubmitSelectedFreelancersServerRpc(FixedString512Bytes freelancerNamesJson)
    {
        if (CharacterSelectManager.Instance != null)
        {
            // A chamada para StorePlayerSelection ainda é necessária
            CharacterSelectManager.Instance.StorePlayerSelection(OwnerClientId, freelancerNamesJson.ToString());
        }
    }
}