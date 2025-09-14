// _Scripts/TileProperties.cs - SEM GIZMOS (Tile.cs já cuida disso)
using UnityEngine;

public class TileProperties : MonoBehaviour
{
    [Header("Tile Data")]
    public TileSpecialType specialType = TileSpecialType.Normal;
    public int prefabIndex = 0;
    public int rotation = 0;
    
    [Header("Position Data")]
    public float tileYPosition = 0f;
    
    [Header("Visual")]
    public Material customMaterial;
    
    private Renderer tileRenderer;
    
    void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
    }
    
    void Start()
    {
        ApplyCustomMaterial();
        
        TileHighlightSystem highlightSystem = GetComponent<TileHighlightSystem>();
        if (highlightSystem != null)
        {
            highlightSystem.InitializeHighlightSystem();
        }
        
        tileYPosition = transform.position.y;
    }
    
    public float GetWorldHeight()
    {
        return transform.position.y * 2f;
    }
    
    public float GetCenterY()
    {
        return transform.position.y;
    }
    
    public void SetTileData(TileSpecialType special, int prefabIdx, int rot)
    {
        specialType = special;
        prefabIndex = prefabIdx;
        rotation = rot;
        tileYPosition = transform.position.y;
        
        transform.rotation = Quaternion.Euler(0, rotation, 0);
        
        TileHighlightSystem highlightSystem = GetComponent<TileHighlightSystem>();
        if (highlightSystem != null)
        {
            highlightSystem.UpdateHighlightPosition();
        }
    }
    
    public void SetYPosition(float yPos)
    {
        tileYPosition = yPos;
        Vector3 pos = transform.position;
        pos.y = yPos;
        transform.position = pos;
        
        TileHighlightSystem highlightSystem = GetComponent<TileHighlightSystem>();
        if (highlightSystem != null)
        {
            highlightSystem.UpdateHighlightPosition();
        }
    }
    
    public void SetCustomMaterial(Material material)
    {
        customMaterial = material;
        ApplyCustomMaterial();
    }
    
    private void ApplyCustomMaterial()
    {
        if (customMaterial != null && tileRenderer != null)
        {
            tileRenderer.material = customMaterial;
        }
    }
    
    public bool IsSpecialTile()
    {
        return specialType != TileSpecialType.Normal;
    }
    
    public bool IsSpawnTile()
    {
        return specialType == TileSpecialType.Player1Spawn || specialType == TileSpecialType.Player2Spawn;
    }
    
    public bool IsBombsiteTile()
    {
        return specialType == TileSpecialType.BombsiteA || specialType == TileSpecialType.BombsiteB;
    }
    
    public Vector2Int GetGridPosition()
    {
        Vector3 worldPos = transform.position;
        return new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.z));
    }
    
    // REMOVIDO TODO OnDrawGizmos - Tile.cs já faz isso
}