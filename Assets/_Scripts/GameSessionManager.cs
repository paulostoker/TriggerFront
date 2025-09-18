// _Scripts/GameSessionManager.cs
using UnityEngine;
using System.Collections.Generic;


public class GameSessionManager : MonoBehaviour
{

public bool IsPlayer1Attacker = true;
public bool SelectionsReady = false;
public List<int> SelectedAttackersIdx = new List<int>();
public List<int> SelectedDefendersIdx = new List<int>();


public void ResetSelections()
{
    SelectedAttackers.Clear();
    SelectedDefenders.Clear();
    SelectedAttackersIdx.Clear();
    SelectedDefendersIdx.Clear();
    SelectionsReady = false;
}
    public static GameSessionManager Instance { get; private set; }

    public List<FreelancerData> SelectedAttackers = new List<FreelancerData>();
    public List<FreelancerData> SelectedDefenders = new List<FreelancerData>();

    void Awake()
    {
        // Se já existe uma instância e não sou eu, me destruo.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        // Eu sou a única instância.
        Instance = this;
        // Me movo para a "cena" especial que sobrevive a transições.
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
{
    UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnActiveSceneChanged;
}
void OnDisable()
{
    UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnActiveSceneChanged;
}
void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene a, UnityEngine.SceneManagement.Scene b)
{
    Debug.Log($"[GSM][SceneChanged] {b.name} atkIdx={SelectedAttackersIdx.Count} defIdx={SelectedDefendersIdx.Count} ready={SelectionsReady}");
    var nm = Unity.Netcode.NetworkManager.Singleton;
    if (nm != null && nm.IsClient && (!SelectionsReady || SelectedAttackersIdx.Count != 5 || SelectedDefendersIdx.Count != 5))
    {
        var pnd = PlayerNetworkData.Instance != null ? PlayerNetworkData.Instance : UnityEngine.Object.FindFirstObjectByType<PlayerNetworkData>();
        if (pnd != null && nm.IsConnectedClient)
        {
            Debug.Log("[GSM] Requesting roster again");
            pnd.RequestRosterServerRpc();
        }
    }
}
    
    // Métodos de ajuda que já tínhamos
    public void AddAttacker(FreelancerData freelancer) { if (SelectedAttackers.Count < 5) SelectedAttackers.Add(freelancer); }
    public void AddDefender(FreelancerData freelancer) { if (SelectedDefenders.Count < 5) SelectedDefenders.Add(freelancer); }
    public FreelancerData UndoLastSelection() 
    {
        FreelancerData removed = null;
        if (SelectedDefenders.Count > 0) { removed = SelectedDefenders[SelectedDefenders.Count - 1]; SelectedDefenders.RemoveAt(SelectedDefenders.Count - 1); }
        else if (SelectedAttackers.Count > 0) { removed = SelectedAttackers[SelectedAttackers.Count - 1]; SelectedAttackers.RemoveAt(SelectedAttackers.Count - 1); }
        return removed;
    }

}