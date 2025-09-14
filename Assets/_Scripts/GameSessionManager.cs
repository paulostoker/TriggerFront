// _Scripts/GameSessionManager.cs
using UnityEngine;
using System.Collections.Generic;

public class GameSessionManager : MonoBehaviour
{
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
    public void ResetSelections() { SelectedAttackers.Clear(); SelectedDefenders.Clear(); }
}