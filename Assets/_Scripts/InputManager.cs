// _Scripts/InputManager.cs - VERSÃO FINAL CORRIGIDA
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class InputManager : MonoBehaviour
{
    #region Serialized Fields
    [Header("Click Filtering")]
    [SerializeField] private LayerMask ignoredLayers = 0;
    [SerializeField] private string[] ignoredTags = { "UI", "FloatingUI" };
    #endregion

    #region Private Fields
    private Camera mainCamera;
    private bool cameraControlsEnabled = true;
    private bool isEnabled = true;
    #endregion

    #region Events
    public static event Action<Vector3> OnGroundClicked;
    public static event Action<GameObject> OnPieceClicked;
    public static event Action<Tile> OnTileClicked;
    public static event Action OnEmptySpaceClicked;
     private bool isProcessingClick = false;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            Debug.LogError("InputManager: Main Camera not found!");
        }
        
        Debug.Log("<color=cyan>[InputManager]</color> Simple click filtering system initialized");
    }
    
    void Update()
    {
        if (!cameraControlsEnabled)
        {
            return;
        }

        ProcessInput(); 
    }
    
    void OnDestroy()
    {
        OnGroundClicked = null;
        OnPieceClicked = null;
        OnTileClicked = null;
        OnEmptySpaceClicked = null;
    }
    #endregion

    #region Input Processing
     private void ProcessInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ProcessMouseClick();
        }
    }
    private IEnumerator ResetClickLockCoroutine()
    {
        yield return new WaitForEndOfFrame();
        isProcessingClick = false;
    }
    
    private void ProcessMouseClick()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        RaycastHit[] allHits = Physics.RaycastAll(ray, 1000f);
        
        if (allHits.Length == 0)
        {
            OnEmptySpaceClicked?.Invoke();
            return;
        }
        
        System.Array.Sort(allHits, (hit1, hit2) => hit1.distance.CompareTo(hit2.distance));
        
        ProcessFilteredClick(allHits);
    }
    
    private void ProcessFilteredClick(RaycastHit[] allHits)
    {
        foreach (RaycastHit hit in allHits)
        {
            GameObject hitObject = hit.collider.gameObject;
            
            if (ShouldIgnoreObject(hitObject))
            {
                continue;
            }

            isProcessingClick = true;
            StartCoroutine(ResetClickLockCoroutine());
            
            HighlightLink link = hit.collider.GetComponentInParent<HighlightLink>();
            if (link != null && link.linkedTile != null)
            {
                ServiceLocator.Audio.PlayButtonClickSound();
                OnTileClicked?.Invoke(link.linkedTile);
                return;
            }

            if (IsPiece(hitObject))
            {
                ServiceLocator.Audio.PlayButtonClickSound();
                // A verificação de IsPiece aqui está buscando pelo NOME do GameObject.
                // Precisamos garantir que o objeto raiz da peça (com o PieceProperties) seja passado.
                OnPieceClicked?.Invoke(hit.collider.GetComponentInParent<PieceProperties>().gameObject);
                return;
            }

            // --- INÍCIO DA CORREÇÃO DEFINITIVA ---
            // Adicionamos uma nova verificação para o SpawnedEffect (o muro)
            SpawnedEffect effect = hit.collider.GetComponentInParent<SpawnedEffect>();
            if (effect != null)
            {
                // Se o clique atingiu um efeito, nós não queremos o efeito em si, mas sim o TILE embaixo dele.
                Tile tileUnderEffect = ServiceLocator.Grid.GetTileUnderPiece(effect.gameObject);
                if (tileUnderEffect != null)
                {
                    // Encontramos o tile. Disparamos o evento OnTileClicked como se tivéssemos clicado diretamente no tile.
                    // Isso alimenta a lógica do GameManager que já sabemos que funciona.
                    ServiceLocator.Audio.PlayButtonClickSound();
                    OnTileClicked?.Invoke(tileUnderEffect);
                    return; // Ação concluída, encerra a função.
                }
            }
            // --- FIM DA CORREÇÃO DEFINITIVA ---
            
            Tile tile = hit.collider.GetComponentInParent<Tile>();
            if (tile != null)
            {
                ServiceLocator.Audio.PlayButtonClickSound();
                OnTileClicked?.Invoke(tile);
                return;
            }
            
            OnGroundClicked?.Invoke(hit.point);
            return;
        }
        
        OnEmptySpaceClicked?.Invoke();
    }
    #endregion

    #region Filtering Logic
    private bool ShouldIgnoreObject(GameObject obj)
    {
        if (obj == null) return true;
        
        if (IsInIgnoredLayer(obj.layer))
        {
            return true;
        }
        
        if (HasIgnoredTag(obj.tag))
        {
            return true;
        }
        
        return false;
    }
    
    private bool IsPiece(GameObject obj)
    {
        // Alterado para uma verificação mais segura baseada em componentes em vez de nome.
        return obj.GetComponentInParent<PieceProperties>() != null;
    }
    
    private bool IsInIgnoredLayer(int layer)
    {
        return (ignoredLayers.value & (1 << layer)) != 0;
    }
    
    private bool HasIgnoredTag(string tag)
    {
        foreach (string ignoredTag in ignoredTags)
        {
            if (tag == ignoredTag) return true;
        }
        return false;
    }
    #endregion

    #region Public Methods
    public void SetInputEnabled(bool enabled)
    {
        isEnabled = enabled;
    }

    public void DisableCameraControls()
    {
        cameraControlsEnabled = false;
        Debug.Log("<color=yellow>[InputManager]</color> Controles de câmera desativados.");
    }
    #endregion
}