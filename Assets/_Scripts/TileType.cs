// _Scripts/TileType.cs - Sistema Simplificado
using UnityEngine;

// REMOVIDO: TileHeight enum - agora usamos float direto

[System.Serializable]
public enum TileSpecialType
{
    Normal,
    Player1Spawn,
    Player2Spawn,
    BombsiteA,
    BombsiteB
}

[System.Serializable]
public enum ObstacleType
{
    None,
    Wall,     // Bloqueia movimento e tiro
    Box,      // Permite tiro, bloqueia movimento
    Door      // Permite movimento, bloqueia tiro
}

[System.Serializable]
public class MapTile
{
    public Vector2Int position;
    public string tilePrefabName;
    public int rotation;                  // 0, 90, 180, 270
    public float tileYPosition;          // NOVO: Posição Y exata do tile
    public TileSpecialType specialType;
    public ObstacleType obstacleType;
    public string obstaclePrefabName;
    public int obstacleRotation;
    public float obstacleYPosition;      // NOVO: Posição Y exata do obstáculo
    
    public MapTile(Vector2Int pos)
    {
        position = pos;
        tilePrefabName = "";
        rotation = 0;
        tileYPosition = 0.2f;            // Altura padrão
        specialType = TileSpecialType.Normal;
        obstacleType = ObstacleType.None;
        obstaclePrefabName = "";
        obstacleRotation = 0;
        obstacleYPosition = 0f;
    }
    
    public Quaternion GetTileRotation()
    {
        return Quaternion.Euler(0, rotation, 0);
    }
    
    public Quaternion GetObstacleRotation()
    {
        return Quaternion.Euler(0, obstacleRotation, 0);
    }
    
    public bool HasValidTilePrefab()
    {
        return !string.IsNullOrEmpty(tilePrefabName);
    }
    
    public bool HasValidObstaclePrefab()
    {
        return obstacleType != ObstacleType.None && !string.IsNullOrEmpty(obstaclePrefabName);
    }
}