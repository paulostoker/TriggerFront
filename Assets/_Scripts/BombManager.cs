// _Scripts/BombManager.cs
using UnityEngine;
using System;

public class BombManager : MonoBehaviour
{
    public enum BombState
    {
        Unassigned,
        Carried,
        Dropped,
        Planting,
        Planted,
        Defusing,
        Defused,
        Exploded
    }

    [Header("Prefabs e Configurações")]
    [SerializeField] private GameObject bombVisualPrefab;
    [SerializeField] private Vector3 bombCarryOffset = new Vector3(0, 0.25f, -0.5f);
    [SerializeField] private float bombGroundOffset = 0.2f;

    [Header("Configurações do Timer (Ajuste no Inspector)")]
    [Tooltip("Duração total do timer da bomba após ser plantada.")]
    public int bombDuration = 15;
    [Tooltip("Duração total do processo de desarme.")]
    public int defuseDuration = 5;

    [Header("Status da Bomba (Apenas para Debug)")]
    [SerializeField] private BombState currentState = BombState.Unassigned;
    [SerializeField] private GameObject currentCarrier = null;
    [SerializeField] private GameObject currentDefuser = null;

    private int bombTimer;
    private int defuseTimer;

    private bool isFirstDefuseTick = false;
    private bool isFirstBombTick = false;
    public static event Action<GameObject> OnDefuseStarted;

    [SerializeField] private GameObject currentPlanter = null;
    [SerializeField] private GameObject bombVisualInstance = null;
    [SerializeField] private Tile droppedBombTile = null;


    #region Lógica do Portador (Carrier)

    public void AssignBombCarrier(GameObject piece)
    {
        if (currentState != BombState.Unassigned) return;
        var opInstance = ServiceLocator.Freelancers.GetFreelancerInstance(piece);
        if (opInstance == null || !opInstance.IsAlive) return;
        opInstance.isBombCarrier = true;
        currentCarrier = piece;
        currentState = BombState.Carried;
        if (bombVisualPrefab != null)
        {
            bombVisualInstance = Instantiate(bombVisualPrefab, currentCarrier.transform);
            bombVisualInstance.transform.localPosition = bombCarryOffset;
        }
        ServiceLocator.Cards.SetBombIndicator(opInstance, true);
    }

    public void DropBomb(Tile tile)
    {
        if (currentState != BombState.Carried || currentCarrier == null) return;
        var opInstance = ServiceLocator.Freelancers.GetFreelancerInstance(currentCarrier);
        if (opInstance == null) return;
        opInstance.isBombCarrier = false;
        currentCarrier = null;
        currentState = BombState.Dropped;
        droppedBombTile = tile;
        if (bombVisualInstance != null)
        {
            GameObject pivot = GameObject.Find("Pivot");
            bombVisualInstance.transform.SetParent(pivot != null ? pivot.transform : null);

            Vector3 groundPosition = ServiceLocator.Pieces.GetRaycastPositionOnTile(new Vector2Int(tile.x, tile.z));
            bombVisualInstance.transform.position = groundPosition + new Vector3(0, bombGroundOffset, 0);
        }
        ServiceLocator.Cards.SetBombIndicator(opInstance, false);
    }

    public void PickupBomb(GameObject newCarrier)
    {
        if (currentState != BombState.Dropped) return;
        var opInstance = ServiceLocator.Freelancers.GetFreelancerInstance(newCarrier);
        if (opInstance == null || !opInstance.IsAlive) return;
        opInstance.isBombCarrier = true;
        currentCarrier = newCarrier;
        currentState = BombState.Carried;
        droppedBombTile = null;
        if (bombVisualInstance != null)
        {
            bombVisualInstance.transform.SetParent(currentCarrier.transform);
            bombVisualInstance.transform.localPosition = bombCarryOffset;
        }
        ServiceLocator.Cards.SetBombIndicator(opInstance, true);
    }

    #endregion

    #region Lógica de Plantio

    public void StartPlanting(GameObject planter)
    {
        if (!CanFreelancerPlant(planter)) return;
        currentState = BombState.Planting;
        currentPlanter = planter;
    }

    public void ConfirmPlant()
    {
        if (currentState != BombState.Planting) return;
        Tile plantTile = ServiceLocator.Grid.GetTileUnderPiece(currentPlanter);
        if (plantTile == null) return;
        currentState = BombState.Planted;
        bombTimer = bombDuration;

        isFirstBombTick = true;

        droppedBombTile = plantTile;
        if (bombVisualInstance != null)
        {
            GameObject pivot = GameObject.Find("Pivot");
            bombVisualInstance.transform.SetParent(pivot != null ? pivot.transform : null);
            Vector3 groundPosition = ServiceLocator.Pieces.GetRaycastPositionOnTile(new Vector2Int(plantTile.x, plantTile.z));
            bombVisualInstance.transform.position = groundPosition + new Vector3(0, bombGroundOffset, 0);
        }
        var opInstance = ServiceLocator.Freelancers.GetFreelancerInstance(currentPlanter);
        if (opInstance != null)
        {
            opInstance.isBombCarrier = false;
            ServiceLocator.Cards.SetBombIndicator(opInstance, false);
        }
        currentCarrier = null;
        currentPlanter = null;
        ServiceLocator.UI.UpdateTimerDisplay();
    }

    public void TickBombTimer()
    {

        if (isFirstBombTick)
        {
            isFirstBombTick = false;
            ServiceLocator.UI.UpdateTimerDisplay();
            return;
        }
   
        if (currentState != BombState.Planted && currentState != BombState.Defusing)
        {
            return;
        }

        bombTimer--;

        if (bombTimer <= 0)
        {
            currentState = BombState.Exploded;
            Debug.Log($"<color=red>[BombManager]</color> A BOMBA EXPLODIU!");
            ServiceLocator.Game.EndGame("Attackers");
        }

        ServiceLocator.UI.UpdateTimerDisplay();
    }

    #endregion

    #region Lógica de Defusão

    public void StartDefusing(GameObject defuser)
    {
        if (!CanFreelancerDefuse(defuser)) return;
        currentState = BombState.Defusing;
        currentDefuser = defuser;
        defuseTimer = defuseDuration;
        isFirstDefuseTick = true;

        OnDefuseStarted?.Invoke(defuser);

        ServiceLocator.Effects.ProcessTriggeredEffects(defuser, ActionType.StartDefuse);
    }

    public void CancelDefuse()
    {
        if (currentState != BombState.Defusing) return;
        currentState = BombState.Planted;
        currentDefuser = null;
        defuseTimer = defuseDuration;
        isFirstDefuseTick = false;
        ServiceLocator.UI.UpdateTimerDisplay();
    }

    public void TickDefuseTimer()
    {
        if (currentState != BombState.Defusing) return;

        if (isFirstDefuseTick)
        {
            isFirstDefuseTick = false;
            ServiceLocator.UI.UpdateTimerDisplay();
            return;
        }

        defuseTimer--;
        if (defuseTimer <= 0)
        {
            currentState = BombState.Defused;
            ServiceLocator.Game.EndGame("Defenders");
        }
        ServiceLocator.UI.UpdateTimerDisplay();
    }

    public void CancelDefuseIfDefuserActs(GameObject actingPiece)
    {
        if (currentState == BombState.Defusing && actingPiece == currentDefuser)
        {
            CancelDefuse();
        }
    }

    public void CancelDefuseIfDefuserDied(GameObject deadPiece)
    {
        if (currentState == BombState.Defusing && deadPiece == currentDefuser)
        {
            CancelDefuse();
        }
    }

    #endregion

    #region Métodos Auxiliares

    public bool CanFreelancerPlant(GameObject piece)
    {
        if (piece == null) return false;
        var opInstance = ServiceLocator.Freelancers.GetFreelancerInstance(piece);
        if (opInstance == null || !opInstance.isBombCarrier) return false;
        Tile currentTile = ServiceLocator.Grid.GetTileUnderPiece(piece);
        return currentTile != null && (currentTile.GetSpecialType() == TileSpecialType.BombsiteA || currentTile.GetSpecialType() == TileSpecialType.BombsiteB);
    }

    public bool CanFreelancerDefuse(GameObject piece)
    {
        if (piece == null || currentState != BombState.Planted) return false;
        var opInstance = ServiceLocator.Freelancers.GetFreelancerInstance(piece);
        if (opInstance == null || ServiceLocator.Game.isPlayer1Attacker == opInstance.IsPlayer1) return false;
        Tile pieceTile = ServiceLocator.Grid.GetTileUnderPiece(piece);
        Tile bombTile = GetDroppedBombTile();
        return pieceTile != null && bombTile != null && pieceTile == bombTile;
    }

    public BombState GetCurrentBombState() => currentState;
    public int GetBombTimeRemaining() => bombTimer;
    public int GetDefuseTimeRemaining() => defuseTimer;
    public bool IsPlanted() => currentState == BombState.Planted || currentState == BombState.Defusing;
    public Tile GetDroppedBombTile() => droppedBombTile;

    public bool IsRoundOver()
    {
        return currentState == BombState.Exploded || currentState == BombState.Defused;
    }

    #endregion
    public static void ResetStaticData()
{
    OnDefuseStarted = null;
    
    Debug.Log("<color=orange>[BombManager]</color> Static events cleared for new game session");
}

}