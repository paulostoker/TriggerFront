// _Scripts/MovementManager.cs - Versão Final Corrigida para "Off-Angle"
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using static ServiceLocator;

public class MovementManager : MonoBehaviour
{
    // Estado do movimento
    private GameObject currentMovingPiece;
    private List<Tile> movementTiles = new List<Tile>();
    private Dictionary<Tile, Tile> pathfindingParentMap = new Dictionary<Tile, Tile>();
    private bool isInMovementMode = false;
    private bool isProcessingMovement = false;

    // Lista para gerenciar os highlights temporários da "Off-Angle"
    private List<GameObject> offAngleHighlights = new List<GameObject>();

    // Eventos
    public static event Action OnAnyPieceFinishedMoving;
    public static event Action<GameObject> OnMovementStarted;
    public static event Action<GameObject> OnMovementCompleted;
    public static event Action OnMovementCancelled;
    public static event Action<GameObject, List<Tile>> OnMovementModeStarted;
    
    void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        if (ServiceLocator.Grid == null) 
            Debug.LogError("MovementManager: GridManager not found via ServiceLocator!");
        if (ServiceLocator.UI == null) 
            Debug.LogError("MovementManager: UIManager not found via ServiceLocator!");
        if (ServiceLocator.Pieces == null) 
            Debug.LogError("MovementManager: PieceManager not found via ServiceLocator!");
        if (ServiceLocator.CameraManager == null) 
            Debug.LogWarning("MovementManager: CameraManager not found - movement will work without camera following");
        
        Debug.Log("<color=cyan>[MovementManager]</color> Initialized with obstacle checking system");
    }
    
    #region PUBLIC METHODS
    
   public List<Tile> StartMovementMode(GameObject piece, int moveRange, Func<GameObject, HashSet<Tile>> getOccupiedTiles)
{
    if (piece == null || ServiceLocator.Freelancers == null) return new List<Tile>();

    CancelMovementMode(); 
    
    currentMovingPiece = piece;
    isInMovementMode = true;

    FreelancerData freelancerData = ServiceLocator.Freelancers.GetFreelancerData(piece);
    if (freelancerData == null || !ServiceLocator.Freelancers.IsAlive(piece))
    {
        CancelMovementMode();
        return new List<Tile>();
    }

    Tile startTile = ServiceLocator.Grid.GetTileUnderPiece(piece);
    if (startTile == null)
    {
        CancelMovementMode();
        return new List<Tile>();
    }

    List<Tile> allReachableTiles = ServiceLocator.Grid.FindPathableTiles(startTile, moveRange, out pathfindingParentMap);
    
    bool canUseOffAngle = ServiceLocator.Effects.GetStatModifier(piece, ModifierType.HasOffAngleCardActive) > 0;

    if (canUseOffAngle)
    {
        List<Tile> tilesToCheckForBoxes = new List<Tile>(allReachableTiles);
        if (!tilesToCheckForBoxes.Contains(startTile))
        {
            tilesToCheckForBoxes.Add(startTile);
        }
        
        foreach (var reachableTile in tilesToCheckForBoxes)
        {
            List<Tile> neighbors = ServiceLocator.Grid.GetNeighborTiles(reachableTile);
            foreach (var neighbor in neighbors)
            {
                ObstacleProperties obstacle = neighbor.GetObstacle();
                if (obstacle != null && obstacle.obstacleType == ObstacleType.Box)
                {
                    if (!allReachableTiles.Contains(neighbor))
                    {
                        allReachableTiles.Add(neighbor);
                        pathfindingParentMap[neighbor] = reachableTile; 
                    }
                }
            }
        }
    }
    
    HashSet<Tile> occupiedTiles = getOccupiedTiles(null);
    movementTiles = allReachableTiles.Where(t => !occupiedTiles.Contains(t) || t == startTile).ToList();
    movementTiles.Remove(startTile);

    foreach (var tile in movementTiles)
    {
        ObstacleProperties obstacle = tile.GetObstacle();
        
        if (canUseOffAngle && obstacle != null && obstacle.obstacleType == ObstacleType.Box)
        {
            var boxRenderer = obstacle.GetComponentInChildren<Renderer>();
            if (boxRenderer != null)
            {
                GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
                highlight.name = $"OffAngleHighlight_{tile.x}_{tile.z}";
                
                var link = highlight.AddComponent<HighlightLink>();
                link.linkedTile = tile;
                
                var highlightRenderer = highlight.GetComponent<Renderer>();
                highlightRenderer.material = ServiceLocator.MovementHighlightMaterial;
                float boxTopY = boxRenderer.bounds.center.y + boxRenderer.bounds.extents.y;
                highlight.transform.position = new Vector3(tile.x, boxTopY + 0.02f, tile.z);
                highlight.transform.rotation = Quaternion.Euler(90, 0, 0);
                highlight.transform.localScale = Vector3.one * 0.7f;
                offAngleHighlights.Add(highlight);
            }
        }
        else if (tile.IsWalkable() && !ServiceLocator.Grid.IsTileBlockedByObstacle(tile))
        {
            tile.ShowMovementHighlight();
        }
    }

    OnMovementModeStarted?.Invoke(piece, movementTiles);
    return new List<Tile>(movementTiles);
}
    
    public void ExecuteMovement(GameObject piece, Tile targetTile, Action<bool, int> onComplete = null)
{
    if (piece == null || targetTile == null) { onComplete?.Invoke(false, 0); return; }
    
    if (!IsValidMovementTarget(targetTile)) 
    {
        onComplete?.Invoke(false, 0);
        return;
    }
    
    Tile startTile = ServiceLocator.Grid.GetTileUnderPiece(piece);
    if (startTile == null) { onComplete?.Invoke(false, 0); return; }
    
    List<Tile> path = ReconstructPath(startTile, targetTile);

    StartCoroutine(MovePieceAlongPathCoroutine(piece, path, onComplete));
}
    
    public bool IsValidMovementTarget(Tile targetTile)
    {
        if (targetTile == null || !movementTiles.Contains(targetTile))
        {
            return false;
        }

        if (!ServiceLocator.Grid.IsTileBlockedByObstacle(targetTile))
        {
            return true;
        }

        ObstacleProperties obstacle = targetTile.GetObstacle();
        if (obstacle != null && obstacle.obstacleType == ObstacleType.Box)
        {
            if (currentMovingPiece != null && ServiceLocator.Effects.GetStatModifier(currentMovingPiece, ModifierType.HasOffAngleCardActive) > 0)
            {
                return true;
            }
        }

        return false;
    }
    
    public void CancelMovementMode()
    {
        foreach (var highlight in offAngleHighlights)
        {
            if (highlight != null)
            {
                Destroy(highlight);
            }
        }
        offAngleHighlights.Clear();
        
        ClearMovementHighlights();
        movementTiles.Clear();
        pathfindingParentMap.Clear();
        currentMovingPiece = null;
        isInMovementMode = false;
        OnMovementCancelled?.Invoke();
    }
    
    public List<Tile> ReconstructPath(Tile startTile, Tile endTile)
    {
        List<Tile> path = new List<Tile>();
        Tile current = endTile;
        
        while (current != null && current != startTile)
        {
            path.Add(current);
            if (!pathfindingParentMap.TryGetValue(current, out current))
            {
                break;
            }
        }
        path.Reverse();
        return path;
    }
    
    #endregion
    
    #region MOVEMENT EXECUTION

    private IEnumerator MovePieceAlongPathCoroutine(GameObject piece, List<Tile> path, Action<bool, int> onComplete)
{
    if (isProcessingMovement) { onComplete?.Invoke(false, 0); yield break; }
    
    isProcessingMovement = true;
    OnMovementStarted?.Invoke(piece);
    
    GameObject pieceToReturnToCenter = null;
    var opInstance = ServiceLocator.Freelancers.GetFreelancerInstance(piece);
    bool isAttacker = opInstance != null && (ServiceLocator.Game.isPlayer1Attacker == opInstance.IsPlayer1);

    CancelMovementMode();
    
    for (int i = 0; i < path.Count; i++)
    {
        Tile currentPathNode = path[i];

        // Lógica de esquiva (dodge) restaurada para o local correto (início do passo)
        if (pieceToReturnToCenter != null)
        {
            StartCoroutine(ServiceLocator.Pieces.AnimatePieceReturn(pieceToReturnToCenter));
            pieceToReturnToCenter = null;
        }

        GameObject pieceOnNextTile = ServiceLocator.Pieces.GetPieceAtTile(currentPathNode);
        if (pieceOnNextTile != null && pieceOnNextTile != piece)
        {
            if (ServiceLocator.Freelancers.IsAlive(pieceOnNextTile))
            {
                Vector3 currentTilePos = currentPathNode.transform.position;
                Vector3 previousTilePos = (i > 0) ? path[i - 1].transform.position : ServiceLocator.Grid.GetTileUnderPiece(piece).transform.position;
                Vector3 moveDirection = (currentTilePos - previousTilePos).normalized;
                Vector3 dodgeDirection = new Vector3(moveDirection.z, 0, -moveDirection.x);
                StartCoroutine(ServiceLocator.Pieces.AnimatePieceDodge(pieceOnNextTile, dodgeDirection));
                
                pieceToReturnToCenter = pieceOnNextTile;
            }
        }
        
        Vector3 targetPosition;
        ObstacleProperties obstacleOnTile = currentPathNode.GetObstacle();
        
        if (obstacleOnTile != null && obstacleOnTile.obstacleType == ObstacleType.Box)
        {
            var renderer = obstacleOnTile.GetComponentInChildren<Renderer>();
            float obstacleTopY = renderer.bounds.center.y + renderer.bounds.extents.y;
            float pieceOffset = GameConfig.Instance != null ? GameConfig.Instance.pieceHeightOffset : 1.0f;
            targetPosition = new Vector3(currentPathNode.x, obstacleTopY + pieceOffset, currentPathNode.z);
        }
        else
        {
            targetPosition = ServiceLocator.Pieces.GetRaycastPositionOnTile(currentPathNode);
        }

        yield return StartCoroutine(MoveAndRotateCoroutine(piece, targetPosition));
        ServiceLocator.Pieces.UpdateOriginalPosition(piece, targetPosition);
        
        if (isAttacker)
        {
            Tile droppedBombTile = ServiceLocator.Bomb.GetDroppedBombTile();
            if (droppedBombTile != null && currentPathNode == droppedBombTile)
            {
                ServiceLocator.Bomb.PickupBomb(piece);
            }
        }
    }
    
    // Garante que a última peça que se esquivou também volte
    if (pieceToReturnToCenter != null)
    {
        yield return StartCoroutine(ServiceLocator.Pieces.AnimatePieceReturn(pieceToReturnToCenter));
    }
    
    isProcessingMovement = false;
    
    ServiceLocator.Freelancers.UpdateOffAngleState(piece);
    ServiceLocator.Effects.CleanUpActionEffects(piece, ActionType.Movement);
    
    OnMovementCompleted?.Invoke(piece);
    OnAnyPieceFinishedMoving?.Invoke();
    Debug.Log("<color=yellow><b>[INVESTIGAÇÃO 1/4]</b></color> MovementManager terminou o movimento. Prestes a notificar o TurnManager.");
ServiceLocator.Turn.NotifyActionCompleted(isMove: true);
    
    // Passa a distância do caminho de volta no callback
        onComplete?.Invoke(true, path.Count); 
}
    
     private IEnumerator MoveAndRotateCoroutine(GameObject piece, Vector3 targetPosition)
    {
        Vector3 startPosition = piece.transform.position;
        float journeyLength = Vector3.Distance(startPosition, targetPosition);
        
        Debug.Log($"--- [MoveAndRotate Debug] Iniciando para '{piece.name}' ---");
        Debug.Log($"[MoveAndRotate Debug] Posição Inicial: {startPosition.ToString("F3")}");
        Debug.Log($"[MoveAndRotate Debug] Posição Alvo:    {targetPosition.ToString("F3")}");
        Debug.Log($"[MoveAndRotate Debug] Distância da Viagem: {journeyLength}");

        if (journeyLength <= 0.01f)
        {
            Debug.LogWarning("[MoveAndRotate Debug] Distância da viagem é quase zero. O movimento foi abortado.", piece);
            yield break; 
        }
        
        Quaternion startRotation = piece.transform.rotation;
        Vector3 direction = targetPosition - startPosition;
        direction.y = 0;
        
        Quaternion targetRotation = startRotation;
        if (direction.sqrMagnitude > 0.001f)
        {
            targetRotation = Quaternion.LookRotation(direction);
        }

        float startTime = Time.time;
        
        while (Vector3.Distance(piece.transform.position, targetPosition) > 0.01f)
        {
            float distCovered = (Time.time - startTime) * GameConfig.Instance.moveSpeed;
            float fractionOfJourney = distCovered / journeyLength;
            piece.transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);

            float step = 900f * Time.deltaTime;
            piece.transform.rotation = Quaternion.RotateTowards(piece.transform.rotation, targetRotation, step);

            // --- ALTERAÇÃO PRINCIPAL APLICADA AQUI ---
            // Agora chamamos o método de rastreamento instantâneo.
            if (ServiceLocator.CameraManager != null) ServiceLocator.CameraManager.TrackPiece(piece);
            
            yield return null;
        }

        piece.transform.position = targetPosition;
        piece.transform.rotation = targetRotation;

        // Usamos o TrackPiece aqui também para garantir a posição final exata.
        if (ServiceLocator.CameraManager != null) ServiceLocator.CameraManager.TrackPiece(piece);
    }
    
    #endregion
    
    #region UTILITY METHODS
    
    private void ClearMovementHighlights()
    {
        foreach (var tile in movementTiles)
        {
            if (tile != null) 
            {
                tile.HideAllHighlights();
            }
        }
    }
    
    #endregion
    
    #region PUBLIC ACCESSORS
    
    public GameObject GetCurrentMovingPiece() => currentMovingPiece;
    public bool IsInMovementMode() => isInMovementMode;
    public bool IsProcessingMovement() => isProcessingMovement;
    public List<Tile> GetMovementTiles() => new List<Tile>(movementTiles);
    
    public int GetRemainingMovement(GameObject piece)
    {
        if (ServiceLocator.Freelancers == null) return 0;
        
        FreelancerData freelancerData = ServiceLocator.Freelancers.GetFreelancerData(piece);
        if (freelancerData == null) return 0;
        
        return freelancerData.baseMovement;
    }
    
    public bool IsInMovementRange(GameObject piece, Tile targetTile)
    {
        if (piece == null || targetTile == null) return false;
        if (ServiceLocator.Grid.IsTileBlockedByObstacle(targetTile)) return false;
        
        Tile startTile = ServiceLocator.Grid.GetTileUnderPiece(piece);
        if (startTile == null) return false;
        
        FreelancerData freelancerData = ServiceLocator.Freelancers.GetFreelancerData(piece);
        if (freelancerData == null) return false;
        
        Dictionary<Tile, Tile> tempPathMap;
        List<Tile> reachableTiles = ServiceLocator.Grid.FindPathableTiles(startTile, freelancerData.baseMovement, out tempPathMap);
        
        return reachableTiles.Contains(targetTile) && targetTile.IsWalkable() && !ServiceLocator.Grid.IsTileBlockedByObstacle(targetTile);
    }
    
    public MovementStatistics GetMovementStatistics(GameObject piece)
    {
        MovementStatistics stats = new MovementStatistics();
        
        if (ServiceLocator.Pieces == null || ServiceLocator.Grid == null || ServiceLocator.Freelancers == null)
            return stats;
        
        FreelancerData freelancerData = ServiceLocator.Freelancers.GetFreelancerData(piece);
        if (freelancerData == null) return stats;
        
        Tile startTile = ServiceLocator.Grid.GetTileUnderPiece(piece);
        if (startTile == null) return stats;
        
        List<Tile> tilesInRange = ServiceLocator.Grid.FindTilesInRange(startTile, freelancerData.baseMovement);
        stats.tilesInRange = tilesInRange.Count;
        
        Dictionary<Tile, Tile> tempPathMap;
        List<Tile> reachableTiles = ServiceLocator.Grid.FindPathableTiles(startTile, freelancerData.baseMovement, out tempPathMap);
        stats.reachableTiles = reachableTiles.Count;
        
        stats.tilesBlockedByObstacles = tilesInRange.Count(tile => ServiceLocator.Grid.IsTileBlockedByObstacle(tile));
        
        HashSet<Tile> occupiedTiles = ServiceLocator.Pieces.GetAllOccupiedTiles(piece);
        stats.tilesOccupiedByPieces = tilesInRange.Count(tile => occupiedTiles.Contains(tile));
        
        stats.unwalkableTiles = tilesInRange.Count(tile => !tile.IsWalkable());
        
        return stats;
    }
    
    #endregion
    
    #region CLEANUP
    
    void OnDestroy()
    {
        StopAllCoroutines();
        CancelMovementMode();
        
        OnMovementStarted = null;
        OnMovementCompleted = null;
        OnMovementCancelled = null;
        OnMovementModeStarted = null;
    }
    public static void ResetStaticData()
{
    OnAnyPieceFinishedMoving = null;
    OnMovementStarted = null;
    OnMovementCompleted = null;
    OnMovementCancelled = null;
    OnMovementModeStarted = null;
    
    Debug.Log("<color=cyan>[MovementManager]</color> Static events cleared for new game session");
}
    #endregion
}

[System.Serializable]
public class MovementStatistics
{
    public int tilesInRange;
    public int reachableTiles;
    public int tilesBlockedByObstacles;
    public int tilesOccupiedByPieces;
    public int unwalkableTiles;
    
    public override string ToString()
    {
        return $"Movement Stats: InRange={tilesInRange}, Reachable={reachableTiles}, " +
               $"BlockedByObstacles={tilesBlockedByObstacles}, OccupiedByPieces={tilesOccupiedByPieces}, " +
               $"Unwalkable={unwalkableTiles}";
    }
}