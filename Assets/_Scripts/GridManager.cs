using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq; // <-- LINHA ADICIONADA PARA CORRIGIR O ERRO
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GridManager : MonoBehaviour
{
    #region Serialized Fields
    [Header("Discovered Prefabs (Auto-Populated)")]
    [SerializeField] private List<GameObject> tilePrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> wallPrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> boxPrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> doorPrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> bombsiteIndicatorPrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> player1IndicatorPrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> player2IndicatorPrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> extraPrefabs = new List<GameObject>();
    #endregion

    #region Grid Data
    public Dictionary<Vector2Int, Tile> tiles = new Dictionary<Vector2Int, Tile>();
    public Dictionary<Vector2Int, GameObject> obstacles = new Dictionary<Vector2Int, GameObject>();
    public Dictionary<Vector2Int, GameObject> indicators = new Dictionary<Vector2Int, GameObject>();

    private Dictionary<Vector2Int, GameObject> tileObjects = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<string, float> tileHeightCache = new Dictionary<string, float>();
    #endregion

    #region Auto-Discovery System
    [ContextMenu("Refresh Prefabs from Folders")]
    public void RefreshPrefabsFromFolders()
    {
#if UNITY_EDITOR
        ClearPrefabLists();
        
        string basePath = "Assets/_Prefabs/Map/";
        
        LoadPrefabs(basePath + "Tiles/", tilePrefabs, "All Tiles");
        LoadPrefabs(basePath + "Obstacles/Wall/", wallPrefabs, "Wall Obstacles");
        LoadPrefabs(basePath + "Obstacles/Box/", boxPrefabs, "Box Obstacles");
        LoadPrefabs(basePath + "Obstacles/Door/", doorPrefabs, "Door Obstacles");
        LoadPrefabs(basePath + "Indicators/Bombsite/", bombsiteIndicatorPrefabs, "Bombsite Indicators");
        LoadPrefabs(basePath + "Indicators/Player1/", player1IndicatorPrefabs, "Player1 Indicators");
        LoadPrefabs(basePath + "Indicators/Player2/", player2IndicatorPrefabs, "Player2 Indicators");
        LoadPrefabs(basePath + "Extras/", extraPrefabs, "Extra Objects");
        
        CacheTileHeights();
        
        EditorUtility.SetDirty(this);
#endif
    }

#if UNITY_EDITOR
    private void LoadPrefabs(string folderPath, List<GameObject> targetList, string category)
    {
        if (!Directory.Exists(folderPath))
        {
            return;
        }
        
        string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        
        foreach (string guid in prefabGUIDs)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            
            if (prefab != null && !targetList.Contains(prefab))
            {
                targetList.Add(prefab);
            }
        }
    }
#endif

    private void ClearPrefabLists()
    {
        tilePrefabs.Clear();
        wallPrefabs.Clear();
        boxPrefabs.Clear();
        doorPrefabs.Clear();
        bombsiteIndicatorPrefabs.Clear();
        player1IndicatorPrefabs.Clear();
        player2IndicatorPrefabs.Clear();
        extraPrefabs.Clear();
    }

    private void CacheTileHeights()
    {
        tileHeightCache.Clear();
        
        foreach (var tilePrefab in tilePrefabs)
        {
            if (tilePrefab != null)
            {
                float height = MeasurePrefabHeight(tilePrefab);
                tileHeightCache[tilePrefab.name] = height;
            }
        }
    }

    public float MeasurePrefabHeight(GameObject prefab)
    {
        if (prefab == null) return 0.4f;
        
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return 0.4f;
        
        Bounds combinedBounds = renderers[0].bounds;
        foreach (var renderer in renderers)
        {
            combinedBounds.Encapsulate(renderer.bounds);
        }
        
        return combinedBounds.size.y;
    }
    #endregion

    #region Map Generation
    public void GenerateGridFromMapEditorData(MapEditorData mapData)
    {
        if (mapData == null || mapData.tiles.Count == 0)
        {
            return;
        }

        RefreshPrefabsFromFolders();
        ClearGrid();

        Transform parentTransform = GetCorrectParentForGameplay();

        foreach (MapTile tileData in mapData.tiles)
        {
            CreateTileFromData(tileData, false, parentTransform);
        }
        
        GenerateExtraObjects(mapData, parentTransform);
    }

    public void GenerateGridFromMapEditorDataForEditor(MapEditorData mapData)
    {
        if (mapData == null) return;

        RefreshPrefabsFromFolders();
        ClearGrid();

        Transform parentTransform = GetCorrectParentForEditor();

        foreach (MapTile tileData in mapData.tiles)
        {
            CreateTileFromData(tileData, true, parentTransform);
        }
        
        GenerateExtraObjectsForEditor(mapData, parentTransform);
    }

    Transform GetCorrectParentForGameplay()
    {
        GameObject mapParent = GameObject.Find("Map");
        if (mapParent != null)
        {
            return mapParent.transform;
        }
        
        return this.transform;
    }

    Transform GetCorrectParentForEditor()
    {
        GameObject editorParent = GameObject.Find("Map_EditorPreview");
        if (editorParent != null)
        {
            return editorParent.transform;
        }
        
        return this.transform;
    }

    private void CreateTileFromData(MapTile tileData, bool isEditor, Transform parentTransform)
    {
        Vector2Int pos = tileData.position;

        GameObject tileObj = CreateTileObject(tileData, parentTransform);
        if (tileObj != null)
        {
            tileObjects[pos] = tileObj;

            Tile tileScript = tileObj.GetComponent<Tile>();
            if (tileScript == null)
            {
                tileScript = tileObj.AddComponent<Tile>();
            }

            tileScript.SetCoordinates(pos.x, pos.y);
            tileScript.Initialize();
            tiles[pos] = tileScript;
        }

        if (tileData.obstacleType != ObstacleType.None)
        {
            GameObject obstacleObj = CreateObstacleObject(tileData, parentTransform);
            if (obstacleObj != null)
            {
                obstacles[pos] = obstacleObj;
            }
        }

        if (isEditor && tileData.specialType != TileSpecialType.Normal)
        {
            GameObject indicatorObj = CreateIndicatorObject(tileData, parentTransform);
            if (indicatorObj != null)
            {
                indicators[pos] = indicatorObj;
            }
        }
    }

    private GameObject CreateTileObject(MapTile tileData, Transform parentTransform)
    {
        if (string.IsNullOrEmpty(tileData.tilePrefabName) || tileData.tilePrefabName == "DEFAULT_TILE")
        {
            return null;
        }
        
        GameObject prefab = GetTilePrefabByName(tileData.tilePrefabName);
        if (prefab == null)
        {
            return null;
        }

        Vector3 worldPosition = new Vector3(
            tileData.position.x,
            tileData.tileYPosition,
            tileData.position.y
        );

        Quaternion rotation = tileData.GetTileRotation();

        GameObject tileObj = Instantiate(prefab, worldPosition, rotation, parentTransform);
        
        int tileLayer = 3;
        SetLayerRecursively(tileObj, tileLayer);

        tileObj.name = $"{prefab.name}_{tileData.position.x}_{tileData.position.y}";

        TileProperties tileProps = tileObj.GetComponent<TileProperties>();
        if (tileProps != null)
        {
            tileProps.SetTileData(tileData.specialType, 0, tileData.rotation);
            tileProps.tileYPosition = tileData.tileYPosition;
        }

        return tileObj;
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        
        obj.layer = newLayer;
        
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
    
    private GameObject CreateObstacleObject(MapTile tileData, Transform parentTransform)
    {
        GameObject prefab = GetObstaclePrefabByName(tileData.obstaclePrefabName);
        if (prefab == null)
        {
            return null;
        }

        Vector3 worldPosition = new Vector3(
            tileData.position.x,
            tileData.obstacleYPosition,
            tileData.position.y
        );

        Quaternion rotation = tileData.GetObstacleRotation();

        GameObject obstacleObj = Instantiate(prefab, worldPosition, rotation, parentTransform);
        
        obstacleObj.name = $"{prefab.name}_{tileData.position.x}_{tileData.position.y}";

        ObstacleProperties obstacleProps = obstacleObj.GetComponent<ObstacleProperties>();
        if (obstacleProps != null)
        {
            obstacleProps.SetObstacleData(tileData.obstacleType, 0, tileData.obstacleRotation);
        }

        return obstacleObj;
    }

    private GameObject CreateIndicatorObject(MapTile tileData, Transform parentTransform)
    {
        List<GameObject> indicatorPrefabs = GetIndicatorPrefabs(tileData.specialType);
        if (indicatorPrefabs.Count == 0) return null;

        GameObject prefab = indicatorPrefabs[0];
        
        Vector3 worldPosition = new Vector3(
            tileData.position.x,
            tileData.tileYPosition + 0.1f,
            tileData.position.y
        );

        GameObject indicatorObj = Instantiate(prefab, worldPosition, Quaternion.identity, parentTransform);
        
        indicatorObj.name = $"{prefab.name}_{tileData.position.x}_{tileData.position.y}";

        return indicatorObj;
    }

    private void GenerateExtraObjects(MapEditorData mapData, Transform parentTransform)
    {
        if (mapData.extraObjects == null || mapData.extraObjects.Count == 0) return;

        Transform extrasParent = parentTransform.Find("Extras");
        if (extrasParent == null)
        {
            GameObject extrasContainer = new GameObject("Extras");
            extrasContainer.transform.SetParent(parentTransform);
            extrasParent = extrasContainer.transform;
        }

        foreach (var extraData in mapData.extraObjects)
        {
            GameObject prefab = GetExtraPrefabByName(extraData.prefabName);
            if (prefab == null)
            {
                continue;
            }

            GameObject extraObj = Instantiate(prefab, extrasParent);
            
            extraObj.name = extraData.objectName;
            extraObj.transform.position = extraData.position;
            extraObj.transform.eulerAngles = extraData.rotation;
            extraObj.transform.localScale = extraData.scale;
        }
    }

    private void GenerateExtraObjectsForEditor(MapEditorData mapData, Transform parentTransform)
    {
        GenerateExtraObjects(mapData, parentTransform);
    }

    private void ClearGrid()
    {
        foreach (Transform child in transform)
        {
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }

        tiles.Clear();
        obstacles.Clear();
        indicators.Clear();
        tileObjects.Clear();
    }
    #endregion

    #region Helper Methods
    public GameObject GetTilePrefabByName(string prefabName)
    {
        return tilePrefabs.Find(p => p != null && p.name == prefabName);
    }

    public GameObject GetObstaclePrefabByName(string prefabName)
    {
        var allObstacles = new List<GameObject>();
        allObstacles.AddRange(wallPrefabs);
        allObstacles.AddRange(boxPrefabs);
        allObstacles.AddRange(doorPrefabs);
        
        return allObstacles.Find(p => p != null && p.name == prefabName);
    }

    public GameObject GetExtraPrefabByName(string prefabName)
    {
        return extraPrefabs.Find(p => p != null && p.name == prefabName);
    }

    private List<GameObject> GetObstaclePrefabList(ObstacleType obstacleType)
    {
        return obstacleType switch
        {
            ObstacleType.Wall => wallPrefabs,
            ObstacleType.Box => boxPrefabs,
            ObstacleType.Door => doorPrefabs,
            _ => new List<GameObject>()
        };
    }

    private List<GameObject> GetIndicatorPrefabs(TileSpecialType specialType)
    {
        return specialType switch
        {
            TileSpecialType.BombsiteA or TileSpecialType.BombsiteB => bombsiteIndicatorPrefabs,
            TileSpecialType.Player1Spawn => player1IndicatorPrefabs,
            TileSpecialType.Player2Spawn => player2IndicatorPrefabs,
            _ => new List<GameObject>()
        };
    }

    public float GetCachedTileHeight(string tilePrefabName)
    {
        if (tileHeightCache.TryGetValue(tilePrefabName, out float height))
        {
            return height;
        }
        
        GameObject prefab = GetTilePrefabByName(tilePrefabName);
        if (prefab != null)
        {
            height = MeasurePrefabHeight(prefab);
            tileHeightCache[tilePrefabName] = height;
            return height;
        }
        
        return 0.4f;
    }
    #endregion

    #region Posicionamento & Validação
     public GameObject GetObjectAtTile(Vector2Int gridPosition)
    {
        Vector3 raycastStart = new Vector3(gridPosition.x, 50f, gridPosition.y);
        
        // --- A CORREÇÃO FINAL ---
        // Adicionamos 'QueryTriggerInteraction.Collide' para garantir que o raycast
        // detecte TODOS os colliders na layer especificada, ignorando as configurações globais de física.
        RaycastHit[] hits = Physics.RaycastAll(raycastStart, Vector3.down, 100f, GameConfig.Instance.pieceLayerMask, QueryTriggerInteraction.Collide);

        if (hits.Length > 0)
        {
            var orderedHits = hits.OrderBy(h => h.distance).ToArray();

            foreach (var hit in orderedHits)
            {
                if (hit.collider.GetComponentInParent<PieceProperties>() != null || hit.collider.GetComponentInParent<SpawnedEffect>() != null)
                {
                    return hit.collider.GetComponentInParent<MonoBehaviour>().gameObject;
                }
            }
        }
        
        return null;
    }
    
    public Vector3 GetFreelancerPositionOnTile(Vector2Int gridPosition)
    {
        if (tiles.TryGetValue(gridPosition, out Tile tile))
        {
            return tile.transform.position + Vector3.up * 0.5f;
        }
        
        return new Vector3(gridPosition.x, 0.7f, gridPosition.y);
    }

    public Vector3 GetFreelancerPositionOnTile(Tile tile)
    {
        if (tile == null) return Vector3.zero;
        return GetFreelancerPositionOnTile(new Vector2Int(tile.x, tile.z));
    }

    public bool IsTileBlockedByObstacle(Vector2Int gridPosition)
    {
        if (obstacles.TryGetValue(gridPosition, out GameObject obstacleObj))
        {
            ObstacleProperties obstacleProps = obstacleObj.GetComponent<ObstacleProperties>();
            if (obstacleProps != null && obstacleProps.BlocksMovement())
            {
                return true;
            }
        }
        
        GameObject objectOnTile = GetObjectAtTile(gridPosition);
        if (objectOnTile != null)
        {
            SpawnedEffect spawnedEffect = objectOnTile.GetComponent<SpawnedEffect>();
            if (spawnedEffect != null && spawnedEffect.data.blocksMovement)
            {
                return true;
            }
        }
        
        return false;
    }

    public bool IsTileBlockedByObstacle(Tile tile)
    {
        if (tile == null) return false;
        return IsTileBlockedByObstacle(new Vector2Int(tile.x, tile.z));
    }

    public bool HasLineOfSightBetweenTiles(Vector2Int start, Vector2Int end, int penetrationPower = 0)
    {
        Vector3 startCenter = GridToWorld(start, GameConfig.Instance.losRayHeight);
        Vector3 endCenter = GridToWorld(end, GameConfig.Instance.losRayHeight);
        LayerMask obstacleMask = GameConfig.Instance.losObstacleLayerMask;

        if (penetrationPower <= 0)
        {
            Vector3 centralDirection = (endCenter - startCenter).normalized;
            float centralDistance = Vector3.Distance(startCenter, endCenter);

            if (centralDistance < 1f)
            {
                return !Physics.Raycast(startCenter, centralDirection, centralDistance, obstacleMask);
            }

            if (!Physics.Raycast(startCenter, centralDirection, centralDistance, obstacleMask))
            {
                return true;
            }

            Vector3 perpendicular = new Vector3(-centralDirection.z, 0, centralDirection.x) * GameConfig.Instance.raycastOffset;
            Vector3 endLeft = endCenter + perpendicular;
            Vector3 dirLeft = (endLeft - startCenter).normalized;
            float distLeft = Vector3.Distance(startCenter, endLeft);
            if (!Physics.Raycast(startCenter, dirLeft, distLeft, obstacleMask))
            {
                return true;
            }

            Vector3 endRight = endCenter - perpendicular;
            Vector3 dirRight = (endRight - startCenter).normalized;
            float distRight = Vector3.Distance(startCenter, endRight);
            if (!Physics.Raycast(startCenter, dirRight, distRight, obstacleMask))
            {
                return true;
            }

            return false;
        }
        else
        {
            if (CanPathPenetrate(startCenter, endCenter, obstacleMask, penetrationPower)) return true;

            Vector3 centralDirection = (endCenter - startCenter).normalized;
            Vector3 perpendicular = new Vector3(-centralDirection.z, 0, centralDirection.x) * GameConfig.Instance.raycastOffset;

            Vector3 endLeft = endCenter + perpendicular;
            if (CanPathPenetrate(startCenter, endLeft, obstacleMask, penetrationPower)) return true;

            Vector3 endRight = endCenter - perpendicular;
            if (CanPathPenetrate(startCenter, endRight, obstacleMask, penetrationPower)) return true;

            return false;
        }
    }

    private bool CanPathPenetrate(Vector3 start, Vector3 end, LayerMask obstacleMask, int penetrationPower)
    {
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);

        RaycastHit[] hits = Physics.RaycastAll(start, direction, distance, obstacleMask);

        int blockingObstacleCount = 0;
        foreach (var hit in hits)
        {
            ObstacleProperties obstacle = hit.collider.GetComponentInParent<ObstacleProperties>();
            if (obstacle != null && obstacle.BlocksLineOfSight())
            {
                blockingObstacleCount++;
            }
        }

        return blockingObstacleCount <= penetrationPower;
    }

    public int CountBlockingObstaclesInPath(Vector2Int start, Vector2Int end)
    {
        Vector3 startWorld = GridToWorld(start, GameConfig.Instance.losRayHeight);
        Vector3 endWorld = GridToWorld(end, GameConfig.Instance.losRayHeight);
        Vector3 direction = (endWorld - startWorld).normalized;
        float distance = Vector3.Distance(startWorld, endWorld);

        RaycastHit[] hits = Physics.RaycastAll(startWorld, direction, distance, GameConfig.Instance.losObstacleLayerMask);

        int blockingObstacleCount = 0;
        foreach (var hit in hits)
        {
            ObstacleProperties obstacle = hit.collider.GetComponentInParent<ObstacleProperties>();
            if (obstacle != null && obstacle.BlocksLineOfSight())
            {
                blockingObstacleCount++;
            }
        }
        return blockingObstacleCount;
    }

    public List<GameObject> GetPiecesInLineOfFire(Tile startTile, Tile endTile)
    {
        List<GameObject> piecesFound = new List<GameObject>();
        List<Tile> tilesInLine = new List<Tile>();
        
        int dx = endTile.x - startTile.x;
        int dy = endTile.z - startTile.z;

        int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
        if (steps == 0) return piecesFound;

        float xIncrement = (float)dx / steps;
        float yIncrement = (float)dy / steps;

        float x = startTile.x;
        float y = startTile.z;

        for (int i = 0; i <= steps; i++)
        {
            Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
            Tile currentTile = GetTileAtGridPosition(gridPos);

            if (currentTile != null && !tilesInLine.Contains(currentTile))
            {
                tilesInLine.Add(currentTile);
                GameObject pieceOnTile = ServiceLocator.Pieces.GetPieceAtTile(currentTile);
                if (pieceOnTile != null && pieceOnTile != ServiceLocator.Combat.GetCurrentAttacker())
                {
                    piecesFound.Add(pieceOnTile);
                }
            }
            
            x += xIncrement;
            y += yIncrement;
        }
        
        return piecesFound;
    }
    #endregion

    #region Pathfinding & Utilitários
    public List<Tile> FindPathableTiles(Tile startTile, int range, out Dictionary<Tile, Tile> cameFromMap)
    {
        cameFromMap = new Dictionary<Tile, Tile>();
        var pathableTiles = new List<Tile>();
        Queue<(Tile, int)> queue = new Queue<(Tile, int)>();
        HashSet<Tile> visited = new HashSet<Tile>();

        if (startTile == null) return pathableTiles;

        queue.Enqueue((startTile, 0));
        visited.Add(startTile);
        cameFromMap[startTile] = null;

        while (queue.Count > 0)
        {
            var (currentTile, distance) = queue.Dequeue();
            if (distance > 0) { pathableTiles.Add(currentTile); }

            if (distance < range)
            {
                Vector2Int[] neighbors = {
                    new Vector2Int(currentTile.x, currentTile.z + 1),
                    new Vector2Int(currentTile.x, currentTile.z - 1),
                    new Vector2Int(currentTile.x + 1, currentTile.z),
                    new Vector2Int(currentTile.x - 1, currentTile.z)
                };

                foreach (var neighborPos in neighbors)
                {
                    if (tiles.TryGetValue(neighborPos, out Tile neighborTile) &&
                        !visited.Contains(neighborTile) &&
                        neighborTile.IsWalkable() &&
                        !IsTileBlockedByObstacle(neighborPos))
                    {
                        visited.Add(neighborTile);
                        queue.Enqueue((neighborTile, distance + 1));
                        cameFromMap[neighborTile] = currentTile;
                    }
                }
            }
        }
        return pathableTiles;
    }

    public List<Tile> FindTilesInRange(Tile startTile, int range)
    {
        List<Tile> inRangeTiles = new List<Tile>();
        if (startTile == null) return inRangeTiles;

        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                if (Mathf.Abs(x) + Mathf.Abs(z) <= range)
                {
                    Vector2Int tilePos = new Vector2Int(startTile.x + x, startTile.z + z);
                    if (tiles.TryGetValue(tilePos, out Tile tile))
                    {
                        inRangeTiles.Add(tile);
                    }
                }
            }
        }
        return inRangeTiles;
    }

    public Tile GetTileUnderPiece(GameObject piece)
    {
        if (piece == null) return null;

        Vector3 piecePos = piece.transform.position;
        Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(piecePos.x), Mathf.RoundToInt(piecePos.z));

        tiles.TryGetValue(gridPos, out Tile tile);
        
        return tile;
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        return new Vector2Int(Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.z));
    }

    public Vector3 GridToWorld(Vector2Int gridPosition, float yPosition = 0f)
    {
        return new Vector3(gridPosition.x, yPosition, gridPosition.y);
    }

    public bool IsValidGridPosition(Vector2Int gridPosition)
    {
        return tiles.ContainsKey(gridPosition);
    }

    public Tile GetTileAtGridPosition(Vector2Int gridPosition)
    {
        tiles.TryGetValue(gridPosition, out Tile tile);
        return tile;
    }

    public List<Tile> GetNeighborTiles(Tile centerTile)
    {
        List<Tile> neighbors = new List<Tile>();
        if (centerTile == null) return neighbors;

        Vector2Int[] neighborPositions = {
            new Vector2Int(centerTile.x + 1, centerTile.z),
            new Vector2Int(centerTile.x - 1, centerTile.z),
            new Vector2Int(centerTile.x, centerTile.z + 1),
            new Vector2Int(centerTile.x, centerTile.z - 1)
        };

        foreach (var pos in neighborPositions)
        {
            if (tiles.TryGetValue(pos, out Tile neighbor))
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    public int GetManhattanDistance(Tile tile1, Tile tile2)
    {
        if (tile1 == null || tile2 == null) return int.MaxValue;
        return Mathf.Abs(tile1.x - tile2.x) + Mathf.Abs(tile1.z - tile2.z);
    }

    public int GetManhattanDistance(Vector2Int pos1, Vector2Int pos2)
    {
        return Mathf.Abs(pos1.x - pos2.x) + Mathf.Abs(pos1.y - pos2.y);
    }

    public bool AreAdjacent(Tile tile1, Tile tile2)
    {
        return GetManhattanDistance(tile1, tile2) == 1;
    }
    #endregion

    #region Acessores Públicos
    public int GetTilePrefabCount()
    {
        return tilePrefabs.Count;
    }

    public int GetObstaclePrefabCount(ObstacleType obstacleType)
    {
        return GetObstaclePrefabList(obstacleType).Count;
    }

    public GameObject GetTilePrefab(int index)
    {
        if (index >= 0 && index < tilePrefabs.Count)
        {
            return tilePrefabs[index];
        }
        return null;
    }

    public GameObject GetObstaclePrefab(ObstacleType obstacleType, int index)
    {
        List<GameObject> prefabList = GetObstaclePrefabList(obstacleType);
        if (index >= 0 && index < prefabList.Count)
        {
            return prefabList[index];
        }
        return null;
    }

    public List<string> GetTilePrefabNames()
    {
        List<string> names = new List<string>();
        foreach (GameObject prefab in tilePrefabs)
        {
            names.Add(prefab != null ? prefab.name : "Missing Prefab");
        }
        return names;
    }

    public List<string> GetObstaclePrefabNames(ObstacleType obstacleType)
    {
        List<string> names = new List<string>();
        List<GameObject> prefabList = GetObstaclePrefabList(obstacleType);
        foreach (GameObject prefab in prefabList)
        {
            names.Add(prefab != null ? prefab.name : "Missing Prefab");
        }
        return names;
    }

    public List<string> GetExtraPrefabNames()
    {
        List<string> names = new List<string>();
        foreach (GameObject prefab in extraPrefabs)
        {
            names.Add(prefab != null ? prefab.name : "Missing Prefab");
        }
        return names;
    }

    public List<GameObject> GetAllTilePrefabs() => new List<GameObject>(tilePrefabs);
    public List<GameObject> GetAllObstaclePrefabs(ObstacleType type) => new List<GameObject>(GetObstaclePrefabList(type));
    public List<GameObject> GetAllExtraPrefabs() => new List<GameObject>(extraPrefabs);
    #endregion
}