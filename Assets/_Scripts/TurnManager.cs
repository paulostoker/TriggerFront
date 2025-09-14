// _Scripts/TurnManager.cs - Versão Corrigida com Reset de Eventos Estáticos
using UnityEngine;
using System;

public class TurnManager : MonoBehaviour
{
    [Header("Turn Indicators")]
    public GameObject turnIndicatorPrefab;
    
    private GameObject activeTurnIndicator;
    private GameObject currentActiveFreelancer;
    private string currentPlayerName;
    private int currentFreelancerIndex = -1;
    
    public static event Action<GameObject, int> OnTurnStarted;
    public static event Action<GameObject> OnTurnEnded;
    public static event Action<GameObject> OnTurnSkipped;
    public static event Action<bool> OnActionCompleted; // bool = isMove
    public static event Action<string> OnPlayerTurnChanged;
    
    void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        if (turnIndicatorPrefab == null) 
        {
            Debug.LogWarning("TurnManager: turnIndicatorPrefab not assigned - no visual indicators will be shown");
        }
        
        Debug.Log("<color=purple>[TurnManager]</color> Initialized successfully");
    }
    
    #region PUBLIC METHODS
    
    public void AnnounceFreelancerTurn(GameObject freelancerPiece, int freelancerIndex, string playerName)
    {
        if (freelancerPiece == null)
        {
            Debug.LogError("TurnManager: Tentativa de anunciar turno com operador nulo!");
            return;
        }
        
        DestroyCurrentIndicator();
        
        currentActiveFreelancer = freelancerPiece;
        currentFreelancerIndex = freelancerIndex;
        currentPlayerName = playerName;
        
        if (ServiceLocator.CameraManager != null)
        {
            ServiceLocator.CameraManager.FocusOnPiece(freelancerPiece);
        }
        
        CreateTurnIndicator(freelancerPiece);
        
        if (GameConfig.Instance.enableTurnLogs)
        {
            Debug.Log($"<color=purple>[TurnManager]</color> --- {playerName}'s Turn - Freelancer #{freelancerIndex + 1} ({freelancerPiece.name}) is active ---");
        }
        
        OnTurnStarted?.Invoke(freelancerPiece, freelancerIndex);
    }
    
    public void EndCurrentFreelancerTurn()
    {
        if (currentActiveFreelancer != null)
        {
            OnTurnEnded?.Invoke(currentActiveFreelancer);
            
            if (GameConfig.Instance.enableTurnLogs)
            {
                Debug.Log($"<color=purple>[TurnManager]</color> Turn ended for {currentActiveFreelancer.name}");
            }
        }
        
        DestroyCurrentIndicator();
        ClearCurrentTurnData();
    }
    
    public void SkipCurrentFreelancerTurn()
    {
        if (currentActiveFreelancer != null)
        {
            OnTurnSkipped?.Invoke(currentActiveFreelancer);
            
            if (GameConfig.Instance.enableTurnLogs)
            {
                Debug.Log($"<color=purple>[TurnManager]</color> {currentActiveFreelancer.name} skipped the rest of their turn");
            }
        }
        
        EndCurrentFreelancerTurn();
    }
    
    public void NotifyActionCompleted(bool isMove)
    {
        Debug.Log($"<color=purple>[TurnManager]</color> Action completed: {(isMove ? "Movement" : "Action")}");
        OnActionCompleted?.Invoke(isMove);
        
        if (GameConfig.Instance.enableTurnLogs)
        {
            string actionType = isMove ? "Movement" : "Action";
            Debug.Log($"<color=purple>[TurnManager]</color> {actionType} completed for {(currentActiveFreelancer != null ? currentActiveFreelancer.name : "unknown freelancer")}");
        }
    }
    
    public void NotifyPlayerChanged(string newPlayerName)
    {
        currentPlayerName = newPlayerName;
        OnPlayerTurnChanged?.Invoke(newPlayerName);
        
        if (GameConfig.Instance.enableTurnLogs)
        {
            Debug.Log($"<color=purple>[TurnManager]</color> Turn switched to {newPlayerName}");
        }
    }
    
    public void DestroyCurrentIndicator()
    {
        if (activeTurnIndicator != null)
        {
            Destroy(activeTurnIndicator);
            activeTurnIndicator = null;
        }
    }
    
    public void ClearAllTurnData()
    {
        DestroyCurrentIndicator();
        ClearCurrentTurnData();
        
        if (GameConfig.Instance.enableTurnLogs)
        {
            Debug.Log("<color=purple>[TurnManager]</color> All turn data cleared");
        }
    }
    
    #endregion
    
    #region PRIVATE METHODS
    
    private void CreateTurnIndicator(GameObject freelancerPiece)
    {
        if (turnIndicatorPrefab == null) return;
        
        Vector3 indicatorPosition = freelancerPiece.transform.position + GameConfig.Instance.turnIndicatorOffset;
        activeTurnIndicator = Instantiate(turnIndicatorPrefab, indicatorPosition, Quaternion.identity);
        activeTurnIndicator.transform.SetParent(freelancerPiece.transform);
        
        activeTurnIndicator.name = $"TurnIndicator_{freelancerPiece.name}";
    }
    
    private void ClearCurrentTurnData()
    {
        currentActiveFreelancer = null;
        currentFreelancerIndex = -1;
        currentPlayerName = null;
    }

    #endregion

    #region UTILITY METHODS
    
        public int GetTurnsRemaining()
    {
        // Este método busca a informação do ServiceLocator, mantendo a lógica centralizada.
        return ServiceLocator.GetTurnsRemaining();
    }
    
    public GameObject GetCurrentActiveFreelancer()
    {
        return currentActiveFreelancer;
    }
    
    public int GetCurrentFreelancerIndex()
    {
        return currentFreelancerIndex;
    }
    
    public string GetCurrentPlayerName()
    {
        return currentPlayerName;
    }
    
    public bool HasActiveTurn()
    {
        return currentActiveFreelancer != null;
    }
    
    public bool HasActiveIndicator()
    {
        return activeTurnIndicator != null;
    }
    
    #endregion
    
    #region STATIC EVENT CLEANUP
    
    public static void ResetStaticData()
    {
        OnTurnStarted = null;
        OnTurnEnded = null;
        OnTurnSkipped = null;
        OnActionCompleted = null;
        OnPlayerTurnChanged = null;
        
        Debug.Log("<color=purple>[TurnManager]</color> Static events cleared for new game session");
    }
    
    #endregion
    
    #region CLEANUP
    
    void OnDestroy()
    {
        ClearAllTurnData();
    }
    
    #endregion
}