// _Scripts/ServiceLocator.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ServiceLocator : MonoBehaviour
{
    #region Singleton
    private static ServiceLocator instance;
    public static ServiceLocator Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<ServiceLocator>();
                if (instance == null)
                {
                    GameObject go = new GameObject("ServiceLocator");
                    instance = go.AddComponent<ServiceLocator>();
                }
            }
            return instance;
        }
    }
    #endregion

    #region Core Managers
    [Header("Core Managers")]
    public GameManager gameManager;
    public GridManager gridManager;
    public UIManager uiManager;
    public CameraManager cameraManager;
    public CombatManager combatManager;
    public MovementManager movementManager;
    public TurnManager turnManager;
    public PieceManager pieceManager;
    public CardManager cardManager;
    public DeckManager deckManager; 
    public AudioManager audioManager;
    public FreelancerManager freelancerManager;
    public BombManager bombManager;
    public EffectManager effectManager;
    public TechniqueManager techniqueManager;
    public CardSearchManager cardSearchManager;

    [Header("Turn Display")]
    public TurnDisplay turnDisplay;

    [Header("Shared Resources")]
    public Camera mainCamera;
    public Material movementHighlightMaterial; 
    public Material attackHighlightMaterial;   
    public Material supportHighlightMaterial;

    private Dictionary<System.Type, MonoBehaviour> services = new Dictionary<System.Type, MonoBehaviour>();
    private int turnsRemaining = 40;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        InitializeServices();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this)
        {
            services.Clear();
            instance = null;
        }
    }
    #endregion

    #region Scene Management
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"<color=orange>[ServiceLocator]</color> Nova cena '{scene.name}' carregada. Executando limpeza e reconexão...");
        ClearAllStaticEvents();
        InitializeServices();
        ConnectCriticalEvents();
    }

    private void ClearAllStaticEvents()
    {
        Debug.Log("<color=yellow>[ServiceLocator]</color> Limpando todos os eventos estáticos...");
        try { TurnManager.ResetStaticData(); } catch { }
        try { CombatManager.ResetStaticData(); } catch { }
        try { MovementManager.ResetStaticData(); } catch { }
        try { PieceManager.ResetStaticData(); } catch { }
        try { CardManager.ResetStaticData(); } catch { }
        try { BombManager.ResetStaticData(); } catch { }
        try { ActionState.ResetStaticData(); } catch { }
        Debug.Log("<color=green>[ServiceLocator]</color> Limpeza de eventos estáticos concluída");
    }

    private void InitializeServices()
    {
        Debug.Log("<color=blue>[ServiceLocator]</color> Inicializando serviços...");
        services.Clear();
        gameManager = FindFirstObjectByType<GameManager>();
        gridManager = FindFirstObjectByType<GridManager>();
        uiManager = FindFirstObjectByType<UIManager>();
        cameraManager = FindFirstObjectByType<CameraManager>();
        combatManager = FindFirstObjectByType<CombatManager>();
        movementManager = FindFirstObjectByType<MovementManager>();
        turnManager = FindFirstObjectByType<TurnManager>();
        pieceManager = FindFirstObjectByType<PieceManager>();
        cardManager = FindFirstObjectByType<CardManager>();
        deckManager = FindFirstObjectByType<DeckManager>(); 
        audioManager = FindFirstObjectByType<AudioManager>();
        turnDisplay = FindFirstObjectByType<TurnDisplay>();
        freelancerManager = FindFirstObjectByType<FreelancerManager>();
        bombManager = FindFirstObjectByType<BombManager>();
        effectManager = FindFirstObjectByType<EffectManager>();
        techniqueManager = FindFirstObjectByType<TechniqueManager>();
        techniqueManager = FindFirstObjectByType<TechniqueManager>(); 
        cardSearchManager = FindFirstObjectByType<CardSearchManager>(); 
        mainCamera = Camera.main;
        if (freelancerManager != null) 
            freelancerManager.FullReset();
        RegisterService(gameManager);
        RegisterService(gridManager);
        RegisterService(uiManager);
        RegisterService(cameraManager);
        RegisterService(combatManager);
        RegisterService(movementManager);
        RegisterService(turnManager);
        RegisterService(pieceManager);
        RegisterService(cardManager);
        RegisterService(deckManager); 
        RegisterService(audioManager);
        RegisterService(turnDisplay);
        RegisterService(freelancerManager);
        RegisterService(bombManager);
        RegisterService(effectManager);
        RegisterService(techniqueManager); 
        RegisterService(cardSearchManager);
        ValidateServices();
    }

    private void ConnectCriticalEvents()
    {
        Debug.Log("<color=cyan>[ServiceLocator]</color> Conectando eventos críticos...");
        if (gameManager != null && turnManager != null)
        {
            TurnManager.OnActionCompleted += gameManager.OnActionCompleted;
            Debug.Log("<color=green>[ServiceLocator]</color> GameManager conectado ao TurnManager.OnActionCompleted");
        }
        if (gameManager != null && combatManager != null)
        {
            CombatManager.OnAttackCompleted += gameManager.OnAttackCompleted;
            Debug.Log("<color=green>[ServiceLocator]</color> GameManager conectado ao CombatManager.OnAttackCompleted");
        }
        if (gameManager != null && movementManager != null)
        {
            MovementManager.OnMovementCompleted += gameManager.OnMovementCompleted;
            Debug.Log("<color=green>[ServiceLocator]</color> GameManager conectado ao MovementManager.OnMovementCompleted");
        }
        if (gameManager != null && cardManager != null)
        {
            CardManager.OnFreelancerCardSelectedForSetup += gameManager.HandleFreelancerCardSelectionForSetup;
            Debug.Log("<color=green>[ServiceLocator]</color> GameManager conectado ao CardManager.OnFreelancerCardSelectedForSetup");
        }
        Debug.Log("<color=green>[ServiceLocator]</color> Conexões críticas estabelecidas");
    }

    private void RegisterService<T>(T service) where T : MonoBehaviour
    {
        if (service != null)
            services[typeof(T)] = service;
    }

    private void ValidateServices()
    {
        if (gameManager == null) Debug.LogError("ServiceLocator: GameManager not found!");
        if (gridManager == null) Debug.LogError("ServiceLocator: GridManager not found!");
        if (uiManager == null) Debug.LogError("ServiceLocator: UIManager not found!");
        if (mainCamera == null) Debug.LogError("ServiceLocator: Main Camera not found!");
        if (cameraManager == null) Debug.LogWarning("ServiceLocator: CameraManager not found!");
        if (combatManager == null) Debug.LogWarning("ServiceLocator: CombatManager not found!");
        if (movementManager == null) Debug.LogWarning("ServiceLocator: MovementManager not found!");
        if (turnManager == null) Debug.LogWarning("ServiceLocator: TurnManager not found!");
        if (pieceManager == null) Debug.LogWarning("ServiceLocator: PieceManager not found!");
        if (cardManager == null) Debug.LogWarning("ServiceLocator: CardManager not found!");
        if (deckManager == null) Debug.LogWarning("ServiceLocator: DeckManager not found!");
        if (audioManager == null) Debug.LogWarning("ServiceLocator: AudioManager not found!");
        if (turnDisplay == null) Debug.LogWarning("ServiceLocator: TurnDisplay not found!");
        if (freelancerManager == null) Debug.LogWarning("ServiceLocator: FreelancerManager not found!");
        if (bombManager == null) Debug.LogError("ServiceLocator: BombManager not found!");
        if (effectManager == null) Debug.LogError("ServiceLocator: EffectManager not found!");
        if (techniqueManager == null) Debug.LogWarning("ServiceLocator: TechniqueManager not found!");
        if (cardSearchManager == null) Debug.LogWarning("ServiceLocator: CardSearchManager not found!");
        Debug.Log("<color=green>[ServiceLocator]</color> Initialized with all available services");
        
        Debug.Log("<color=green>[ServiceLocator]</color> Initialized with all available services");
        
    }
    #endregion

    #region Service Access
    public static T Get<T>() where T : MonoBehaviour
    {
        if (Instance.services.TryGetValue(typeof(T), out MonoBehaviour service))
            return service as T;
        return null;
    }

    public static GameManager Game => Instance.gameManager;
    public static GridManager Grid => Instance.gridManager;
    public static UIManager UI => Instance.uiManager;
    public static CameraManager CameraManager => Instance.cameraManager;
    public static CombatManager Combat => Instance.combatManager;
    public static MovementManager Movement => Instance.movementManager;
    public static TurnManager Turn => Instance.turnManager;
    public static PieceManager Pieces => Instance.pieceManager;
    public static CardManager Cards => Instance.cardManager;
    public static DeckManager Decks => Instance.deckManager; 
    public static Camera MainCamera => Instance.mainCamera;
    public static FreelancerManager Freelancers => Instance.freelancerManager;
    public static Material MovementHighlightMaterial => Instance.movementHighlightMaterial;
    public static Material AttackHighlightMaterial => Instance.attackHighlightMaterial;
    public static Material SupportHighlightMaterial => Instance.supportHighlightMaterial;
    public static AudioManager Audio => Instance.audioManager;
    public static TurnDisplay TurnDisplay => Instance.turnDisplay;
    public static BombManager Bomb => Instance.bombManager;
    public static EffectManager Effects => Instance.effectManager;
    public static TechniqueManager Techniques => Instance.techniqueManager; 
    public static CardSearchManager Search => Instance.cardSearchManager;
     

    public static bool IsAvailable<T>() where T : MonoBehaviour
    {
        return Get<T>() != null;
    }

    public static void RefreshServices()
    {
        if (Instance != null)
            Instance.InitializeServices();
    }
    #endregion

    #region Turn Control
    public static void UpdateTurnCounter(bool isPlayer1Turn)
    {
        if (Instance.turnDisplay != null)
            Instance.turnDisplay.UpdateTurnDisplay(isPlayer1Turn);
    }

    public static void DecrementTurn()
    {
        Instance.turnsRemaining--;
        if (Instance.turnDisplay != null)
            Instance.turnDisplay.SetTurnsRemaining(Instance.turnsRemaining);
        if (Instance.turnsRemaining <= 0)
            Debug.Log("<color=red>[ServiceLocator]</color> Turn limit reached! Game should end.");
    }

    public static void SetTurnsRemaining(int turns)
    {
        Instance.turnsRemaining = turns;
        if (Instance.turnDisplay != null)
            Instance.turnDisplay.SetTurnsRemaining(turns);
    }

    public static int GetTurnsRemaining() => Instance.turnsRemaining;
    #endregion
}