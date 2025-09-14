using UnityEngine;

public class TileHighlightSystem : MonoBehaviour
{
    [Header("Highlight Objects")]
    [SerializeField] private GameObject movementHighlight;
    [SerializeField] private GameObject attackHighlight;
    [SerializeField] private GameObject supportHighlight;
    
    [Header("Highlight Materials (Loaded from ServiceLocator)")]
    public Material movementHighlightMaterial;
    public Material attackHighlightMaterial;

    
    public Material supportHighlightMaterial;
    
    private bool isInitialized = false;
    
    void Start()
    {
        if (!isInitialized)
        {
            InitializeHighlightSystem();
        }
    }
    
    public void InitializeHighlightSystem()
    {
        if (isInitialized) return;
        
        LoadHighlightMaterials();
        CreateHighlightObjects();
        HideAllHighlights();
        
        isInitialized = true;
    }
    
     private void LoadHighlightMaterials()
    {
        movementHighlightMaterial = ServiceLocator.MovementHighlightMaterial;
        attackHighlightMaterial = ServiceLocator.AttackHighlightMaterial;
        supportHighlightMaterial = ServiceLocator.SupportHighlightMaterial; // <-- LINHA ADICIONADA

        if (movementHighlightMaterial == null)
            Debug.LogError($"'movementHighlightMaterial' não foi atribuído no Inspector do ServiceLocator!", this.gameObject);
        if (attackHighlightMaterial == null)
            Debug.LogError($"'attackHighlightMaterial' não foi atribuído no Inspector do ServiceLocator!", this.gameObject);
        if (supportHighlightMaterial == null)
            Debug.LogError($"'supportHighlightMaterial' não foi atribuído no Inspector do ServiceLocator!", this.gameObject);
    }
    
    private void CreateHighlightObjects()
    {
        movementHighlight = CreateHighlightQuad("MovementHighlight", movementHighlightMaterial);
        attackHighlight = CreateHighlightQuad("AttackHighlight", attackHighlightMaterial);
        supportHighlight = CreateHighlightQuad("SupportHighlight", supportHighlightMaterial); // <-- LINHA ADICIONADA
    }

    private GameObject CreateHighlightQuad(string name, Material mat)
    {
        if (mat == null) return null;

        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
        highlight.name = name;
        highlight.transform.SetParent(transform, false);

        // --- INÍCIO DA CORREÇÃO ---
        // 1. Remove o MeshCollider padrão que vem com o Quad.
        Destroy(highlight.GetComponent<MeshCollider>());

        // 2. Adiciona um BoxCollider, que é mais simples e compatível com triggers.
        BoxCollider boxCollider = highlight.AddComponent<BoxCollider>();

        // 3. Configura o BoxCollider.
        boxCollider.isTrigger = true; // Agora isso funciona sem erros.
        boxCollider.size = new Vector3(1, 1, 0.01f); // Ajusta o tamanho para ser plano.
                                                     // --- FIM DA CORREÇÃO ---

        highlight.GetComponent<Renderer>().material = mat;

        float highlightOffset = 0.01f;
        if (GameConfig.Instance != null)
        {
            highlightOffset = GameConfig.Instance.highlightYOffset;
        }



        float yPos = GetTileTopY();
        highlight.transform.localPosition = new Vector3(0, yPos + highlightOffset, 0);
        highlight.transform.localRotation = Quaternion.Euler(90, 0, 0);
        highlight.transform.localScale = Vector3.one * 0.9f;

        highlight.SetActive(false);

        return highlight;
    }

    
    private float GetTileTopY()
    {
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.size.y;
        }
        return 0.2f; 
    }
    
    // ... O resto do script continua igual (Show/Hide Highlights, etc.)
    
    public void ShowMovementHighlight()
    {
        if (!isInitialized) InitializeHighlightSystem();
        
        HideAllHighlights();
        if (movementHighlight != null)
        {
            UpdateHighlightPosition();
            movementHighlight.SetActive(true);
        }
    }
    
    public void ShowAttackHighlight()
    {
        if (!isInitialized) InitializeHighlightSystem();
        
        HideAllHighlights();
        if (attackHighlight != null)
        {
            UpdateHighlightPosition();
            attackHighlight.SetActive(true);
        }
    }

    public void ShowSupportHighlight()
    {
        if (!isInitialized) InitializeHighlightSystem();
        
        HideAllHighlights();
        if (supportHighlight != null)
        {
            UpdateHighlightPosition();
            supportHighlight.SetActive(true);
        }
    }

    
    public void HideAllHighlights()
    {
        if (movementHighlight != null)
            movementHighlight.SetActive(false);
        
        if (attackHighlight != null)
            attackHighlight.SetActive(false);

        if (supportHighlight != null) // <-- LINHA ADICIONADA
            supportHighlight.SetActive(false);
    }
    
    public void UpdateHighlightPosition()
    {
        if (!isInitialized) return;
        
        float highlightOffset = 0.01f;
        if (GameConfig.Instance != null)
        {
            highlightOffset = GameConfig.Instance.highlightYOffset;
        }

        float yPos = GetTileTopY();
        Vector3 highlightPos = new Vector3(0, yPos + highlightOffset, 0);
        
        if (movementHighlight != null)
            movementHighlight.transform.localPosition = highlightPos;
        
        if (attackHighlight != null)
            attackHighlight.transform.localPosition = highlightPos;
    }
}