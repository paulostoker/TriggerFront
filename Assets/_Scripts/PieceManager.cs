// _Scripts/PieceManager.cs - Versão Final com Atualização de UI Pós-Morte
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using static ServiceLocator;

public class PieceManager : MonoBehaviour
{
    private List<GameObject> player1Pieces = new List<GameObject>();
    private List<GameObject> player2Pieces = new List<GameObject>();
    private GameObject selectedPiece = null;

    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();

    public static event Action OnAnyPieceKilled;

    [Header("Visual Settings")]
    public GameObject deadPiecePrefab;
    public Material player2Material;

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask tileLayerMask = 1 << 3;
    [SerializeField] private float raycastHeight = 50f;
    [SerializeField] private float raycastDistance = 60f;
    [SerializeField] private float pieceHeightOffset = 1.0f;
    [SerializeField] private bool useUpwardNormalFilter = true;

    public static event Action<GameObject> OnPieceSelected;
    public static event Action<GameObject> OnPieceDeselected;
    public static event Action OnAllPiecesDeselected;
    public static event Action<List<GameObject>, bool> OnPiecesSpawned;
    public static event Action<GameObject> OnPieceDestroyed;
    public static event Action<GameObject, int> OnPieceDamaged;
    public static event Action<GameObject, bool> OnPieceEcoStatusChanged;

    #region Event Subscription
    private void OnEnable()
    {
        MovementManager.OnAnyPieceFinishedMoving += HandleAnyPieceMoved;
        OnAnyPieceKilled += HandleAnyPieceKilled_UpdateUI;
        /// ADICIONAR LINHA
        ActionState.OnActionPhaseBegan += HandleActionPhaseBegan;
    }

    private void OnDisable()
    {
        MovementManager.OnAnyPieceFinishedMoving -= HandleAnyPieceMoved;
        OnAnyPieceKilled -= HandleAnyPieceKilled_UpdateUI;
        /// ADICIONAR LINHA
        ActionState.OnActionPhaseBegan -= HandleActionPhaseBegan;
    }
    #endregion

    #region Event Handlers
    /// COMEÇO DO CÓDIGO A SER ADICIONADO
    private void HandleActionPhaseBegan()
    {
        // No início da fase de ação, atualiza a UI de status de todos.
        UpdateAllLivingPieceStatusUI();
    }
    /// FIM DO CÓDIGO A SER ADICIONADO

    private void HandleAnyPieceMoved()
    {
        UpdateAllLivingPieceStatusUI();
    }

    private void HandleAnyPieceKilled_UpdateUI()
    {

        UpdateAllLivingPieceStatusUI();
    }

    private void UpdateAllLivingPieceStatusUI()
    {
        var allLivingFreelancers = ServiceLocator.Freelancers.GetAllFreelancerInstances()
                                    .Where(op => op.IsAlive && op.PieceGameObject != null);

        foreach (var instance in allLivingFreelancers)
        {
            // Garante que o estado lógico seja reavaliado ANTES de atualizar a UI.
            // Isso desligará o IsInOffAngleState se a peça não estiver mais em um Box.
            ServiceLocator.Freelancers.UpdateOffAngleState(instance.PieceGameObject);

            // Atualiza a UI, que agora lerá o estado correto.
            UpdatePieceStatusUI(instance.PieceGameObject);
        }
    }
    #endregion



    void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (tileLayerMask == 0)
        {
            int tileLayer = LayerMask.NameToLayer("Tile");
            if (tileLayer != -1)
            {
                tileLayerMask = 1 << tileLayer;
            }
            else
            {
                tileLayerMask = 1 << 3;
            }
        }
    }

    #region PIECE SPAWNING & SETUP

    public void SpawnPiecesFromSpawnPoints(
        List<FreelancerData> player1Deck,
        List<FreelancerData> player2Deck,
        List<Vector2Int> player1SpawnPoints,
        List<Vector2Int> player2SpawnPoints,
        Transform parent)
    {
        StartCoroutine(SpawnPiecesAfterTileSetup(player1Deck, player2Deck, player1SpawnPoints, player2SpawnPoints, parent));
    }

    private IEnumerator SpawnPiecesAfterTileSetup(
        List<FreelancerData> player1Deck,
        List<FreelancerData> player2Deck,
        List<Vector2Int> player1SpawnPoints,
        List<Vector2Int> player2SpawnPoints,
        Transform parent)
    {
        yield return new WaitForEndOfFrame();

        ClearAllPieces();

        SpawnPlayerPieces(player1Deck, player1SpawnPoints, true, parent);
        SpawnPlayerPieces(player2Deck, player2SpawnPoints, false, parent);

        if (GameConfig.Instance.enablePieceLogs)
        {
            Debug.Log($"<color=lime>[PieceManager]</color> Spawned {player1Pieces.Count + player2Pieces.Count} pieces inside '{parent.name}'");
        }
    }

    private void SpawnPlayerPieces(List<FreelancerData> freelancerDeck, List<Vector2Int> spawnPoints, bool isPlayer1, Transform parent)
    {
        List<GameObject> spawnedPieces = new List<GameObject>();

        for (int i = 0; i < freelancerDeck.Count; i++)
        {
            Vector2Int spawnCoord = spawnPoints[i];
            FreelancerData opData = freelancerDeck[i];

            Vector3 spawnPos = GetRaycastPositionOnTile(spawnCoord);

            Quaternion rotation = isPlayer1 ? Quaternion.identity : Quaternion.Euler(0, 180, 0);

            GameObject piece = Instantiate(opData.piecePrefab, spawnPos, rotation, parent);

            piece.name = $"{(isPlayer1 ? "P1" : "P2")}_{opData.name}";

            if (!isPlayer1 && player2Material != null)
            {
                Renderer pieceRenderer = piece.GetComponentInChildren<Renderer>();
                if (pieceRenderer != null)
                {
                    pieceRenderer.material = player2Material;
                }
                else
                {
                    Debug.LogWarning($"A peça {piece.name} não possui um componente 'Renderer' para aplicar o material do Jogador 2.");
                }
            }

            ServiceLocator.Freelancers.RegisterPieceInstance(piece, opData, isPlayer1);

            if (isPlayer1)
            {
                player1Pieces.Add(piece);
            }
            else
            {
                player2Pieces.Add(piece);
            }
            spawnedPieces.Add(piece);

            UpdatePieceDisplay(piece);
            UpdateOriginalPosition(piece, spawnPos);

            if (GameConfig.Instance.enablePieceLogs)
            {
                Debug.Log($"<color=lime>[PieceManager]</color> Spawned {piece.name} at corrected position {spawnPos}");
            }
        }

        OnPiecesSpawned?.Invoke(spawnedPieces, isPlayer1);
    }

    #endregion

    #region SISTEMA DE POSICIONAMENTO POR RAYCAST

    public Vector3 GetRaycastPositionOnTile(Vector2Int gridPosition)
    {
        Vector3 raycastStart = new Vector3(gridPosition.x, raycastHeight, gridPosition.y);
        RaycastHit[] hits = Physics.RaycastAll(raycastStart, Vector3.down, raycastDistance, tileLayerMask);

        if (hits.Length > 0)
        {
            RaycastHit bestHit = hits[0];

            if (useUpwardNormalFilter)
            {
                foreach (var hit in hits)
                {
                    if (hit.normal.y > 0.7f)
                    {
                        if (bestHit.normal.y <= 0.7f || hit.point.y > bestHit.point.y)
                        {
                            bestHit = hit;
                        }
                    }
                }

                if (bestHit.normal.y <= 0.7f)
                {
                    foreach (var hit in hits)
                    {
                        if (hit.point.y > bestHit.point.y)
                        {
                            bestHit = hit;
                        }
                    }
                }
            }
            else
            {
                foreach (var hit in hits)
                {
                    if (hit.point.y > bestHit.point.y)
                    {
                        bestHit = hit;
                    }
                }
            }

            return bestHit.point + Vector3.up * pieceHeightOffset;
        }

        if (ServiceLocator.Grid != null)
        {
            return ServiceLocator.Grid.GetFreelancerPositionOnTile(gridPosition);
        }

        return new Vector3(gridPosition.x, 1.5f, gridPosition.y);
    }

    public Vector3 GetRaycastPositionOnTile(Tile tile)
    {
        if (tile == null) return Vector3.zero;
        return GetRaycastPositionOnTile(new Vector2Int(tile.x, tile.z));
    }

    public void UpdatePiecePositionWithRaycast(GameObject piece)
    {
        if (piece == null) return;

        Vector2Int gridPos = new Vector2Int(
            Mathf.RoundToInt(piece.transform.position.x),
            Mathf.RoundToInt(piece.transform.position.z)
        );

        Vector3 newPosition = GetRaycastPositionOnTile(gridPos);
        piece.transform.position = newPosition;

        UpdateOriginalPosition(piece, newPosition);
    }

    public void UpdateAllPiecePositionsWithRaycast()
    {
        var allPieces = new List<GameObject>(player1Pieces);
        allPieces.AddRange(player2Pieces);

        int updatedCount = 0;
        foreach (var piece in allPieces)
        {
            if (piece != null && piece.activeInHierarchy)
            {
                UpdatePiecePositionWithRaycast(piece);
                updatedCount++;
            }
        }
    }

    public bool TestRaycastAtPosition(Vector2Int gridPosition, out RaycastHit hitInfo)
    {
        Vector3 raycastStart = new Vector3(gridPosition.x, raycastHeight, gridPosition.y);
        RaycastHit[] hits = Physics.RaycastAll(raycastStart, Vector3.down, raycastDistance, tileLayerMask);

        if (hits.Length > 0)
        {
            hitInfo = hits[0];
            foreach (var hit in hits)
            {
                if (hit.point.y > hitInfo.point.y)
                {
                    hitInfo = hit;
                }
            }
            return true;
        }
        else
        {
            hitInfo = default;
            return false;
        }
    }

    #endregion

    #region PIECE SELECTION

    public void SelectPiece(GameObject piece, Func<bool> canMove, Func<bool> canAct, Func<bool> canUseSkill)
    {
        if (piece == null) return;

        if (selectedPiece == piece && ServiceLocator.UI != null && ServiceLocator.UI.IsActionMenuVisible())
            return;

        DeselectCurrentPiece();
        selectedPiece = piece;

        if (ServiceLocator.UI != null)
        {
            ServiceLocator.UI.ShowActionMenu(piece.transform.position, canMove(), canAct(), canUseSkill(), true);
        }

        OnPieceSelected?.Invoke(piece);
    }

    public void DeselectCurrentPiece()
    {
        if (selectedPiece != null)
        {
            GameObject previousPiece = selectedPiece;
            selectedPiece = null;

            if (ServiceLocator.UI != null)
            {
                ServiceLocator.UI.HideActionMenu();
            }

            OnPieceDeselected?.Invoke(previousPiece);
        }
        OnAllPiecesDeselected?.Invoke();
    }

    #endregion

    #region PIECE UI & STATE MANAGEMENT

    private void UpdatePieceDisplay(GameObject piece)
    {
        if (piece == null || ServiceLocator.Freelancers == null) return;

        PieceDisplay display = piece.GetComponentInChildren<PieceDisplay>();
        if (display != null)
        {
            FreelancerData baseData = ServiceLocator.Freelancers.GetFreelancerData(piece);
            int currentHP = ServiceLocator.Freelancers.GetCurrentHP(piece);

            if (baseData != null)
            {
                display.SetName(baseData.name);
                display.UpdateHP(currentHP, baseData.HP);
            }
        }
    }

    public void UpdatePieceEnergyUI(GameObject piece)
    {
        if (piece == null || ServiceLocator.Freelancers == null) return;

        PieceDisplay display = piece.GetComponentInChildren<PieceDisplay>();
        if (display != null)
        {
            int actionCount = ServiceLocator.Freelancers.GetEnergyCount(piece, CardType.Action);
            int utilityCount = ServiceLocator.Freelancers.GetEnergyCount(piece, CardType.Utility);
            int auraCount = ServiceLocator.Freelancers.GetEnergyCount(piece, CardType.Aura);

            display.UpdateEnergyDisplay(actionCount, utilityCount, auraCount);
        }
    }

    public void UpdatePieceStatusUI(GameObject piece)
    {
        if (piece == null) return;

        FreelancerInstance opInstance = ServiceLocator.Freelancers.GetFreelancerInstance(piece);
        PieceDisplay display = piece.GetComponentInChildren<PieceDisplay>();

        if (opInstance != null && display != null)
        {
            display.UpdateStatusEffects(opInstance);
        }
    }

    public void HandlePieceDeath(GameObject piece, int finalDamage)
    {
        if (piece == null) return;

        FreelancerInstance freelancerInstance = ServiceLocator.Freelancers.GetFreelancerInstance(piece);
        if (freelancerInstance == null)
        {
            Debug.LogError($"Não foi possível encontrar a FreelancerInstance para a peça {piece.name}. Abortando a morte.");
            Destroy(piece);
            return;
        }

        Vector3 position = piece.transform.position;
        Quaternion rotation = piece.transform.rotation;
        Transform parent = piece.transform.parent;

        GameObject deadPieceObject = Instantiate(deadPiecePrefab, position, rotation, parent);
        deadPieceObject.name = $"Dead_{freelancerInstance.BaseData.name}";

        ServiceLocator.Freelancers.UpdatePieceReference(piece, deadPieceObject);

        PieceDisplay deadPieceDisplay = deadPieceObject.GetComponentInChildren<PieceDisplay>();
        if (deadPieceDisplay != null)
        {
            deadPieceDisplay.SetName(freelancerInstance.BaseData.name);
            deadPieceDisplay.ShowDamagePopup(finalDamage);
            deadPieceDisplay.UpdateHP(0, freelancerInstance.BaseData.HP);
        }

        if (!freelancerInstance.IsPlayer1 && player2Material != null)
        {
            Renderer deadRenderer = deadPieceObject.GetComponentInChildren<Renderer>();
            if (deadRenderer != null)
            {
                deadRenderer.material = player2Material;
            }
        }

        if (selectedPiece == piece)
        {
            DeselectCurrentPiece();
        }

        player1Pieces.Remove(piece);
        player2Pieces.Remove(piece);
        originalPositions.Remove(piece);


        OnAnyPieceKilled?.Invoke();
        UpdateAllLivingPieceStatusUI();

        Destroy(piece);
    }

    #endregion

    #region PIECE POSITION SYSTEM

    public void UpdateOriginalPosition(GameObject piece, Vector3 newPosition)
    {
        if (piece != null)
        {
            originalPositions[piece] = newPosition;
        }
    }

    public Vector3 GetOriginalPosition(GameObject piece)
    {
        if (piece != null && originalPositions.TryGetValue(piece, out Vector3 position))
        {
            return position;
        }
        return piece != null ? piece.transform.position : Vector3.zero;
    }

    public void MovePieceToTile(GameObject piece, Tile targetTile)
    {
        if (piece == null || targetTile == null) return;

        Vector3 targetPosition = GetRaycastPositionOnTile(targetTile);
        piece.transform.position = targetPosition;
        UpdateOriginalPosition(piece, targetPosition);
    }

    #endregion

    #region PIECE ANIMATION SYSTEM

    public IEnumerator AnimatePieceDodge(GameObject piece, Vector3 direction)
    {
        yield return StartCoroutine(DodgeCoroutine(piece, direction));
    }

    public IEnumerator AnimatePieceReturn(GameObject piece)
    {
        yield return StartCoroutine(ReturnCoroutine(piece));
    }

    private IEnumerator DodgeCoroutine(GameObject piece, Vector3 direction)
    {
        Vector3 originalPos = GetOriginalPosition(piece);
        Vector3 dodgePosition = originalPos + (direction.normalized * GameConfig.Instance.dodgeDistance);
        float elapsedTime = 0f;
        Vector3 startPos = piece.transform.position;

        while (elapsedTime < GameConfig.Instance.dodgeDuration)
        {
            piece.transform.position = Vector3.Lerp(startPos, dodgePosition, elapsedTime / GameConfig.Instance.dodgeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        piece.transform.position = dodgePosition;
    }

    private IEnumerator ReturnCoroutine(GameObject piece)
    {
        Vector3 correctPosition = GetOriginalPosition(piece);
        float elapsedTime = 0f;
        Vector3 startPos = piece.transform.position;

        while (elapsedTime < GameConfig.Instance.returnDuration)
        {
            piece.transform.position = Vector3.Lerp(startPos, correctPosition, elapsedTime / GameConfig.Instance.returnDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        piece.transform.position = correctPosition;
    }

    #endregion

    #region UTILITY METHODS

    public GameObject GetActivePiece(int freelancerIndex, bool isPlayer1)
    {
        var pieceList = isPlayer1 ? player1Pieces : player2Pieces;
        if (freelancerIndex < 0 || freelancerIndex >= pieceList.Count)
            return null;
        return pieceList[freelancerIndex];
    }

    public bool IsPieceOnTeam(GameObject piece, bool isPlayer1)
    {
        if (piece == null) return false;
        var targetList = isPlayer1 ? player1Pieces : player2Pieces;
        return targetList.Contains(piece);
    }

    public GameObject GetPieceAtTile(Tile tile)
    {
        if (tile == null) return null;

        var allPieces = player1Pieces.Concat(player2Pieces);
        foreach (var piece in allPieces)
        {
            if (piece.activeInHierarchy && ServiceLocator.Grid.GetTileUnderPiece(piece) == tile)
                return piece;
        }

        return null;
    }

    public HashSet<Tile> GetAllOccupiedTiles(GameObject excludePiece = null)
    {
        HashSet<Tile> occupiedTiles = new HashSet<Tile>();
        var allPieces = player1Pieces.Concat(player2Pieces);
        foreach (var piece in allPieces.Where(p => p != null && p.activeInHierarchy && p != excludePiece))
        {
            var tile = ServiceLocator.Grid.GetTileUnderPiece(piece);
            if (tile != null) occupiedTiles.Add(tile);
        }
        return occupiedTiles;
    }

    public void RemovePiece(GameObject piece)
    {
        if (piece == null) return;

        if (selectedPiece == piece)
        {
            DeselectCurrentPiece();
        }

        player1Pieces.Remove(piece);
        player2Pieces.Remove(piece);
        originalPositions.Remove(piece);

        OnPieceDestroyed?.Invoke(piece);
    }

    private void ClearAllPieces()
    {
        foreach (var piece in player1Pieces) if (piece != null) Destroy(piece);
        foreach (var piece in player2Pieces) if (piece != null) Destroy(piece);

        player1Pieces.Clear();
        player2Pieces.Clear();
        originalPositions.Clear();
        selectedPiece = null;
    }
    public int GetFreelancerIndex(GameObject piece)
    {
        if (piece == null) return -1;

        int index = player1Pieces.FindIndex(p => p == piece);
        if (index != -1)
        {
            return index;
        }

        index = player2Pieces.FindIndex(p => p == piece);
        if (index != -1)
        {
            return index;
        }

        return -1;
    }

    public GameObject GetSelectedPiece() => selectedPiece;
    public bool HasSelectedPiece() => selectedPiece != null;
    public List<GameObject> GetPlayerPieces(bool isPlayer1) => isPlayer1 ? new List<GameObject>(player1Pieces) : new List<GameObject>(player2Pieces);

    #endregion


    #region CLEANUP

    void OnDestroy()
    {
        ClearAllPieces();
        OnPieceSelected = null;
        OnPieceDeselected = null;
        OnAllPiecesDeselected = null;
        OnPiecesSpawned = null;
        OnPieceDestroyed = null;
        OnPieceDamaged = null;
        OnPieceEcoStatusChanged = null;
    }
    public static void ResetStaticData()
{
    OnAnyPieceKilled = null;
    OnPieceSelected = null;
    OnPieceDeselected = null;
    OnAllPiecesDeselected = null;
    OnPiecesSpawned = null;
    OnPieceDestroyed = null;
    OnPieceDamaged = null;
    OnPieceEcoStatusChanged = null;
    
    Debug.Log("<color=lime>[PieceManager]</color> Static events cleared for new game session");
}
    
    #endregion
}