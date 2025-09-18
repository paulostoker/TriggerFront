// _Scripts/GameManager.cs
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    #region Fields & Properties
    [Header("Map Data")]
    public MapEditorData currentMapData;

    [Header("Data Sources")]
    [Tooltip("Referência ao ScriptableObject que contém todos os freelancers disponíveis.")]
    public FreelancerDatabase freelancerDatabase;
    [Tooltip("Perfil de deck de cartas para o Jogador 1 (Atacantes).")]
    public PlayerDeckProfile player1CardDeckProfile;
    [Tooltip("Perfil de deck de cartas para o Jogador 2 (Defensores).")]
    public PlayerDeckProfile player2CardDeckProfile;

    [Header("Intro Animation")]
    public IntroAnimationManager introAnimationManager;

    [Header("End Game Animation")]
    public float endGamePivotScale = 0.8f;
    public float endGamePivotRotationSpeed = 10f;

    [Header("UI References")]
    public TurnDisplay turnDisplay;
    [Tooltip("Painel preto com um CanvasGroup para o fade-in inicial.")]
    public CanvasGroup fadeFromBlackPanel;

    [Header("Game Mode Settings")]
    [SerializeField] private bool devFallbackOnTimeout = false;
[SerializeField] private float devFallbackTimeout = 6f;
private bool _started;
    public bool isPlayer1Attacker = true;

    public List<FreelancerData> Player1FreelancerDeck { get; private set; }
    public List<FreelancerData> Player2FreelancerDeck { get; private set; }

    private InputManager inputManager;
    private IGameState currentState;
    public IGameState CurrentState => currentState;
    private bool isPlayer1Turn = true;
    private int turnsRemaining = 40;
    private int preparationPhaseCount = 0;
    #endregion

    #region Unity Lifecycle
    void Awake()
{
    GameSessionManager gameSession = GameSessionManager.Instance;

    if (gameSession != null && gameSession.SelectedAttackers.Count == 5 && gameSession.SelectedDefenders.Count == 5)
    {
        Player1FreelancerDeck = new List<FreelancerData>(gameSession.SelectedAttackers);
        Player2FreelancerDeck = new List<FreelancerData>(gameSession.SelectedDefenders);
        isPlayer1Attacker = gameSession.IsPlayer1Attacker;
    }
    else
    {
        Player1FreelancerDeck = new List<FreelancerData>();
        Player2FreelancerDeck = new List<FreelancerData>();
        DraftFreelancers();
    }

    SetupInputManager();
    if (player1CardDeckProfile == null || player2CardDeckProfile == null)
    {
        return;
    }
}




    private void Start()
{
    var net = Unity.Netcode.NetworkManager.Singleton;
    bool isNet = net != null && net.IsListening;

    if (!isNet)
    {
        if (!_started)
        {
            _started = true;
            StartCoroutine(InitializeGameWithIntro());
        }
        return;
    }

    PlayerNetworkData.OnRosterApplied -= HandleRosterApplied;
    PlayerNetworkData.OnRosterApplied += HandleRosterApplied;

    var gsm = GameSessionManager.Instance;
    bool hasRoster = gsm != null
        && gsm.SelectedAttackers != null && gsm.SelectedAttackers.Count > 0
        && gsm.SelectedDefenders != null && gsm.SelectedDefenders.Count > 0;

    if (hasRoster)
    {
        HandleRosterApplied();
    }
    else if (devFallbackOnTimeout)
    {
        StartCoroutine(DevFallbackTimer());
    }
}


    void Update()
    {
        
        currentState?.Execute();
    }

    void OnDestroy()
    {
        if (inputManager != null)
        {
            InputManager.OnPieceClicked -= OnPieceClicked;
            InputManager.OnTileClicked -= OnTileClicked;
            InputManager.OnGroundClicked -= OnGroundClicked;
            InputManager.OnEmptySpaceClicked -= OnEmptySpaceClicked;
        }
        PieceManager.OnAnyPieceKilled -= HandlePieceKilled;
        PlayerNetworkData.OnRosterApplied -= HandleRosterApplied;
    }
    #endregion

    #region Initialization
    private IEnumerator InitializeGameWithIntro()
    {
if (Unity.Netcode.NetworkManager.Singleton != null)
    yield return StartCoroutine(Network_ResolveRoster());

        StartCoroutine(FadeInFromBlack());
        ClearEditorObjects();
        if (ServiceLocator.Grid == null || ServiceLocator.UI == null || ServiceLocator.Freelancers == null)
        {
            yield break;
        }
        ServiceLocator.Freelancers.CreateFreelancerInstances(Player1FreelancerDeck, Player2FreelancerDeck);
        if (currentMapData != null)
            LoadGameMap();
        else
        {
            yield break;
        }
        yield return new WaitForEndOfFrame();
        if (ServiceLocator.Cards != null && !ServiceLocator.Cards.AreFreelancerCardsInitialized())
            ServiceLocator.Cards.InitializeFreelancerCards(Player1FreelancerDeck, Player2FreelancerDeck);
        yield return new WaitForEndOfFrame();
        if (introAnimationManager != null)
        {
            introAnimationManager.StartIntroAnimation();
            while (introAnimationManager.IsAnimationInProgress)
                yield return null;
        }
        else
        {
            yield return new WaitForSeconds(GameConfig.Instance.introAnimationDuration);
        }
        ChangeState(new SetupState(this));
        ServiceLocator.SetTurnsRemaining(turnsRemaining);
    }

    private IEnumerator FadeInFromBlack()
    {
        if (fadeFromBlackPanel == null) yield break;
        fadeFromBlackPanel.alpha = 1f;
        float duration = 3f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            fadeFromBlackPanel.alpha = 1f - (elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        fadeFromBlackPanel.alpha = 0f;
        fadeFromBlackPanel.interactable = false;
        fadeFromBlackPanel.blocksRaycasts = false;
    }

    private void SetupInputManager()
    {
        inputManager = GetComponent<InputManager>();
        if (inputManager == null) inputManager = gameObject.AddComponent<InputManager>();
        InputManager.OnPieceClicked += OnPieceClicked;
        InputManager.OnTileClicked += OnTileClicked;
        InputManager.OnGroundClicked += OnGroundClicked;
        InputManager.OnEmptySpaceClicked += OnEmptySpaceClicked;
    }

    private void DraftFreelancers()
    {
        if (freelancerDatabase == null || freelancerDatabase.allFreelancers.Count < GameConfig.Instance.totalFreelancers * 2)
        {
            return;
        }
        List<FreelancerData> availableFreelancers = new List<FreelancerData>(freelancerDatabase.allFreelancers);
        Player1FreelancerDeck.Clear();
        Player2FreelancerDeck.Clear();
        for (int i = 0; i < GameConfig.Instance.totalFreelancers; i++)
        {
            int randomIndex = Random.Range(0, availableFreelancers.Count);
            Player1FreelancerDeck.Add(availableFreelancers[randomIndex]);
            availableFreelancers.RemoveAt(randomIndex);
        }
        for (int i = 0; i < GameConfig.Instance.totalFreelancers; i++)
        {
            int randomIndex = Random.Range(0, availableFreelancers.Count);
            Player2FreelancerDeck.Add(availableFreelancers[randomIndex]);
            availableFreelancers.RemoveAt(randomIndex);
        }
    }

    private void ClearEditorObjects()
    {
        GameObject editorPreview = GameObject.Find("Map_EditorPreview");
        if (editorPreview != null)
            Destroy(editorPreview);
    }

    private void LoadGameMap()
    {
        if (currentMapData == null)
        {
            return;
        }
        GameObject pivotObject = new GameObject("Pivot");
        pivotObject.transform.position = new Vector3(9.5f, 0f, 9.5f);
        if (introAnimationManager != null)
            introAnimationManager.RegisterPivot(pivotObject.transform);
        GameObject existingMap = GameObject.Find("Map");
        if (existingMap != null)
            Destroy(existingMap);
        GameObject mapParent = CreateGameMapStructure();
        mapParent.transform.SetParent(pivotObject.transform);
        GameObject piecesParent = new GameObject("Pieces");
        piecesParent.transform.SetParent(pivotObject.transform);
        if (ServiceLocator.Grid != null)
        {
            ServiceLocator.Grid.GenerateGridFromMapEditorData(currentMapData);
            SpawnPiecesFromMapData(currentMapData, piecesParent.transform);
        }
    }

    GameObject CreateGameMapStructure()
    {
        GameObject mapContainer = new GameObject("Map");
        GameObject extrasContainer = new GameObject("Extras");
        extrasContainer.transform.SetParent(mapContainer.transform);
        return mapContainer;
    }

    void SpawnPiecesFromMapData(MapEditorData mapData, Transform piecesParent)
{
    if (ServiceLocator.Pieces == null) return;
    List<Vector2Int> player1Spawns = mapData.GetPlayer1SpawnPoints();
    List<Vector2Int> player2Spawns = mapData.GetPlayer2SpawnPoints();
    ServiceLocator.Pieces.SpawnPiecesFromSpawnPoints(Player1FreelancerDeck, Player2FreelancerDeck, player1Spawns, player2Spawns, piecesParent);
    StartCoroutine(CorrectPiecePositionsAfterSpawn());
    if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer)
    {
        if (ServiceLocator.Combat != null) ServiceLocator.Combat.Server_PopulateSyncedFreelancers();
    }
}


    private IEnumerator CorrectPiecePositionsAfterSpawn()
    {
        yield return new WaitForEndOfFrame();
        if (ServiceLocator.Pieces != null)
            ServiceLocator.Pieces.UpdateAllPiecePositionsWithRaycast();
    }
    #endregion

    #region Game State Management
    public void ChangeState(IGameState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public void EndPreparation() => ChangeState(new ActionState(this));

    public void EndGame(string winner)
    {
        if (inputManager != null) inputManager.DisableCameraControls();
        if (ServiceLocator.CameraManager != null) ServiceLocator.CameraManager.SetupForEndGame();
        if (ServiceLocator.Turn != null) ServiceLocator.Turn.DestroyCurrentIndicator();
        if (ServiceLocator.UI != null) ServiceLocator.UI.ShowGameOverUI(winner);
        StartCoroutine(EndGameAnimationCoroutine());
        if (ServiceLocator.Audio != null) ServiceLocator.Audio.PlayVictoryMusic();
        this.enabled = false;
    }

    private IEnumerator EndGameAnimationCoroutine()
    {
        GameObject pivotObject = GameObject.Find("Pivot");
        if (pivotObject == null) yield break;
        Transform pivotTransform = pivotObject.transform;
        Vector3 startScale = pivotTransform.localScale;
        Vector3 targetScale = Vector3.one * endGamePivotScale;
        float duration = 1.0f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            pivotTransform.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        pivotTransform.localScale = targetScale;
        while (true)
        {
            pivotTransform.Rotate(0, endGamePivotRotationSpeed * Time.deltaTime, 0);
            yield return null;
        }
    }

    private void CheckForGameOver()
    {
        if (ServiceLocator.Freelancers == null || ServiceLocator.Bomb == null) return;
        bool isBombPlanted = ServiceLocator.Bomb.IsPlanted();
        bool allAttackersDead = ServiceLocator.Freelancers.AreAllFreelancersDeadOnTeam(isPlayer1Attacker);
        bool allDefendersDead = ServiceLocator.Freelancers.AreAllFreelancersDeadOnTeam(!isPlayer1Attacker);
        if (isBombPlanted)
        {
            if (allDefendersDead)
                EndGame("Attackers");
        }
        else
        {
            if (allAttackersDead)
                EndGame("Defenders");
            else if (allDefendersDead)
                EndGame("Attackers");
        }
    }
    #endregion

    #region Input Handling
    private void OnPieceClicked(GameObject piece)
    {
        // --- INÍCIO DA ADIÇÃO ---
        if (ServiceLocator.Techniques.IsInTechniqueMode)
        {
            Tile targetTile = ServiceLocator.Grid.GetTileUnderPiece(piece);
            ServiceLocator.Techniques.HandleTargetSelection(targetTile, piece);
            return;
        }
        // --- FIM DA ADIÇÃO ---
        ProcessPieceClick(piece);
    }

    private void OnTileClicked(Tile tile)
    {
        Debug.Log($"<color=yellow>[INVESTIGAÇÃO]</color> Frame: {Time.frameCount} - GameManager.OnTileClicked RECEBIDO para o tile {tile.name}.");
        if (ServiceLocator.Techniques.IsInTechniqueMode)
        {
            ServiceLocator.Techniques.HandleTargetSelection(tile, null);
            return;
        }
        ProcessTileClick(tile);
    }
    private void OnGroundClicked(Vector3 position)
    {
        // --- INÍCIO DA ADIÇÃO ---
        if (ServiceLocator.Techniques.IsInTechniqueMode)
        {
            ServiceLocator.Techniques.CancelTechniqueMode();
            return;
        }
        // --- FIM DA ADIÇÃO ---
        ProcessGroundClick();
    }
    private void OnEmptySpaceClicked() 
    {
        // --- INÍCIO DA ADIÇÃO ---
        if (ServiceLocator.Techniques.IsInTechniqueMode)
        {
            ServiceLocator.Techniques.CancelTechniqueMode();
            return;
        }
        // --- FIM DA ADIÇÃO ---
        ProcessGroundClick();
    }

    private void ProcessPieceClick(GameObject piece)
{
    if (currentState == null || piece == null) return;

    if (ServiceLocator.Cards != null && ServiceLocator.Cards.IsInEnergyEquipMode())
    {
        return;
    }
    if (ServiceLocator.Cards != null && ServiceLocator.Cards.IsInAllySelectionMode())
    {
        return;
    }
    if (currentState is ActionState actionStateCheck && actionStateCheck.IsSkillModeActive)
        ServiceLocator.Cards.EnterSkillMode(false);
        
    if (currentState is SetupState)
    {
        bool isAttackerPiece = (IsPieceOnCurrentTeam(piece) && IsAttackerTurn());
        if (isAttackerPiece)
        {
            ServiceLocator.Bomb.AssignBombCarrier(piece);
            ChangeState(new PreparationState(this));
        }
        return;
    }
    if (currentState is PreparationState)
    {
        HandlePreparationPieceClick(piece);
    }
    else if (currentState is ActionState actionState)
    {
        if (actionState.IsProcessingAction) return;
        if (ServiceLocator.Combat != null && ServiceLocator.Combat.IsInAttackMode())
        {
            // --- INÍCIO DA ALTERAÇÃO ---
            // Adicionamos uma verificação: se a "peça" clicada for, na verdade, um SpawnedEffect...
            if (piece.GetComponent<SpawnedEffect>() != null)
            {
                // ...então a tratamos imediatamente como um alvo válido e atacamos, ignorando a verificação de time.
                Tile pieceTile = ServiceLocator.Grid?.GetTileUnderPiece(piece);
                if (pieceTile != null && ServiceLocator.Combat.IsValidAttackTarget(pieceTile))
                {
                    ExecuteAttackSafe(ServiceLocator.Pieces?.GetSelectedPiece(), piece);
                    return; // Importante: 'return' para não executar o resto do código e cancelar.
                }
            }
            // --- FIM DA ALTERAÇÃO ---

            // Se não for um SpawnedEffect, a lógica original para peças normais continua.
            if (!IsPieceOnCurrentTeam(piece))
            {
                Tile pieceTile = ServiceLocator.Grid?.GetTileUnderPiece(piece);
                if (pieceTile != null && ServiceLocator.Combat.IsValidAttackTarget(pieceTile))
                {
                    ExecuteAttackSafe(ServiceLocator.Pieces?.GetSelectedPiece(), piece);
                    return;
                }
            }
            CancelCombatMode();
            return;
        }
        if (ServiceLocator.Movement != null && ServiceLocator.Movement.IsInMovementMode())
        {
            CancelMovementMode();
            return;
        }
        if (IsCurrentPlayerPiece(piece))
            SelectPiece(piece);
        else
            DeselectPiece();
    }
}

    private void ProcessTileClick(Tile tile)
    {
        if (tile == null) return;

        if (ServiceLocator.Cards != null && ServiceLocator.Cards.IsInEnergyEquipMode())
        {
            ServiceLocator.Cards.ExitEnergyEquipMode();
            return;
        }

        if (ServiceLocator.Cards != null && ServiceLocator.Cards.IsInAllySelectionMode())
        {
            ServiceLocator.Cards.ExitAllySelectionMode();
            return;
        }
        
        if (currentState is ActionState actionStateCheck && actionStateCheck.IsSkillModeActive)
        {
            ServiceLocator.Cards.EnterSkillMode(false);
            return;
        }
        
        if (!(currentState is ActionState actionState) || actionState.IsProcessingAction)
            return;
            
        if (ServiceLocator.Combat != null && ServiceLocator.Combat.IsInAttackMode())
        {
            HandleCombatTileClickSafe(tile);
            return;
        }
        
        if (ServiceLocator.Movement != null && ServiceLocator.Movement.IsInMovementMode())
        {
            HandleMovementTileClickSafe(tile);
            return;
        }
        
        DeselectPiece();
    }
    private void ProcessGroundClick()
    {
        if (ServiceLocator.Cards != null && ServiceLocator.Cards.IsInEnergyEquipMode())
        {
            ServiceLocator.Cards.ExitEnergyEquipMode();
            return;
        }
        if (ServiceLocator.Cards != null && ServiceLocator.Cards.IsInAllySelectionMode())
        {
            ServiceLocator.Cards.ExitAllySelectionMode();
            return;
        }
        if (currentState is ActionState actionStateCheck && actionStateCheck.IsSkillModeActive)
        {
            ServiceLocator.Cards.EnterSkillMode(false);
            return;
        }
        if (currentState is PreparationState)
        {
            CancelEquipMode();
        }
        else if (currentState is ActionState)
        {
            if (ServiceLocator.Combat != null && ServiceLocator.Combat.IsInAttackMode())
                CancelCombatMode();
            else if (ServiceLocator.Movement != null && ServiceLocator.Movement.IsInMovementMode())
                CancelMovementMode();
            else
                DeselectPiece();
        }
    }

    private void HandlePreparationPieceClick(GameObject piece)
    {
        if (ServiceLocator.Cards != null && ServiceLocator.Cards.IsInEquipMode())
        {
            if (IsPieceOnCurrentTeam(piece))
                ServiceLocator.Cards.TryEquipSelectedCard(piece, isPlayer1Turn);
        }
    }

    private void HandleCombatTileClickSafe(Tile tile)
{
    Debug.Log($"[DEBUG 1] Método 'HandleCombatTileClickSafe' iniciado para o tile '{tile.name}'.");

    if (ServiceLocator.Combat == null) 
    {
        Debug.LogError("[DEBUG FALHA] O CombatManager não foi encontrado (ServiceLocator.Combat is null).");
        CancelCombatMode(); 
        return;
    }
    
    // VERIFICAÇÃO A: O tile clicado é considerado um alvo válido pelo CombatManager?
    if (!ServiceLocator.Combat.IsValidAttackTarget(tile)) 
    {
        Debug.LogWarning($"[DEBUG FALHA A] O tile '{tile.name}' NÃO é um alvo de ataque válido na lista do CombatManager. Cancelando.");
        CancelCombatMode(); 
        return; 
    }
    Debug.Log("<color=green>[DEBUG SUCESSO A]</color> O tile é um alvo de ataque válido.");

    // VERIFICAÇÃO B: Existe uma peça atacante selecionada?
    GameObject selectedPiece = ServiceLocator.Pieces?.GetSelectedPiece();
    if (selectedPiece == null) 
    {
        Debug.LogWarning("[DEBUG FALHA B] Não há nenhuma peça atacante selecionada. Cancelando.");
        CancelCombatMode(); 
        return; 
    }
    Debug.Log($"<color=green>[DEBUG SUCESSO B]</color> Peça atacante é '{selectedPiece.name}'.");

    // VERIFICAÇÃO C: O que o GridManager ENCONTRA neste tile? (Este é o teste mais importante)
    GameObject targetObject = ServiceLocator.Grid?.GetObjectAtTile(tile.GetGridPosition());

    if (targetObject == null)
    {
        Debug.LogError($"[DEBUG FALHA C] Grid.GetObjectAtTile() retornou NULL para a posição {tile.GetGridPosition()}. Nenhum objeto foi encontrado no tile. Verifique as LAYERS do prefab do muro! Cancelando.");
        CancelCombatMode();
        return;
    }
    Debug.Log($"<color=cyan>[DEBUG INFORMAÇÃO C]</color> Grid.GetObjectAtTile() encontrou o objeto: '{targetObject.name}'.");

    // VERIFICAÇÃO D: Vamos analisar o objeto que foi encontrado.
    if (targetObject != null)
    {
        bool isEnemy = !IsPieceOnCurrentTeam(targetObject);
        bool isSpawnedEffect = targetObject.GetComponent<SpawnedEffect>() != null;

        Debug.Log($"[DEBUG ANÁLISE D] Analisando '{targetObject.name}': É inimigo? -> {isEnemy}. É um SpawnedEffect? -> {isSpawnedEffect}.");

        if (isEnemy || isSpawnedEffect)
        {
            Debug.Log("<color=lime>[DEBUG FINAL] CONDIÇÕES ATENDIDAS! EXECUTANDO ATAQUE...</color>");
            ExecuteAttackSafe(selectedPiece, targetObject);
            return;
        }
        else
        {
            Debug.LogWarning($"[DEBUG FALHA FINAL] O objeto '{targetObject.name}' foi encontrado, mas não passou na verificação final (não é inimigo nem SpawnedEffect). Cancelando.");
        }
    }
    
    // Se chegou até aqui, algo deu errado.
    Debug.LogError("[DEBUG GERAL] A lógica chegou ao fim do método sem executar o ataque. Cancelando.");
    CancelCombatMode();
}

    private void HandleMovementTileClickSafe(Tile tile)
    {
        if (ServiceLocator.Movement == null) { CancelMovementMode(); return; }
        if (!ServiceLocator.Movement.IsValidMovementTarget(tile)) { CancelMovementMode(); return; }
        GameObject selectedPiece = ServiceLocator.Pieces?.GetSelectedPiece();
        if (selectedPiece == null) { CancelMovementMode(); return; }
        ExecuteMovementSafe(selectedPiece, tile);
    }

    public void HandlePreparationPhaseClick() { }
    public void HandleActionPhaseClick() { }
    #endregion

    #region Movement & Combat
    public void EnterAttackMode()
    {
        if (ServiceLocator.Pieces == null || ServiceLocator.Combat == null) return;
        GameObject selectedPiece = ServiceLocator.Pieces.GetSelectedPiece();
        if (selectedPiece == null) return;
        if (ServiceLocator.UI != null) ServiceLocator.UI.HideActionMenu();
        ServiceLocator.Combat.StartAttackMode(selectedPiece, IsValidEnemyTarget);
    }

    public void EnterMoveMode()
    {
        ActionState actionState = currentState as ActionState;
        if (actionState == null) return;
        GameObject selectedPiece = ServiceLocator.Pieces.GetSelectedPiece();
        if (selectedPiece == null) return;
        if (ServiceLocator.UI != null) ServiceLocator.UI.HideActionMenu();
        int moveRange = 0;
        if (actionState.IsInWidePeekMode())
        {
            moveRange = actionState.GetRemainingWidePeekMovement();
        }
        else
        {
            FreelancerData opData = ServiceLocator.Freelancers.GetFreelancerData(selectedPiece);
            moveRange = (opData != null ? opData.baseMovement : 0) + ServiceLocator.Effects.GetStatModifier(selectedPiece, ModifierType.Movement);
        }
        ServiceLocator.Movement.StartMovementMode(selectedPiece, moveRange, ServiceLocator.Pieces.GetAllOccupiedTiles);
    }

     private void ExecuteAttackSafe(GameObject attacker, GameObject target)
    {
        if (attacker == null || target == null) { CancelCombatMode(); return; }
        var actionState = currentState as ActionState;
        if (actionState != null) actionState.IsProcessingAction = true;
        
        ServiceLocator.Combat.ExecuteAttack(attacker, target, (success) => {
            if (actionState != null) 
            {
                actionState.IsProcessingAction = false;
            }
        });
    }

    private void ExecuteMovementSafe(GameObject piece, Tile targetTile)
    {
        if (piece == null || targetTile == null) { CancelMovementMode(); return; }
        var actionState = currentState as ActionState;
        if (actionState != null) actionState.IsProcessingAction = true;
        ServiceLocator.Movement.ExecuteMovement(piece, targetTile, (success, distance) => {
            if (actionState != null)
            {
                actionState.IsProcessingAction = false;
                if (success && actionState.IsInWidePeekMode())
                    actionState.RegisterWidePeekMove(distance);
            }
        });
    }

    private void CancelCombatMode()
    {
        if (ServiceLocator.Combat != null) ServiceLocator.Combat.CancelAttackMode();
        if (ServiceLocator.Pieces != null)
        {
            GameObject selectedPiece = ServiceLocator.Pieces.GetSelectedPiece();
            if (selectedPiece != null) SelectPiece(selectedPiece);
        }
    }

    private void CancelMovementMode()
    {
        if (ServiceLocator.Movement != null) ServiceLocator.Movement.CancelMovementMode();
        if (ServiceLocator.Pieces != null)
        {
            GameObject selectedPiece = ServiceLocator.Pieces.GetSelectedPiece();
            if (selectedPiece != null) SelectPiece(selectedPiece);
        }
    }

    private bool IsValidEnemyTarget(GameObject piece) => !IsPieceOnCurrentTeam(piece);

    public void OnAttackCompleted(GameObject attacker, GameObject target, int damage)
    {
        ServiceLocator.Bomb.CancelDefuseIfDefuserActs(attacker);
    }

    public void OnMovementCompleted(GameObject piece)
    {
        ServiceLocator.Bomb.CancelDefuseIfDefuserActs(piece);
    }
    
    #endregion

    #region Turn Management
    public void SwitchPlayerAndEnterPreparation()
    {
        if (ServiceLocator.Bomb.GetCurrentBombState() == BombManager.BombState.Planting)
            ServiceLocator.Bomb.ConfirmPlant();
        isPlayer1Turn = !isPlayer1Turn;
        turnsRemaining--;
        ServiceLocator.DecrementTurn();
        if (turnsRemaining <= 0)
        {
            if (!ServiceLocator.Bomb.IsPlanted())
                EndGame("Defenders");
            return;
        }
        bool isCurrentTeamAllDead = ServiceLocator.Freelancers.AreAllFreelancersDeadOnTeam(isPlayer1Turn);
        bool isCurrentTeamAttacker = (isPlayer1Turn && isPlayer1Attacker) || (!isPlayer1Turn && !isPlayer1Attacker);
        if (ServiceLocator.Bomb.IsPlanted() && isCurrentTeamAllDead && isCurrentTeamAttacker)
        {
            SwitchPlayerAndEnterPreparation();
            return;
        }
        ServiceLocator.UpdateTurnCounter(isPlayer1Turn);
        ChangeState(new PreparationState(this));
    }

    public void AnnounceActiveTurn(int freelancerIndex)
    {
        if (ServiceLocator.Pieces == null || ServiceLocator.Turn == null) return;
        GameObject activePiece = ServiceLocator.Pieces.GetActivePiece(freelancerIndex, isPlayer1Turn);
        if (activePiece != null)
            ServiceLocator.Turn.AnnounceFreelancerTurn(activePiece, freelancerIndex, GetCurrentPlayerName());
    }

 public void SkipFreelancerTurn()
    {
        if (currentState is ActionState actionState)
        {
            actionState.SkipCurrentFreelancerTurn();
        }
    }

    public void OnActionCompleted(bool isMove)
    {
        if (currentState is ActionState actionState)
        {
            if (isMove)
            {
                actionState.SetMoved();
            }
            else
            {
                var activeFreelancer = actionState.GetActiveFreelancer();
                if(activeFreelancer != null)
                {
                    ServiceLocator.Freelancers.ConsumeActionCharge(activeFreelancer);
                }
            }
            
            actionState.IsProcessingAction = false;

            if (!actionState.CanMove() && !actionState.CanAct())
            {
                actionState.EndFreelancerTurn();
            }
            else
            {
                if (ServiceLocator.Pieces != null)
                {
                    GameObject selectedPiece = ServiceLocator.Pieces.GetSelectedPiece();
                    if (selectedPiece != null) SelectPiece(selectedPiece);
                }
            }
        }
    }

    public void DestroyActiveIndicator()
    {
        if (ServiceLocator.Turn != null) ServiceLocator.Turn.DestroyCurrentIndicator();
    }
    
    #endregion

    #region Camera Management
    public void ResetCameraForPreparation()
    {
        if (ServiceLocator.CameraManager != null)
            ServiceLocator.CameraManager.ResetForPreparation(isPlayer1Turn);
    }

    public void SetupCameraForAction()
    {
        if (ServiceLocator.CameraManager != null)
            ServiceLocator.CameraManager.SetupForAction();
    }

    public void FocusCameraOnPiece(GameObject piece)
    {
        if (ServiceLocator.CameraManager != null)
            ServiceLocator.CameraManager.FocusOnPiece(piece);
    }
    #endregion

    #region Game Actions
 public void EnterActionMode()
    {
        GameObject selectedPiece = ServiceLocator.Pieces.GetSelectedPiece();
        if (selectedPiece == null) return;
        
        // Escondemos o menu principal antes de mostrar o sub-menu
        ServiceLocator.UI.HideActionMenu();

        // Verificamos as condições para plantar/desarmar a bomba
        bool canPlant = ServiceLocator.Bomb.CanFreelancerPlant(selectedPiece);
        bool canDefuse = ServiceLocator.Bomb.CanFreelancerDefuse(selectedPiece);
        
        // Chamamos o sub-menu, agora passando a peça selecionada
        ServiceLocator.UI.ShowActionSubMenu(selectedPiece.transform.position, canPlant, canDefuse, selectedPiece);
    }

    // --- MÉTODOS ATUALIZADOS ---
    public void EnterTechniqueMode(int techniqueIndex)
    {
        ServiceLocator.UI.HideActionMenu();
        GameObject piece = ServiceLocator.Pieces.GetSelectedPiece();
        if (piece == null) return;
        
        FreelancerData data = ServiceLocator.Freelancers.GetFreelancerData(piece);
        if (data != null && data.techniques.Count > techniqueIndex)
        {
            TechniqueData techData = data.techniques[techniqueIndex];
            ServiceLocator.Techniques.StartTechniqueMode(piece, techData);
        }
    }

    public void EnterUltimateMode()
    {
        ServiceLocator.UI.HideActionMenu();
        GameObject piece = ServiceLocator.Pieces.GetSelectedPiece();
        if (piece == null) return;

        FreelancerData data = ServiceLocator.Freelancers.GetFreelancerData(piece);
        if (data != null && data.ultimate != null)
        {
            ServiceLocator.Techniques.StartTechniqueMode(piece, data.ultimate);
        }
    }
    // --- FIM DA ATUALIZAÇÃO ---

    public void PlantBomb()
    {
        GameObject selectedPiece = ServiceLocator.Pieces.GetSelectedPiece();
        if (selectedPiece == null) return;
        ServiceLocator.Bomb.StartPlanting(selectedPiece);
        ServiceLocator.UI.HideActionSubMenu();
        if (currentState is ActionState actionState) actionState.ForceCompleteTurn();
    }

    public void DefuseBomb()
    {
        GameObject selectedPiece = ServiceLocator.Pieces.GetSelectedPiece();
        if (selectedPiece == null) return;
        ServiceLocator.Bomb.StartDefusing(selectedPiece);
        ServiceLocator.UI.HideActionSubMenu();
        if (currentState is ActionState actionState) actionState.ForceCompleteTurn();
    }

    public void DrawCardsForCurrentPlayer(int amount)
    {
        if (ServiceLocator.Cards != null)
            ServiceLocator.Cards.DrawCardsForCurrentPlayer(amount, isPlayer1Turn);
    }

    public void CancelEquipMode()
    {
        if (ServiceLocator.Cards != null)
            ServiceLocator.Cards.ExitEquipMode();
    }

    public void SelectPiece(GameObject piece)
    {
        if (currentState is ActionState actionState && ServiceLocator.Pieces != null)
        {
            ServiceLocator.Pieces.SelectPiece(piece,
                () => ServiceLocator.Freelancers.CanFreelancerMove(piece),
                () => ServiceLocator.Freelancers.CanFreelancerAct(piece),
                () => ServiceLocator.Freelancers.CanFreelancerUseSkill(piece));
        }
    }

    public void DeselectPiece()
    {
        if (ServiceLocator.Pieces != null)
            ServiceLocator.Pieces.DeselectCurrentPiece();
    }

    public void HandleFreelancerCardSelectionForSetup(GameObject piece)
    {
        if (currentState is SetupState)
        {
            ServiceLocator.Bomb.AssignBombCarrier(piece);
            ChangeState(new PreparationState(this));
        }
    }
    void OnEnable()
    {
        PieceManager.OnAnyPieceKilled += HandlePieceKilled;
    }

    void OnDisable()
    {
        PieceManager.OnAnyPieceKilled -= HandlePieceKilled;
    }
    private void HandlePieceKilled()
    {
        StartCoroutine(CheckForGameOverAfterDelay());
    }
    private IEnumerator CheckForGameOverAfterDelay()
    {
        yield return new WaitForEndOfFrame();
        CheckForGameOver();
    }
    #endregion

    #region Utility Methods
    public void StartNewRound() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    public bool IsAttackerTurn() => (isPlayer1Turn && isPlayer1Attacker) || (!isPlayer1Turn && !isPlayer1Attacker);
    public bool IsPlayer1Turn() => isPlayer1Turn;
    public GameObject GetActivePiece(int freelancerIndex) => ServiceLocator.Pieces?.GetActivePiece(freelancerIndex, isPlayer1Turn);
    public string GetCurrentPlayerName() => isPlayer1Turn ? "Attackers" : "Defenders";
    public bool IsPieceOnCurrentTeam(GameObject piece) => ServiceLocator.Pieces?.IsPieceOnTeam(piece, isPlayer1Turn) ?? false;
    public bool IsCurrentPlayerPiece(GameObject piece)
    {
        if (currentState is ActionState actionState)
            return piece == GetActivePiece(actionState.GetActiveFreelancerIndex());
        return false;
    }
    public List<FreelancerData> GetPlayer1FreelancerDeck() => new List<FreelancerData>(Player1FreelancerDeck);
    public List<FreelancerData> GetPlayer2FreelancerDeck() => new List<FreelancerData>(Player2FreelancerDeck);
    public bool AreFreelancerDecksValid()
    {
        return Player1FreelancerDeck != null && Player1FreelancerDeck.Count == GameConfig.Instance.totalFreelancers &&
               Player2FreelancerDeck != null && Player2FreelancerDeck.Count == GameConfig.Instance.totalFreelancers;
    }

    private void InitializeFreelancerCardsAfterSpawn()
    {
        if (ServiceLocator.Cards == null) return;
        StartCoroutine(InitializeFreelancerCardsCoroutine());
    }

    private System.Collections.IEnumerator InitializeFreelancerCardsCoroutine()
    {
        yield return new WaitForEndOfFrame();
        if (ServiceLocator.Cards != null && Player1FreelancerDeck != null && Player2FreelancerDeck != null)
            ServiceLocator.Cards.InitializeFreelancerCards(Player1FreelancerDeck, Player2FreelancerDeck);
    }
    public void Network_ApplyRosterAndContinue(List<FreelancerData> p1, List<FreelancerData> p2, bool isP1)
    {
        Player1FreelancerDeck = p1;
        Player2FreelancerDeck = p2;
        isPlayer1Attacker = isP1;
        SetupInputManager();
        if (player1CardDeckProfile == null || player2CardDeckProfile == null)
        {
            return;
        }
    }
    IEnumerator Network_WaitForRosterThenBuildDecks()
    {
        var nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm == null) yield break;
        if (!nm.IsClient) yield break;

        var session = GameSessionManager.Instance;
        while (session == null) { yield return null; session = GameSessionManager.Instance; }

        float t = 0f;
        while (!session.SelectionsReady && t < 5f)
        {
            t += UnityEngine.Time.deltaTime;
            yield return null;
        }

        if (session.SelectedAttackers.Count != 5 && session.SelectedAttackersIdx.Count == 5 && freelancerDatabase != null)
        {
            session.SelectedAttackers.Clear();
            session.SelectedDefenders.Clear();
            for (int i = 0; i < session.SelectedAttackersIdx.Count; i++)
            {
                int idx = session.SelectedAttackersIdx[i];
                if (idx >= 0 && idx < freelancerDatabase.allFreelancers.Count)
                    session.SelectedAttackers.Add(freelancerDatabase.allFreelancers[idx]);
            }
            for (int i = 0; i < session.SelectedDefendersIdx.Count; i++)
            {
                int idx = session.SelectedDefendersIdx[i];
                if (idx >= 0 && idx < freelancerDatabase.allFreelancers.Count)
                    session.SelectedDefenders.Add(freelancerDatabase.allFreelancers[idx]);
            }
        }

        if (session.SelectedAttackers.Count == 5 && session.SelectedDefenders.Count == 5)
        {
            Player1FreelancerDeck = new List<FreelancerData>(session.SelectedAttackers);
            Player2FreelancerDeck = new List<FreelancerData>(session.SelectedDefenders);
            isPlayer1Attacker = session.IsPlayer1Attacker;
        }
    }
    System.Collections.IEnumerator Network_ResolveRoster()
    {
        var nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm == null) yield break;

        var session = GameSessionManager.Instance;
        while (session == null) { yield return null; session = GameSessionManager.Instance; }

        float t = 0f;
        while (!session.SelectionsReady && t < 0.5f)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (!session.SelectionsReady)
        {
            var pnd = PlayerNetworkData.Instance != null ? PlayerNetworkData.Instance : UnityEngine.Object.FindFirstObjectByType<PlayerNetworkData>();
            if (pnd != null && nm.IsClient) pnd.RequestRosterServerRpc();
            t = 0f;
            while (!session.SelectionsReady && t < 5f)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        if (session.SelectedAttackers.Count != 5 && session.SelectedAttackersIdx.Count == 5 && freelancerDatabase != null)
        {
            session.SelectedAttackers.Clear();
            session.SelectedDefenders.Clear();
            for (int i = 0; i < session.SelectedAttackersIdx.Count; i++)
            {
                int idx = session.SelectedAttackersIdx[i];
                if (idx >= 0 && idx < freelancerDatabase.allFreelancers.Count)
                    session.SelectedAttackers.Add(freelancerDatabase.allFreelancers[idx]);
            }
            for (int i = 0; i < session.SelectedDefendersIdx.Count; i++)
            {
                int idx = session.SelectedDefendersIdx[i];
                if (idx >= 0 && idx < freelancerDatabase.allFreelancers.Count)
                    session.SelectedDefenders.Add(freelancerDatabase.allFreelancers[idx]);
            }
        }

        if (session.SelectedAttackers.Count == 5 && session.SelectedDefenders.Count == 5)
        {
            Player1FreelancerDeck = new List<FreelancerData>(session.SelectedAttackers);
            Player2FreelancerDeck = new List<FreelancerData>(session.SelectedDefenders);
            isPlayer1Attacker = session.IsPlayer1Attacker;
        }
    }
private void HandleRosterApplied()
{
    if (_started) return;
    _started = true;
    StartCoroutine(InitializeGameWithIntro());
}

private System.Collections.IEnumerator DevFallbackTimer()
{
    yield return new UnityEngine.WaitForSecondsRealtime(devFallbackTimeout);
    if (_started) yield break;
    _started = true;
    StartCoroutine(InitializeGameWithIntro());
}



    #endregion

    #region Event Callbacks
    private void OnAttackCancelled() { }
    private void OnMovementCancelled() { }
    private void OnCardEquipped(CardData card, GameObject piece, GameObject cardUI) { }
    private void OnEquipModeEntered(bool isPlayer1) { }
    private void OnEquipModeExited() { }
    private void OnPieceSelected(GameObject piece) { }
    private void OnPieceDeselected(GameObject piece) { }
    private void OnPieceDamaged(GameObject piece, int damage) { }
    #endregion
}