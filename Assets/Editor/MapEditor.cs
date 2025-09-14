#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class MapEditor : EditorWindow
{
    private enum EditMode
    {
        Select,
        PaintTiles,
        PaintObstacles,
        PaintHeight,
        RotateTiles,
        RotateObstacles
    }

    private MapEditorData currentMapData;
    private bool hasUnsavedChanges = false;
    private Dictionary<Vector2Int, MapTile> editingTiles = new Dictionary<Vector2Int, MapTile>();
    private Dictionary<string, string> editingMetadata = new Dictionary<string, string>();

    private GridManager gridManager;
    
    private EditMode currentEditMode = EditMode.Select;
    
    // Configurações de pintura
    private int selectedTilePrefabIndex = 0;
    private int selectedTileRotation = 0;
    private TileSpecialType selectedSpecialType = TileSpecialType.Normal;
    private ObstacleType selectedObstacleType = ObstacleType.None;
    private int selectedObstaclePrefabIndex = 0;
    private int selectedObstacleRotation = 0;
    private float paintHeight = 0.2f; // Altura Y para pintar
    
    // UI
    private Vector2 scrollPosition;
    private bool showMapSettings = true;
    private bool showModeSettings = true;
    private bool showValidation = true;
    
    // Cache de prefabs
    private List<string> tilePrefabNames = new List<string>();
    private List<string> wallPrefabNames = new List<string>();
    private List<string> boxPrefabNames = new List<string>();
    private List<string> doorPrefabNames = new List<string>();
    private List<string> extraPrefabNames = new List<string>();
    
    // Sistema de Undo/Redo
    private List<MapEditorState> undoStack = new List<MapEditorState>();
    private List<MapEditorState> redoStack = new List<MapEditorState>();
    private const int maxUndoSteps = 50;
    
    // Rastreamento de posições para detecção de mudanças manuais
    private Dictionary<string, Vector3> lastKnownPositions = new Dictionary<string, Vector3>();
    
    [MenuItem("Tools/Map Editor")]
    public static void OpenMapEditor()
    {
        MapEditor window = GetWindow<MapEditor>("Map Editor");
        window.minSize = new Vector2(400, 700);
        window.Show();
    }

    
    
    void OnEnable()
    {
        InitializeEditor();
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Selection.selectionChanged += OnSelectionChanged;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Selection.selectionChanged -= OnSelectionChanged;
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        //ConfirmAndSaveChanges();
    }

    void OnFocus()
    {
        InitializeEditor();
    }
    
    private void OnPlayModeStateChanged(PlayModeStateChange state)
{
    switch (state)
    {
        // Este é o momento exato em que o Unity sai do modo de edição para entrar no modo de jogo.
        case PlayModeStateChange.ExitingEditMode:
            // 1. PRIMEIRO, perguntamos ao usuário se ele quer salvar.
            bool canContinue = ConfirmAndSaveChanges();

            // 2. Verificamos a resposta do usuário.
            if (canContinue)
            {
                // Se ele clicou em "Save" ou "Discard", nós limpamos a cena.
                ClearSceneObjects();
            }
            else
            {
                // Se ele clicou em "Cancel", nós abortamos a entrada no modo de jogo.
                UnityEditor.EditorApplication.isPlaying = false;
                Debug.Log("<color=orange>[MapEditor]</color> Entrada no modo de jogo cancelada pelo usuário.");
            }
            break;

        case PlayModeStateChange.EnteredEditMode:
            // Esta parte já estava correta.
            ClearAllSceneObjects();
            currentMapData = null;
            hasUnsavedChanges = false;
            editingTiles.Clear();
            editingMetadata.Clear();
            ClearUndoRedo();
            break;
    }
}
    
    void InitializeEditor()
    {
        gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null) return;
        RefreshPrefabCache();
    }
    
void OnGUI()
{
    if (gridManager == null)
    {
        EditorGUILayout.HelpBox("GridManager não encontrado na cena.", MessageType.Error);
        return;
    }

    if (currentMapData != null)
    {
        DetectManualDeletions();
        SaveChangesToExtras();
    }

    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
    
    GUILayout.Label("Map Editor", EditorStyles.boldLabel);
    EditorGUILayout.Space();
    
    DrawMapControls();
    DrawEditModeControls();
    DrawModeSpecificSettings();
    DrawValidationSection();
    DrawDebugSection();
    
    EditorGUILayout.EndScrollView();
    
    ProcessKeyboardShortcuts();
}
    
    void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;
        
        if (currentEditMode == EditMode.Select)
            return; // Permite seleção normal do Unity
        
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 hitPoint = hit.point;
                int x = Mathf.RoundToInt(hitPoint.x);
                int z = Mathf.RoundToInt(hitPoint.z);
                
                if (currentMapData != null && currentMapData.IsValidPosition(x, z))
                {
                    ProcessTileClick(x, z, e);
                    e.Use();
                }
            }
        }
        
        sceneView.Repaint();
    }

    #region SISTEMA DE MODOS

    void DrawEditModeControls()
    {
        showModeSettings = EditorGUILayout.Foldout(showModeSettings, "Edit Modes", true);
        if (!showModeSettings) return;
        
        EditorGUILayout.BeginVertical("box");
        
        GUILayout.Label("Current Mode:", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(currentEditMode == EditMode.Select, "Select", GUI.skin.button))
            SetEditMode(EditMode.Select);
        if (GUILayout.Toggle(currentEditMode == EditMode.PaintTiles, "Paint Tiles", GUI.skin.button))
            SetEditMode(EditMode.PaintTiles);
        if (GUILayout.Toggle(currentEditMode == EditMode.PaintObstacles, "Paint Obstacles", GUI.skin.button))
            SetEditMode(EditMode.PaintObstacles);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(currentEditMode == EditMode.PaintHeight, "Paint Height", GUI.skin.button))
            SetEditMode(EditMode.PaintHeight);
        if (GUILayout.Toggle(currentEditMode == EditMode.RotateTiles, "Rotate Tiles", GUI.skin.button))
            SetEditMode(EditMode.RotateTiles);
        if (GUILayout.Toggle(currentEditMode == EditMode.RotateObstacles, "Rotate Obstacles", GUI.skin.button))
            SetEditMode(EditMode.RotateObstacles);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(GetModeDescription(currentEditMode), MessageType.Info);
        
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = undoStack.Count > 0;
        if (GUILayout.Button("Undo (Ctrl+Z)")) PerformUndo();
        GUI.enabled = redoStack.Count > 0;
        if (GUILayout.Button("Redo (Ctrl+Y)")) PerformRedo();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }

    void DrawModeSpecificSettings()
    {
        if (currentEditMode == EditMode.Select) return;
        
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label($"Settings - {currentEditMode}", EditorStyles.boldLabel);
        
        switch (currentEditMode)
        {
            case EditMode.PaintTiles:
                DrawTileSettings();
                break;
            case EditMode.PaintObstacles:
                DrawObstacleSettings();
                break;
            case EditMode.PaintHeight:
                DrawHeightSettings();
                break;
            case EditMode.RotateTiles:
            case EditMode.RotateObstacles:
                DrawRotationSettings();
                break;
        }
        
        EditorGUILayout.EndVertical();
    }

    void SetEditMode(EditMode newMode)
    {
        if (currentEditMode != newMode)
        {
            currentEditMode = newMode;
            SceneView.RepaintAll();
        }
    }

    void OnSelectionChanged()
    {
        if (currentEditMode == EditMode.Select && Selection.activeGameObject != null)
        {
            CheckForManualChanges();
        }
    }

    void OnHierarchyChanged()
    {
        if (currentEditMode == EditMode.Select)
        {
            CheckForManualChanges();
        }
    }

    // NOVO: Sistema aprimorado de detecção de mudanças manuais
    void CheckForManualChanges()
{
    if (currentMapData == null) return;
    
    GameObject mapParentObj = GameObject.Find("Map_EditorPreview");
    if (mapParentObj == null) return;
    
    Transform mapParent = mapParentObj.transform;
    bool foundChanges = false;
    
    for (int i = 0; i < mapParent.childCount; i++)
    {
        Transform child = mapParent.GetChild(i);
        // Ignora o container de extras e os indicadores visuais
        if (child.name == "Extras" || child.name.StartsWith("Indicator")) continue;
        
        string uniqueKey = child.name;
        Vector3 currentPos = child.position;
        
        // Verifica se a posição Y mudou
        if (lastKnownPositions.TryGetValue(uniqueKey, out Vector3 lastPos))
        {
            if (Mathf.Abs(currentPos.y - lastPos.y) > 0.001f)
            {
                // Extrai coordenadas do nome
                if (TryExtractCoordinates(child.name, out Vector2Int gridPos))
                {

                    if (child.GetComponent<ObstacleProperties>() != null)
                    {
                        UpdateObstacleHeight(gridPos, currentPos.y);
                    }
                    else if (child.GetComponent<Tile>() != null) // Verificação extra para ter certeza que é um tile
                    {
                        UpdateTileHeight(gridPos, currentPos.y);
                    }
                    
                    foundChanges = true;
                    Debug.Log($"<color=orange>[Manual Change]</color> {child.name} moved to Y={currentPos.y:F3}");
                }
            }
        }
        
        lastKnownPositions[uniqueKey] = currentPos;
    }
    
    if (foundChanges)
    {
        hasUnsavedChanges = true;
        Repaint();
    }
}

    bool TryExtractCoordinates(string objectName, out Vector2Int coords)
    {
        coords = Vector2Int.zero;
        string[] parts = objectName.Split('_');
        if (parts.Length >= 3)
        {
            if (int.TryParse(parts[parts.Length - 2], out int x) && 
                int.TryParse(parts[parts.Length - 1], out int z))
            {
                coords = new Vector2Int(x, z);
                return true;
            }
        }
        return false;
    }

    bool IsObstacle(string objectName)
    {
        return objectName.Contains("Wall") || objectName.Contains("Box") || objectName.Contains("Door");
    }

    void UpdateTileHeight(Vector2Int pos, float newYPosition)
    {
        if (!editingTiles.ContainsKey(pos))
        {
            MapTile existingTile = currentMapData.GetTile(pos.x, pos.y);
            if (existingTile != null)
            {
                editingTiles[pos] = CloneMapTile(existingTile);
            }
            else
            {
                editingTiles[pos] = new MapTile(pos);
            }
        }
        
        editingTiles[pos].tileYPosition = newYPosition;
    }

    void UpdateObstacleHeight(Vector2Int pos, float newYPosition)
    {
        if (!editingTiles.ContainsKey(pos))
        {
            MapTile existingTile = currentMapData.GetTile(pos.x, pos.y);
            if (existingTile != null)
            {
                editingTiles[pos] = CloneMapTile(existingTile);
            }
        }
        
        if (editingTiles.ContainsKey(pos))
        {
            editingTiles[pos].obstacleYPosition = newYPosition;
        }
    }

    void InitializeKnownPositions()
    {
        lastKnownPositions.Clear();
        
        GameObject mapParentObj = GameObject.Find("Map_EditorPreview");
        if (mapParentObj == null) return;
        
        Transform mapParent = mapParentObj.transform;
        
        for (int i = 0; i < mapParent.childCount; i++)
        {
            Transform child = mapParent.GetChild(i);
            if (child.name == "Extras") continue;
            
            lastKnownPositions[child.name] = child.position;
        }
    }

    string GetModeDescription(EditMode mode)
    {
        return mode switch
        {
            EditMode.Select => "Select and move objects manually. Changes are automatically saved.",
            EditMode.PaintTiles => "Click to paint tiles with selected prefab.",
            EditMode.PaintObstacles => "Click to add obstacles on tiles using raycast positioning.",
            EditMode.PaintHeight => "Click to set tile height to specified value.",
            EditMode.RotateTiles => "Click to rotate tiles and their obstacles.",
            EditMode.RotateObstacles => "Click to rotate only obstacles.",
            _ => ""
        };
    }

    #endregion

    #region CONFIGURAÇÕES POR MODO

    void DrawTileSettings()
    {
        if (tilePrefabNames.Count > 0)
        {
            selectedTilePrefabIndex = EditorGUILayout.Popup("Tile Prefab", selectedTilePrefabIndex, tilePrefabNames.ToArray());
        }
        else
        {
            EditorGUILayout.HelpBox("No tile prefabs found", MessageType.Warning);
        }
        
        selectedSpecialType = (TileSpecialType)EditorGUILayout.EnumPopup("Special Type", selectedSpecialType);
    }

    void DrawObstacleSettings()
    {
        ObstacleType newObstacleType = (ObstacleType)EditorGUILayout.EnumPopup("Obstacle Type", selectedObstacleType);
        if (newObstacleType != selectedObstacleType)
        {
            selectedObstacleType = newObstacleType;
            selectedObstaclePrefabIndex = 0;
            RefreshPrefabCache();
        }
        
        if (selectedObstacleType != ObstacleType.None)
        {
            List<string> obstaclePrefabNames = GetObstaclePrefabNames(selectedObstacleType);
            if (obstaclePrefabNames.Count > 0)
            {
                selectedObstaclePrefabIndex = EditorGUILayout.Popup("Obstacle Prefab", selectedObstaclePrefabIndex, obstaclePrefabNames.ToArray());
            }
        }
    }

    void DrawHeightSettings()
{
    paintHeight = EditorGUILayout.FloatField("Height (Y)", paintHeight);
    
    paintHeight = Mathf.Max(0f, paintHeight);
    
    EditorGUILayout.HelpBox($"Click tiles to set Y position to {paintHeight:F2}", MessageType.Info);
}

    void DrawRotationSettings()
    {
        EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("0°", selectedTileRotation == 0 ? GUI.skin.box : GUI.skin.button)) 
            selectedTileRotation = 0;
        if (GUILayout.Button("90°", selectedTileRotation == 90 ? GUI.skin.box : GUI.skin.button)) 
            selectedTileRotation = 90;
        if (GUILayout.Button("180°", selectedTileRotation == 180 ? GUI.skin.box : GUI.skin.button)) 
            selectedTileRotation = 180;
        if (GUILayout.Button("270°", selectedTileRotation == 270 ? GUI.skin.box : GUI.skin.button)) 
            selectedTileRotation = 270;
        EditorGUILayout.EndHorizontal();
        
        if (currentEditMode == EditMode.RotateObstacles)
        {
            selectedObstacleRotation = selectedTileRotation;
        }
    }

    #endregion

    #region PROCESSAMENTO DE CLIQUES

    void ProcessTileClick(int x, int z, Event e)
    {
        if (currentEditMode == EditMode.Select) return;
        
        SaveStateForUndo();
        
        Vector2Int pos = new Vector2Int(x, z);
        
        switch (currentEditMode)
        {
            case EditMode.PaintTiles:
                PaintTile(pos);
                break;
            case EditMode.PaintObstacles:
                PaintObstacle(pos);
                break;
            case EditMode.PaintHeight:
                PaintTileHeight(pos);
                break;
            case EditMode.RotateTiles:
                RotateTile(pos);
                break;
            case EditMode.RotateObstacles:
                RotateObstacle(pos);
                break;
        }
        
        RefreshTileDisplay(x, z);
        hasUnsavedChanges = true;
    }

    void PaintTile(Vector2Int pos)
{
    if (tilePrefabNames.Count == 0) return;
    
    if (!editingTiles.ContainsKey(pos))
    {
        MapTile existingTile = currentMapData.GetTile(pos);
        editingTiles[pos] = existingTile != null ? CloneMapTile(existingTile) : new MapTile(pos);
    }
    
    editingTiles[pos].tilePrefabName = tilePrefabNames[selectedTilePrefabIndex];
    editingTiles[pos].specialType = selectedSpecialType;
    
    // --- CORREÇÃO APLICADA AQUI ---
    // Em vez de calcular a altura, definimos a posição Y como 0,
    // pois o pivô já foi ajustado nos prefabs.
    editingTiles[pos].tileYPosition = 0f;
}

    // NOVO: Sistema de raycast para posicionar obstáculos
    // NOVO: Sistema de raycast para posicionar obstáculos
void PaintObstacle(Vector2Int pos)
{
    if (selectedObstacleType == ObstacleType.None) return;

    List<string> obstaclePrefabNames = GetObstaclePrefabNames(selectedObstacleType);
    if (obstaclePrefabNames.Count == 0) return;

    if (!editingTiles.ContainsKey(pos))
    {
        MapTile existingTile = currentMapData.GetTile(pos);
        editingTiles[pos] = existingTile != null ? CloneMapTile(existingTile) : new MapTile(pos);
    }

    // 1. Criar a LayerMask para a layer "Tile"
    int tileLayerMask = LayerMask.GetMask("Tile");

    // Raycast de cima para baixo para encontrar o tile, agora com a máscara
    Ray ray = new Ray(new Vector3(pos.x, 100f, pos.y), Vector3.down);
    if (Physics.Raycast(ray, out RaycastHit hit, 200f, tileLayerMask)) // <--- ALTERAÇÃO AQUI
    {
        // Usa o ponto de colisão para posicionar o obstáculo
        editingTiles[pos].obstacleYPosition = hit.point.y;
        Debug.Log($"<color=green>[Raycast]</color> Obstacle positioned at Y={hit.point.y:F3} on a Tile.");
    }
    else
    {
        // Fallback: usa altura do tile
        float tileY = editingTiles.ContainsKey(pos) ? editingTiles[pos].tileYPosition : 0f;
        GameObject tilePrefab = gridManager.GetTilePrefabByName(GetCurrentTileData(pos.x, pos.y).tilePrefabName);
        if(tilePrefab != null)
        {
            float tileHeight = gridManager.MeasurePrefabHeight(tilePrefab);
            editingTiles[pos].obstacleYPosition = tileY + (tileHeight / 2f); // Topo do tile
        }
        Debug.Log($"<color=yellow>[Fallback]</color> No raycast hit on a tile, using tile height as fallback.");
    }

    editingTiles[pos].obstacleType = selectedObstacleType;
    editingTiles[pos].obstaclePrefabName = obstaclePrefabNames[selectedObstaclePrefabIndex];
    editingTiles[pos].obstacleRotation = selectedObstacleRotation;
}

    // NOVO: Paint Height com movimento sincronizado de obstáculos
    void PaintTileHeight(Vector2Int pos)
{
    MapTile tileData = GetCurrentTileData(pos.x, pos.y);
    if (tileData == null) return;
    
    if (!editingTiles.ContainsKey(pos))
    {
        editingTiles[pos] = CloneMapTile(tileData);
    }
    
    float oldTileY = editingTiles[pos].tileYPosition;
    
    // --- CORREÇÃO APLICADA AQUI ---
    // A nova posição Y é simplesmente o valor de 'paintHeight',
    // pois seu pivô já está na base do objeto.
    float newTileY = paintHeight;
    
    float heightDifference = newTileY - oldTileY;
    
    editingTiles[pos].tileYPosition = newTileY;
    
    // Se tem obstáculo, move junto
    if (editingTiles[pos].obstacleType != ObstacleType.None)
    {
        editingTiles[pos].obstacleYPosition += heightDifference;
        Debug.Log($"<color=cyan>[Height Sync]</color> Obstacle moved by {heightDifference:F3}");
    }
}

    void RotateTile(Vector2Int pos)
    {
        if (!editingTiles.ContainsKey(pos))
        {
            MapTile existingTile = currentMapData.GetTile(pos);
            editingTiles[pos] = existingTile != null ? CloneMapTile(existingTile) : new MapTile(pos);
        }
        
        editingTiles[pos].rotation = selectedTileRotation;
        editingTiles[pos].obstacleRotation = selectedTileRotation;
    }

    void RotateObstacle(Vector2Int pos)
    {
        if (!editingTiles.ContainsKey(pos))
        {
            MapTile existingTile = currentMapData.GetTile(pos);
            editingTiles[pos] = existingTile != null ? CloneMapTile(existingTile) : new MapTile(pos);
        }
        
        editingTiles[pos].obstacleRotation = selectedTileRotation;
    }

    #endregion

    #region SISTEMA DE UNDO/REDO

    void SaveStateForUndo()
    {
        MapEditorState state = new MapEditorState();
        state.tiles = new Dictionary<Vector2Int, MapTile>();
        
        foreach (var kvp in editingTiles)
        {
            state.tiles[kvp.Key] = CloneMapTile(kvp.Value);
        }
        
        undoStack.Add(state);
        redoStack.Clear();
        
        if (undoStack.Count > maxUndoSteps)
        {
            undoStack.RemoveAt(0);
        }
    }

    void PerformUndo()
    {
        if (undoStack.Count == 0) return;
        
        MapEditorState currentState = new MapEditorState();
        currentState.tiles = new Dictionary<Vector2Int, MapTile>();
        foreach (var kvp in editingTiles)
        {
            currentState.tiles[kvp.Key] = CloneMapTile(kvp.Value);
        }
        redoStack.Add(currentState);
        
        MapEditorState previousState = undoStack[undoStack.Count - 1];
        undoStack.RemoveAt(undoStack.Count - 1);
        
        editingTiles.Clear();
        foreach (var kvp in previousState.tiles)
        {
            editingTiles[kvp.Key] = CloneMapTile(kvp.Value);
        }
        
        RefreshAllTileDisplays();
        hasUnsavedChanges = true;
    }

    void PerformRedo()
    {
        if (redoStack.Count == 0) return;
        
        SaveStateForUndo();
        undoStack.RemoveAt(undoStack.Count - 1);
        
        MapEditorState nextState = redoStack[redoStack.Count - 1];
        redoStack.RemoveAt(redoStack.Count - 1);
        
        editingTiles.Clear();
        foreach (var kvp in nextState.tiles)
        {
            editingTiles[kvp.Key] = CloneMapTile(kvp.Value);
        }
        
        RefreshAllTileDisplays();
        hasUnsavedChanges = true;
    }

    void ClearUndoRedo()
    {
        undoStack.Clear();
        redoStack.Clear();
    }

    #endregion

    #region CONTROLES DE MAPA

    void DrawMapControls()
    {
        showMapSettings = EditorGUILayout.Foldout(showMapSettings, "Map Controls", true);
        if (!showMapSettings) return;
        
        EditorGUILayout.BeginVertical("box");
        
        GUI.enabled = false;
        EditorGUILayout.ObjectField("Current Map", currentMapData, typeof(MapEditorData), false);
        GUI.enabled = true;
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("New Map")) CreateNewMap();
        if (GUILayout.Button("Load Map")) LoadMap();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = hasUnsavedChanges && currentMapData != null;
        if (GUILayout.Button("Save Map")) SaveCurrentMap();
        GUI.enabled = currentMapData != null;
        if (GUILayout.Button("Save As...")) SaveMapAs();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Clear Scene Preview"))
        {
            if (EditorUtility.DisplayDialog("Clear Scene Preview", "Clear the map preview?", "Yes", "No"))
            {
                ClearSceneObjects();
                currentMapData = null;
                hasUnsavedChanges = false;
                editingTiles.Clear();
                editingMetadata.Clear();
                ClearUndoRedo();
            }
        }
        
        if (currentMapData != null)
        {
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            
            string newName = EditorGUILayout.TextField("Map Name", GetEditingMetadata("mapName", currentMapData.mapName));
            string newDesc = EditorGUILayout.TextField("Description", GetEditingMetadata("mapDescription", currentMapData.mapDescription));
            string newAuthor = EditorGUILayout.TextField("Author", GetEditingMetadata("authorName", currentMapData.authorName));
            
            if (EditorGUI.EndChangeCheck())
            {
                editingMetadata["mapName"] = newName;
                editingMetadata["mapDescription"] = newDesc;
                editingMetadata["authorName"] = newAuthor;
                hasUnsavedChanges = true;
            }
        }
        
        if (hasUnsavedChanges)
        {
            EditorGUILayout.HelpBox("Unsaved changes", MessageType.Warning);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawValidationSection()
    {
        showValidation = EditorGUILayout.Foldout(showValidation, "Map Validation", true);
        if (!showValidation) return;
        
        EditorGUILayout.BeginVertical("box");
        
        if (GUILayout.Button("Validate Map"))
        {
            ValidateCurrentMap();
        }
        
        if (currentMapData != null)
        {
            EditorGUILayout.LabelField("Statistics:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Player 1 Spawns: {GetCurrentPlayer1SpawnCount()}");
            EditorGUILayout.LabelField($"Player 2 Spawns: {GetCurrentPlayer2SpawnCount()}");
            EditorGUILayout.LabelField($"Bombsite A Tiles: {GetCurrentBombsiteACount()}");
            EditorGUILayout.LabelField($"Bombsite B Tiles: {GetCurrentBombsiteBCount()}");
            EditorGUILayout.LabelField($"Pending Changes: {editingTiles.Count}");
            EditorGUILayout.LabelField($"Undo Steps: {undoStack.Count}");
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void DrawDebugSection()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Debug Tools", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Refresh Prefab Cache"))
        {
            RefreshPrefabCache();
        }
        
        if (GUILayout.Button("Log Map Statistics"))
        {
            LogCurrentMapStatistics();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    void RefreshPrefabCache()
    {
        if (gridManager == null) return;
        
        gridManager.RefreshPrefabsFromFolders();
        tilePrefabNames = gridManager.GetTilePrefabNames();
        wallPrefabNames = gridManager.GetObstaclePrefabNames(ObstacleType.Wall);
        boxPrefabNames = gridManager.GetObstaclePrefabNames(ObstacleType.Box);
        doorPrefabNames = gridManager.GetObstaclePrefabNames(ObstacleType.Door);
        extraPrefabNames = gridManager.GetExtraPrefabNames();
        
        selectedTilePrefabIndex = Mathf.Clamp(selectedTilePrefabIndex, 0, Mathf.Max(0, tilePrefabNames.Count - 1));
        selectedObstaclePrefabIndex = Mathf.Clamp(selectedObstaclePrefabIndex, 0, Mathf.Max(0, GetObstaclePrefabNames(selectedObstacleType).Count - 1));
        
        Repaint();
    }
    
    List<string> GetObstaclePrefabNames(ObstacleType obstacleType)
    {
        return obstacleType switch {
            ObstacleType.Wall => wallPrefabNames,
            ObstacleType.Box => boxPrefabNames,
            ObstacleType.Door => doorPrefabNames,
            _ => new List<string>()
        };
    }

    #endregion

    #region GERENCIAMENTO DE ARQUIVOS

    void CreateNewMap()
    {
        if (!ConfirmAndSaveChanges()) return;
        
        string path = EditorUtility.SaveFilePanelInProject("Create New Map", "NewMap", "asset", "Choose location");
        if (!string.IsNullOrEmpty(path))
        {
            currentMapData = CreateInstance<MapEditorData>();
            currentMapData.InitializeEmptyMap();
            
            AssetDatabase.CreateAsset(currentMapData, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            editingTiles.Clear();
            editingMetadata.Clear();
            ClearUndoRedo();
            
            InitializeDefaultTiles();
            GenerateMapInEditorPreview();
            
            hasUnsavedChanges = false;
            
            Debug.Log($"New map created: {currentMapData.name}");
        }
    }
    
    void InitializeDefaultTiles()
{
    if (currentMapData == null || tilePrefabNames.Count == 0) return;
    
    string defaultTileName = tilePrefabNames[0];

    foreach (var tile in currentMapData.tiles)
    {
        tile.tilePrefabName = defaultTileName;
        // --- CORREÇÃO APLICADA AQUI ---
        tile.tileYPosition = 0f; // Define a posição Y padrão como 0.
        tile.specialType = TileSpecialType.Normal;
        tile.obstacleType = ObstacleType.None;
        tile.obstaclePrefabName = "";
        tile.rotation = 0;
        tile.obstacleRotation = 0;
        tile.obstacleYPosition = 0f;
    }
    
    Debug.Log($"<color=green>[MapEditor]</color> Initialized {currentMapData.tiles.Count} tiles");
}
    
    void LoadMap()
    {
        if (!ConfirmAndSaveChanges()) return;
        
        string path = EditorUtility.OpenFilePanelWithFilters("Load Map", "Assets", new string[] { "Map Data", "asset" });
        if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
        {
            path = "Assets" + path.Substring(Application.dataPath.Length);
            currentMapData = AssetDatabase.LoadAssetAtPath<MapEditorData>(path);
            
            if (currentMapData != null)
            {
                editingTiles.Clear();
                editingMetadata.Clear();
                ClearUndoRedo();
                
                ValidateAndFixMapTiles();
                GenerateMapInEditorPreview();
                
                hasUnsavedChanges = false;
                
                Debug.Log($"Map loaded: {currentMapData.name}");
            }
        }
    }
void DetectManualDeletions()
{
    if (currentMapData == null) return;

    Transform mapParent = GetOrCreateMapParent();
    bool deletionsFound = false;
    
    // Usamos uma cópia da lista para poder modificar 'editingTiles' sem erros.
    var tilesWithObstacles = currentMapData.tiles.Where(t => t.obstacleType != ObstacleType.None).ToList();

    foreach (var tileData in tilesWithObstacles)
    {
        // Se já há uma mudança pendente para este tile, pulamos, pois ela tem prioridade.
        if (editingTiles.ContainsKey(tileData.position)) continue;

        string expectedName = $"{tileData.obstaclePrefabName}_{tileData.position.x}_{tileData.position.y}";
        Transform obstacleTransform = mapParent.Find(expectedName);

        // Se o objeto não for encontrado na cena, ele foi deletado!
        if (obstacleTransform == null)
        {
            Debug.Log($"<color=red>[Deleção Manual Detectada]</color> Obstáculo em {tileData.position} foi removido da cena.");
            Vector2Int pos = tileData.position;

            // Registra a mudança no dicionário 'editingTiles' para que ela seja salva.
            editingTiles[pos] = CloneMapTile(tileData);
            editingTiles[pos].obstacleType = ObstacleType.None;
            editingTiles[pos].obstaclePrefabName = "";
            editingTiles[pos].obstacleYPosition = 0;

            deletionsFound = true;
        }
    }

    if (deletionsFound)
    {
        hasUnsavedChanges = true;
    }
}
    void ValidateAndFixMapTiles()
{
    if (currentMapData == null || tilePrefabNames.Count == 0) return;
    
    string defaultTileName = tilePrefabNames[0];
    int fixedTiles = 0;
    
    foreach (var tile in currentMapData.tiles)
    {
        // Mantemos a validação para o nome do prefab, isso é útil.
        if (string.IsNullOrEmpty(tile.tilePrefabName) || 
            tile.tilePrefabName == "DEFAULT_TILE" ||
            gridManager.GetTilePrefabByName(tile.tilePrefabName) == null)
        {
            tile.tilePrefabName = defaultTileName;
            
            // Se o prefab for inválido, também resetamos a altura para 0.
            tile.tileYPosition = 0f;
            
            fixedTiles++;
        }
        
        // --- BLOCO PROBLEMÁTICO REMOVIDO ---
        // Removemos a verificação "if (tile.tileYPosition <= 0f)".
        // Agora, o editor confiará no valor de Y que foi salvo no arquivo,
        // mesmo que seja 0.
    }
    
    if (fixedTiles > 0)
    {
        Debug.Log($"<color=yellow>[MapEditor]</color> Fixed {fixedTiles} tiles with invalid prefabs.");
        hasUnsavedChanges = true;
    }
}
    
private void SaveChangesToExtras()
{
    if (currentMapData == null) return;

    // Pega os dados atuais da cena
    List<ExtraObject> sceneExtras = new List<ExtraObject>();
    Transform extrasContainer = GetOrCreateMapParent().Find("Extras");

    if (extrasContainer != null)
    {
        for (int i = 0; i < extrasContainer.childCount; i++)
        {
            GameObject extraInstance = extrasContainer.GetChild(i).gameObject;
            if (extraInstance.GetComponent<ExtraProperties>() != null)
            {
                GameObject sourcePrefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(extraInstance);
                if (sourcePrefab != null)
                {
                    sceneExtras.Add(new ExtraObject(sourcePrefab.name, extraInstance.name, extraInstance.transform));
                }
            }
        }
    }

  
    bool hasChanges = false;
    if (sceneExtras.Count != currentMapData.extraObjects.Count)
    {
        hasChanges = true;
    }
    else
    {
        
    }


    if (hasChanges)
    {
        currentMapData.extraObjects.Clear();
        currentMapData.extraObjects.AddRange(sceneExtras);
        hasUnsavedChanges = true;
        Debug.Log($"<color=cyan>[Extras]</color> Mudanças detectadas! {currentMapData.extraObjects.Count} extras prontos para salvar.");
    }
}

    void SaveCurrentMap()
    {
        if (currentMapData == null) return;
        
        ApplyEditingChangesToAsset();
        
        EditorUtility.SetDirty(currentMapData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        hasUnsavedChanges = false;
        ShowNotification(new GUIContent("Map Saved!"));
        
        Debug.Log($"Map saved: {currentMapData.name}");
    }
    
    void SaveMapAs()
    {
        if (currentMapData == null) return;

        string path = EditorUtility.SaveFilePanelInProject("Save Map As", currentMapData.name + "_copy", "asset", "Choose location");
        if (!string.IsNullOrEmpty(path))
        {
            MapEditorData newMapData = CreateInstance<MapEditorData>();
            
            newMapData.gridWidth = currentMapData.gridWidth;
            newMapData.gridHeight = currentMapData.gridHeight;
            newMapData.mapName = GetEditingMetadata("mapName", currentMapData.mapName);
            newMapData.mapDescription = GetEditingMetadata("mapDescription", currentMapData.mapDescription);
            newMapData.authorName = GetEditingMetadata("authorName", currentMapData.authorName);
            
            newMapData.tiles = new List<MapTile>();
            foreach (var tile in currentMapData.tiles)
            {
                Vector2Int pos = tile.position;
                if (editingTiles.ContainsKey(pos))
                {
                    newMapData.tiles.Add(CloneMapTile(editingTiles[pos]));
                }
                else
                {
                    newMapData.tiles.Add(CloneMapTile(tile));
                }
            }
            
            newMapData.extraObjects = new List<ExtraObject>(currentMapData.extraObjects);
            
            AssetDatabase.CreateAsset(newMapData, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            currentMapData = newMapData;
            editingTiles.Clear();
            editingMetadata.Clear();
            ClearUndoRedo();
            hasUnsavedChanges = false;
            ShowNotification(new GUIContent("Map Saved As"));
            
            Debug.Log($"Map saved as: {newMapData.name}");
        }
    }

    bool ConfirmAndSaveChanges()
    {
        if (hasUnsavedChanges && currentMapData != null)
        {
            string mapName = GetEditingMetadata("mapName", currentMapData.mapName);
            if (string.IsNullOrEmpty(mapName)) mapName = "unnamed map";
            
            int choice = EditorUtility.DisplayDialogComplex("Unsaved Changes", 
                $"Save changes to '{mapName}'?", 
                "Save", "Discard", "Cancel");

            switch (choice) {
                case 0: SaveCurrentMap(); return true;
                case 1: return true;
                case 2: return false;
            }
        }
        return true;
    }

    #endregion

    #region GERAÇÃO E ATUALIZAÇÃO

    void GenerateMapInEditorPreview()
{
    if (currentMapData == null || gridManager == null)
    {
        ClearSceneObjects();
        return;
    }
    
    ClearSceneObjects();
    
    Transform mapParent = CreateMapEditorPreviewStructure();
    Transform extrasContainer = mapParent.Find("Extras"); // Pega o container de extras.

    // --- LÓGICA EXISTENTE PARA TILES E OBSTÁCULOS ---
    if (currentMapData.tiles != null && currentMapData.tiles.Count > 0)
    {
        foreach (var tile in currentMapData.tiles)
        {
            CreateTileObjectInEditor(tile, mapParent);
            if (tile.obstacleType != ObstacleType.None)
            {
                CreateObstacleObjectInEditor(tile, mapParent);
            }
            if (tile.specialType != TileSpecialType.Normal)
            {
                CreateIndicatorObjectInEditor(tile, mapParent);
            }
        }
    }
    
    // --- INÍCIO DA NOVA LÓGICA PARA CARREGAR EXTRAS ---
    if (currentMapData.extraObjects != null && extrasContainer != null)
    {
        foreach (var extraData in currentMapData.extraObjects)
        {
            GameObject prefab = gridManager.GetExtraPrefabByName(extraData.prefabName);
            if (prefab != null)
            {
                GameObject newExtraObj = PrefabUtility.InstantiatePrefab(prefab, extrasContainer) as GameObject;
                if (newExtraObj != null)
                {
                    // Aplica as transformações salvas
                    newExtraObj.transform.position = extraData.position;
                    newExtraObj.transform.rotation = Quaternion.Euler(extraData.rotation);
                    newExtraObj.transform.localScale = extraData.scale;
                    newExtraObj.name = extraData.objectName; // Restaura o nome completo

                    // Garante que o script marcador esteja presente
                    if (newExtraObj.GetComponent<ExtraProperties>() == null)
                    {
                        newExtraObj.AddComponent<ExtraProperties>();
                    }
                }
            }
        }
    }
    // --- FIM DA NOVA LÓGICA ---

    InitializeKnownPositions();
    Debug.Log($"<color=green>[MapEditor]</color> Preview gerado para '{currentMapData.mapName}'.");
}

    Transform CreateMapEditorPreviewStructure()
    {
        GameObject mapEditorPreview = new GameObject("Map_EditorPreview");
        GameObject extrasContainer = new GameObject("Extras");
        extrasContainer.transform.SetParent(mapEditorPreview.transform);
        
        return mapEditorPreview.transform;
    }

    Transform GetOrCreateMapParent()
    {
        GameObject mapParent = GameObject.Find("Map_EditorPreview");
        if (mapParent == null)
        {
            return CreateMapEditorPreviewStructure();
        }
        
        Transform extrasContainer = mapParent.transform.Find("Extras");
        if (extrasContainer == null)
        {
            GameObject extras = new GameObject("Extras");
            extras.transform.SetParent(mapParent.transform);
        }
        
        return mapParent.transform;
    }
    
   void ClearSceneObjects()
{
    GameObject mapParent = GameObject.Find("Map_EditorPreview");
    if (mapParent != null)
    {
        // SOLUÇÃO: Antes de destruir o pai, deselecionamos qualquer objeto
        // para garantir que nenhum filho esteja ativo no Inspector.
        UnityEditor.Selection.activeObject = null;
        
        // Usamos o método de Undo para destruir o objeto pai e todos os seus filhos.
        Undo.DestroyObjectImmediate(mapParent);
    }
}
    
    void ClearAllSceneObjects()
    {
        GameObject[] objectsToDestroy = {
            GameObject.Find("Map_EditorPreview"),
            GameObject.Find("Map")
        };
        
        foreach (var obj in objectsToDestroy)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
    }
    
    void RefreshTileDisplay(int x, int z)
    {
        if (currentMapData == null) return;
        
        Transform mapParent = GetOrCreateMapParent();
        
        RemoveExistingObjectsAtPosition(mapParent, x, z);
        
        MapTile tileData = GetCurrentTileData(x, z);
        if (tileData == null) return;
        
        CreateTileObjectInEditor(tileData, mapParent);
        
        if (tileData.obstacleType != ObstacleType.None)
        {
            CreateObstacleObjectInEditor(tileData, mapParent);
        }
        
        if (tileData.specialType != TileSpecialType.Normal)
        {
            CreateIndicatorObjectInEditor(tileData, mapParent);
        }
    }

    void RemoveExistingObjectsAtPosition(Transform mapParent, int x, int z)
{
    List<Transform> toDestroy = new List<Transform>();

    for (int i = mapParent.childCount - 1; i >= 0; i--)
    {
        Transform child = mapParent.GetChild(i);
        if (child == null || child.name == "Extras") continue;

        if (child.name.EndsWith($"_{x}_{z}"))
        {
            toDestroy.Add(child);
        }
    }

    if (toDestroy.Count > 0)
    {
        UnityEditor.Undo.SetCurrentGroupName("Remove Tile Objects");
        int group = UnityEditor.Undo.GetCurrentGroup();

        foreach (Transform obj in toDestroy)
        {
            if (obj == null) continue;

            if (UnityEditor.Selection.activeObject == obj.gameObject)
            {
                UnityEditor.Selection.activeObject = null;
            }
            
            UnityEditor.Undo.DestroyObjectImmediate(obj.gameObject);
        }

        UnityEditor.Undo.CollapseUndoOperations(group);
    }
}

    void RefreshAllTileDisplays()
    {
        if (currentMapData == null) return;
        
        foreach (var tile in currentMapData.tiles)
        {
            RefreshTileDisplay(tile.position.x, tile.position.y);
        }
    }

    private GameObject CreateTileObjectInEditor(MapTile tileData, Transform parent)
    {
        if (string.IsNullOrEmpty(tileData.tilePrefabName))
        {
            if (tilePrefabNames.Count > 0)
            {
                tileData.tilePrefabName = tilePrefabNames[0];
            }
            else
            {
                return null;
            }
        }
        
        GameObject prefab = gridManager.GetTilePrefabByName(tileData.tilePrefabName);
        if (prefab == null) 
        {
            if (tilePrefabNames.Count > 0)
            {
                string fallbackPrefab = tilePrefabNames[0];
                prefab = gridManager.GetTilePrefabByName(fallbackPrefab);
                tileData.tilePrefabName = fallbackPrefab;
            }
            
            if (prefab == null) return null;
        }
        
        Vector3 worldPosition = new Vector3(
            tileData.position.x, 
            tileData.tileYPosition, 
            tileData.position.y
        );
        
        Quaternion rotation = tileData.GetTileRotation();
        
        GameObject newTileObj = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (newTileObj == null) return null;

        newTileObj.transform.position = worldPosition;
        newTileObj.transform.rotation = rotation;
        
        Tile tileScript = newTileObj.GetComponent<Tile>();
        if (tileScript == null)
        {
            tileScript = newTileObj.AddComponent<Tile>();
        }
        
        tileScript.SetCoordinates(tileData.position.x, tileData.position.y);
        tileScript.Initialize();
        
        TileProperties tileProps = newTileObj.GetComponent<TileProperties>();
        if (tileProps != null)
        {
            tileProps.SetTileData(tileData.specialType, 0, tileData.rotation);
        }
        
        string correctName = $"{prefab.name}_{tileData.position.x}_{tileData.position.y}";
        newTileObj.name = correctName;
        
        return newTileObj;
    }

    private GameObject CreateObstacleObjectInEditor(MapTile tileData, Transform parent)
    {
        GameObject prefab = gridManager.GetObstaclePrefabByName(tileData.obstaclePrefabName);
        if (prefab == null) return null;
        
        Vector3 position = new Vector3(
            tileData.position.x, 
            tileData.obstacleYPosition,
            tileData.position.y
        );
        
        Quaternion rotation = tileData.GetObstacleRotation();
        
        GameObject newObstacleObj = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (newObstacleObj == null) return null;
        
        newObstacleObj.name = $"{prefab.name}_{tileData.position.x}_{tileData.position.y}";
        newObstacleObj.transform.position = position;
        newObstacleObj.transform.rotation = rotation;
        
        ObstacleProperties obstacleProps = newObstacleObj.GetComponent<ObstacleProperties>();
        if (obstacleProps != null)
        {
            obstacleProps.SetObstacleData(tileData.obstacleType, 0, tileData.obstacleRotation);
        }
        
        return newObstacleObj;
    }

    private GameObject CreateIndicatorObjectInEditor(MapTile tileData, Transform parent)
    {
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        indicator.transform.SetParent(parent);
        
        Vector3 worldPosition = new Vector3(
            tileData.position.x,
            tileData.tileYPosition + 0.25f,
            tileData.position.y
        );
        
        indicator.transform.position = worldPosition;
        indicator.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
        
        indicator.name = $"Indicator_{tileData.specialType}_{tileData.position.x}_{tileData.position.y}";
        
        Renderer renderer = indicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material indicatorMat = new Material(Shader.Find("Standard"));
            indicatorMat.color = GetIndicatorColor(tileData.specialType);
            renderer.material = indicatorMat;
        }
        
        Collider collider = indicator.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
        
        return indicator;
    }

    private Color GetIndicatorColor(TileSpecialType specialType)
    {
        return specialType switch
        {
            TileSpecialType.Player1Spawn => Color.blue,
            TileSpecialType.Player2Spawn => Color.red,
            TileSpecialType.BombsiteA => Color.yellow,
            TileSpecialType.BombsiteB => new Color(1f, 0.5f, 0f, 1f),
            _ => Color.white
        };
    }

    #endregion

    #region MÉTODOS AUXILIARES

    private MapTile GetCurrentTileData(int x, int z)
    {
        Vector2Int pos = new Vector2Int(x, z);
        
        if (editingTiles.ContainsKey(pos))
        {
            return editingTiles[pos];
        }
        
        return currentMapData?.GetTile(x, z);
    }
    
private void ApplyEditingChangesToAsset()
{
    CheckForManualChanges();
    DetectManualDeletions();
    
    // ATUALIZA OS DADOS DOS EXTRAS ANTES DE SALVAR
    SaveChangesToExtras();

    foreach (var kvp in editingTiles)
    {
        Vector2Int pos = kvp.Key;
        MapTile editedTile = kvp.Value;
        currentMapData.SetTile(pos, editedTile);
    }
    
    if (editingMetadata.ContainsKey("mapName"))
        currentMapData.mapName = editingMetadata["mapName"];
    if (editingMetadata.ContainsKey("mapDescription"))
        currentMapData.mapDescription = editingMetadata["mapDescription"];
    if (editingMetadata.ContainsKey("authorName"))
        currentMapData.authorName = editingMetadata["authorName"];
    
    editingTiles.Clear();
    editingMetadata.Clear();
}  
    private string GetEditingMetadata(string key, string defaultValue)
    {
        return editingMetadata.ContainsKey(key) ? editingMetadata[key] : defaultValue;
    }
    
    private MapTile CloneMapTile(MapTile original)
    {
        return new MapTile(original.position)
        {
            tilePrefabName = original.tilePrefabName,
            rotation = original.rotation,
            tileYPosition = original.tileYPosition,
            specialType = original.specialType,
            obstacleType = original.obstacleType,
            obstaclePrefabName = original.obstaclePrefabName,
            obstacleRotation = original.obstacleRotation,
            obstacleYPosition = original.obstacleYPosition
        };
    }

    #endregion

    #region VALIDAÇÃO E ESTATÍSTICAS

    void ValidateCurrentMap()
    {
        if (currentMapData == null) return;
        
        List<string> errors = new List<string>();
        
        int p1Spawns = GetCurrentPlayer1SpawnCount();
        int p2Spawns = GetCurrentPlayer2SpawnCount();
        int bombsiteA = GetCurrentBombsiteACount();
        int bombsiteB = GetCurrentBombsiteBCount();
        
        if (p1Spawns < 5) errors.Add($"Player 1 needs at least 5 spawn points (found {p1Spawns})");
        if (p2Spawns < 5) errors.Add($"Player 2 needs at least 5 spawn points (found {p2Spawns})");
        if (bombsiteA == 0) errors.Add("Bombsite A must have at least 1 tile");
        if (bombsiteB == 0) errors.Add("Bombsite B must have at least 1 tile");
        
        if (errors.Count == 0)
        {
            EditorUtility.DisplayDialog("Validation Success", "The map is valid!", "OK");
        }
        else
        {
            string errorMessage = "Validation failed:\n\n" + string.Join("\n", errors);
            EditorUtility.DisplayDialog("Validation Failed", errorMessage, "OK");
        }
    }
    
    private int GetCurrentPlayer1SpawnCount()
    {
        int count = 0;
        if (currentMapData == null) return count;
        
        foreach (var tile in currentMapData.tiles)
        {
            Vector2Int pos = tile.position;
            TileSpecialType specialType = editingTiles.ContainsKey(pos) ? 
                editingTiles[pos].specialType : tile.specialType;
                
            if (specialType == TileSpecialType.Player1Spawn) count++;
        }
        
        return count;
    }
    
    private int GetCurrentPlayer2SpawnCount()
    {
        int count = 0;
        if (currentMapData == null) return count;
        
        foreach (var tile in currentMapData.tiles)
        {
            Vector2Int pos = tile.position;
            TileSpecialType specialType = editingTiles.ContainsKey(pos) ? 
                editingTiles[pos].specialType : tile.specialType;
                
            if (specialType == TileSpecialType.Player2Spawn) count++;
        }
        
        return count;
    }
    
    private int GetCurrentBombsiteACount()
    {
        int count = 0;
        if (currentMapData == null) return count;
        
        foreach (var tile in currentMapData.tiles)
        {
            Vector2Int pos = tile.position;
            TileSpecialType specialType = editingTiles.ContainsKey(pos) ? 
                editingTiles[pos].specialType : tile.specialType;
                
            if (specialType == TileSpecialType.BombsiteA) count++;
        }
        
        return count;
    }
    
    private int GetCurrentBombsiteBCount()
    {
        int count = 0;
        if (currentMapData == null) return count;
        
        foreach (var tile in currentMapData.tiles)
        {
            Vector2Int pos = tile.position;
            TileSpecialType specialType = editingTiles.ContainsKey(pos) ? 
                editingTiles[pos].specialType : tile.specialType;
                
            if (specialType == TileSpecialType.BombsiteB) count++;
        }
        
        return count;
    }

    #endregion

    #region FERRAMENTAS DE DEBUG

    private void LogCurrentMapStatistics()
    {
        if (currentMapData == null) return;
        
        Debug.Log("=== CURRENT MAP STATISTICS ===");
        Debug.Log($"Map Name: {GetEditingMetadata("mapName", currentMapData.mapName)}");
        Debug.Log($"Grid Size: {currentMapData.gridWidth}x{currentMapData.gridHeight}");
        Debug.Log($"Total Tiles: {currentMapData.tiles.Count}");
        Debug.Log($"Player 1 Spawns: {GetCurrentPlayer1SpawnCount()}");
        Debug.Log($"Player 2 Spawns: {GetCurrentPlayer2SpawnCount()}");
        Debug.Log($"Bombsite A: {GetCurrentBombsiteACount()} tiles");
        Debug.Log($"Bombsite B: {GetCurrentBombsiteBCount()} tiles");
        Debug.Log($"Pending Changes: {editingTiles.Count}");
        Debug.Log($"Has Unsaved Changes: {hasUnsavedChanges}");
    }

    #endregion

    #region ATALHOS DE TECLADO

    void ProcessKeyboardShortcuts()
    {
        if (Event.current != null && Event.current.type == EventType.KeyDown)
        {
            switch (Event.current.keyCode)
            {
                case KeyCode.S:
                    if (Event.current.control && hasUnsavedChanges && currentMapData != null)
                    {
                        SaveCurrentMap();
                        Event.current.Use();
                    }
                    break;
                case KeyCode.Z:
                    if (Event.current.control && !Event.current.shift)
                    {
                        PerformUndo();
                        Event.current.Use();
                    }
                    break;
                case KeyCode.Y:
                    if (Event.current.control)
                    {
                        PerformRedo();
                        Event.current.Use();
                    }
                    break;
                case KeyCode.L:
                    if (Event.current.control)
                    {
                        LoadMap();
                        Event.current.Use();
                    }
                    break;
                case KeyCode.N:
                    if (Event.current.control)
                    {
                        CreateNewMap();
                        Event.current.Use();
                    }
                    break;
                case KeyCode.Alpha1:
                    SetEditMode(EditMode.Select);
                    Event.current.Use();
                    break;
                case KeyCode.Alpha2:
                    SetEditMode(EditMode.PaintTiles);
                    Event.current.Use();
                    break;
                case KeyCode.Alpha3:
                    SetEditMode(EditMode.PaintObstacles);
                    Event.current.Use();
                    break;
                case KeyCode.Alpha4:
                    SetEditMode(EditMode.PaintHeight);
                    Event.current.Use();
                    break;
                case KeyCode.Alpha5:
                    SetEditMode(EditMode.RotateTiles);
                    Event.current.Use();
                    break;
                case KeyCode.Alpha6:
                    SetEditMode(EditMode.RotateObstacles);
                    Event.current.Use();
                    break;
            }
        }
    }

    #endregion

    #region CLASSE AUXILIAR

    [System.Serializable]
    private class MapEditorState
    {
        public Dictionary<Vector2Int, MapTile> tiles;
    }

    #endregion

    void OnDestroy()
    {
       // if (hasUnsavedChanges)
       // {
        //    ConfirmAndSaveChanges();
       // }
    }
}
#endif