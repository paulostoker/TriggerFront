// _Scripts/MapEditorData.cs - Sistema Simplificado
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMapData", menuName = "Game/Map Editor Data")]
public class MapEditorData : ScriptableObject
{
    [Header("Grid Settings")]
    public int gridWidth = 20;
    public int gridHeight = 20;
    
    [Header("Map Data")]
    public List<MapTile> tiles = new List<MapTile>();
    public List<ExtraObject> extraObjects = new List<ExtraObject>();
    
    [Header("Metadata")]
    public string mapName = "New Map";
    public string mapDescription = "";
    public string authorName = "";
    
    void OnEnable()
    {
        if (tiles.Count == 0)
        {
            InitializeEmptyMap();
        }
    }
    
    public void InitializeEmptyMap()
    {
        tiles.Clear();
        extraObjects.Clear();
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                MapTile newTile = new MapTile(new Vector2Int(x, z));
                // SIMPLIFICADO: Define valores padrão diretos
                newTile.tilePrefabName = "DEFAULT_TILE";
                newTile.tileYPosition = 0.2f; // Centro do tile padrão
                newTile.obstacleYPosition = 0f;
                tiles.Add(newTile);
            }
        }
        
        Debug.Log($"MapEditorData: Initialized empty {gridWidth}x{gridHeight} map");
    }
    
    public MapTile GetTile(int x, int z)
    {
        return tiles.Find(t => t.position.x == x && t.position.y == z);
    }
    
    public MapTile GetTile(Vector2Int position)
    {
        return GetTile(position.x, position.y);
    }
    
    public void SetTile(int x, int z, MapTile newTile)
    {
        int index = tiles.FindIndex(t => t.position.x == x && t.position.y == z);
        if (index >= 0)
        {
            tiles[index] = newTile;
        }
        else
        {
            tiles.Add(newTile);
        }
    }
    
    public void SetTile(Vector2Int position, MapTile newTile)
    {
        SetTile(position.x, position.y, newTile);
    }
    
    public bool IsValidPosition(int x, int z)
    {
        return x >= 0 && x < gridWidth && z >= 0 && z < gridHeight;
    }
    
    public bool IsValidPosition(Vector2Int position)
    {
        return IsValidPosition(position.x, position.y);
    }
    
    #region EXTRA OBJECTS
    
    public void AddExtraObject(ExtraObject extraObj)
    {
        if (extraObj != null)
        {
            extraObjects.Add(extraObj);
        }
    }
    
    public void RemoveExtraObject(ExtraObject extraObj)
    {
        extraObjects.Remove(extraObj);
    }
    
    public void RemoveExtraObjectByName(string objectName)
    {
        extraObjects.RemoveAll(obj => obj.objectName == objectName);
    }
    
    public void ClearExtraObjects()
    {
        extraObjects.Clear();
    }
    
    public List<ExtraObject> GetExtraObjects()
    {
        return new List<ExtraObject>(extraObjects);
    }
    
    #endregion
    
    #region VALIDATION
    
    public List<Vector2Int> GetPlayer1SpawnPoints()
    {
        List<Vector2Int> spawns = new List<Vector2Int>();
        foreach (var tile in tiles)
        {
            if (tile.specialType == TileSpecialType.Player1Spawn)
            {
                spawns.Add(tile.position);
            }
        }
        return spawns;
    }
    
    public List<Vector2Int> GetPlayer2SpawnPoints()
    {
        List<Vector2Int> spawns = new List<Vector2Int>();
        foreach (var tile in tiles)
        {
            if (tile.specialType == TileSpecialType.Player2Spawn)
            {
                spawns.Add(tile.position);
            }
        }
        return spawns;
    }
    
    public List<Vector2Int> GetBombsiteATiles()
    {
        List<Vector2Int> bombsite = new List<Vector2Int>();
        foreach (var tile in tiles)
        {
            if (tile.specialType == TileSpecialType.BombsiteA)
            {
                bombsite.Add(tile.position);
            }
        }
        return bombsite;
    }
    
    public List<Vector2Int> GetBombsiteBTiles()
    {
        List<Vector2Int> bombsite = new List<Vector2Int>();
        foreach (var tile in tiles)
        {
            if (tile.specialType == TileSpecialType.BombsiteB)
            {
                bombsite.Add(tile.position);
            }
        }
        return bombsite;
    }
    
    // SIMPLIFICADO: Conta obstáculos ao invés de alturas
    public int GetObstacleCount(ObstacleType obstacleType)
    {
        return tiles.FindAll(t => t.obstacleType == obstacleType).Count;
    }
    
    // NOVO: Estatísticas de altura
    public float GetAverageHeight()
    {
        if (tiles.Count == 0) return 0f;
        
        float sum = 0f;
        foreach (var tile in tiles)
        {
            sum += tile.tileYPosition;
        }
        return sum / tiles.Count;
    }
    
    public float GetMinHeight()
    {
        if (tiles.Count == 0) return 0f;
        
        float min = float.MaxValue;
        foreach (var tile in tiles)
        {
            if (tile.tileYPosition < min)
                min = tile.tileYPosition;
        }
        return min;
    }
    
    public float GetMaxHeight()
    {
        if (tiles.Count == 0) return 0f;
        
        float max = float.MinValue;
        foreach (var tile in tiles)
        {
            if (tile.tileYPosition > max)
                max = tile.tileYPosition;
        }
        return max;
    }
    
    public bool ValidateMap(out List<string> errors)
    {
        errors = new List<string>();
        
        var player1Spawns = GetPlayer1SpawnPoints();
        if (player1Spawns.Count < 5)
        {
            errors.Add($"Player 1 needs at least 5 spawn points (found {player1Spawns.Count})");
        }
        
        var player2Spawns = GetPlayer2SpawnPoints();
        if (player2Spawns.Count < 5)
        {
            errors.Add($"Player 2 needs at least 5 spawn points (found {player2Spawns.Count})");
        }
        
        var bombsiteA = GetBombsiteATiles();
        var bombsiteB = GetBombsiteBTiles();
        
        if (bombsiteA.Count == 0)
        {
            errors.Add("Bombsite A must have at least 1 tile");
        }
        
        if (bombsiteB.Count == 0)
        {
            errors.Add("Bombsite B must have at least 1 tile");
        }
        
        if (tiles.Count != gridWidth * gridHeight)
        {
            errors.Add($"Map should have {gridWidth * gridHeight} tiles but has {tiles.Count}");
        }
        
        // NOVO: Validação de alturas
        foreach (var tile in tiles)
        {
            if (tile.tileYPosition < 0f)
            {
                errors.Add($"Tile at ({tile.position.x},{tile.position.y}) has negative Y position");
            }
        }
        
        return errors.Count == 0;
    }
    
    #endregion
    
    #region STATISTICS
    
    public void LogMapStatistics()
    {
        Debug.Log($"=== Map Statistics: {mapName} ===");
        Debug.Log($"Size: {gridWidth}x{gridHeight} ({tiles.Count} tiles)");
        Debug.Log($"Player 1 Spawns: {GetPlayer1SpawnPoints().Count}");
        Debug.Log($"Player 2 Spawns: {GetPlayer2SpawnPoints().Count}");
        Debug.Log($"Bombsite A: {GetBombsiteATiles().Count} tiles");
        Debug.Log($"Bombsite B: {GetBombsiteBTiles().Count} tiles");
        Debug.Log($"Extra Objects: {extraObjects.Count}");
        
        // NOVO: Estatísticas de altura
        Debug.Log($"Height Range: {GetMinHeight():F3} to {GetMaxHeight():F3}");
        Debug.Log($"Average Height: {GetAverageHeight():F3}");
        
        Debug.Log($"Walls: {GetObstacleCount(ObstacleType.Wall)}");
        Debug.Log($"Boxes: {GetObstacleCount(ObstacleType.Box)}");
        Debug.Log($"Doors: {GetObstacleCount(ObstacleType.Door)}");
    }
    
    #endregion
}

// Classe para objetos extras
[System.Serializable]
public class ExtraObject
{
    public string prefabName;
    public string objectName;
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
    
    public ExtraObject(string prefabName, string objectName, Transform transform)
    {
        this.prefabName = prefabName;
        this.objectName = objectName;
        this.position = transform.position;
        this.rotation = transform.eulerAngles;
        this.scale = transform.localScale;
    }
}