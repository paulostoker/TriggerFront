// _Scripts/Tile.cs - Versão com FindObjectOfType corrigido
using UnityEngine;

public class Tile : MonoBehaviour
{
    public int x, z;
    public bool isWalkable = true;

    private TileHighlightSystem highlightSystem;
    private TileProperties tileProperties;

    void Awake()
    {
        highlightSystem = GetComponent<TileHighlightSystem>();
        tileProperties = GetComponent<TileProperties>();
        
        if (highlightSystem == null)
        {
            highlightSystem = gameObject.AddComponent<TileHighlightSystem>();
        }
        
        if (tileProperties == null)
        {
            tileProperties = gameObject.AddComponent<TileProperties>();
        }
    }

    public void Initialize()
    {
        if (x == 0 && z == 0)
        {
            Vector3 worldPos = transform.position;
            x = Mathf.RoundToInt(worldPos.x);
            z = Mathf.RoundToInt(worldPos.z);
        }
        
        if (highlightSystem != null)
        {
            highlightSystem.InitializeHighlightSystem();
        }
        
        if (tileProperties != null)
        {
            tileProperties.tileYPosition = transform.position.y;
        }
    }
    
    public void SetCoordinates(int newX, int newZ)
    {
        x = newX;
        z = newZ;
    }

    #region HIGHLIGHT SYSTEM
    
    public void ShowMovementHighlight()
    {
        if (highlightSystem != null)
        {
            highlightSystem.ShowMovementHighlight();
        }
    }
    
    public void ShowAttackHighlight()
    {
        if (highlightSystem != null)
        {
            highlightSystem.ShowAttackHighlight();
        }
    }
    
    public void ShowSupportHighlight()
    {
        if (highlightSystem != null)
        {
            highlightSystem.ShowSupportHighlight();
        }
    }
    public void HideAllHighlights()
    {
        if (highlightSystem != null)
        {
            highlightSystem.HideAllHighlights();
        }
    }
    
    #endregion
    
    #region TILE PROPERTIES
    
    public float GetWorldHeight()
    {
        return transform.position.y * 2f;
    }
    
    public float GetCenterY()
    {
        return transform.position.y;
    }
    
    public TileSpecialType GetSpecialType()
    {
        return tileProperties != null ? tileProperties.specialType : TileSpecialType.Normal;
    }
    
    public bool HasObstacle()
    {
        Vector2Int gridPos = new Vector2Int(x, z);
        
        // --- CORREÇÃO DO AVISO ---
        GridManager gridManager = FindFirstObjectByType<GridManager>();
        
        if (gridManager != null)
        {
            return gridManager.obstacles.ContainsKey(gridPos);
        }
        
        return false;
    }
    
    public ObstacleProperties GetObstacle()
    {
        Vector2Int gridPos = new Vector2Int(x, z);
        
        // --- CORREÇÃO DO AVISO ---
        GridManager gridManager = FindFirstObjectByType<GridManager>();
        
        if (gridManager != null && gridManager.obstacles.TryGetValue(gridPos, out GameObject obstacleObj))
        {
            return obstacleObj.GetComponent<ObstacleProperties>();
        }
        
        return null;
    }
    
    public bool IsWalkable()
    {
        if (!isWalkable) return false;
        
        ObstacleProperties obstacle = GetObstacle();
        if (obstacle != null)
        {
            return !obstacle.BlocksMovement();
        }
        
        return true;
    }
    
    #endregion
    
    #region UTILITY METHODS
    
    public Vector2Int GetGridPosition()
    {
        return new Vector2Int(x, z);
    }

    #endregion
    
    #region DEBUG
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position;
        Gizmos.DrawCube(center, Vector3.one * 0.9f);
        
        #if UNITY_EDITOR
        string info = $"Coords: ({x},{z})\nY: {transform.position.y:F3}";
        if (tileProperties != null)
        {
            info += $"\nSpecial: {tileProperties.specialType}";
        }
        UnityEditor.Handles.Label(center + Vector3.up * 1.5f, info);
        #endif
    }
    
    #endregion
}