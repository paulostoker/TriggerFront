// _Scripts/Card3D.cs - Versão Corrigida
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

// Adicionamos IPointerEnterHandler e IPointerExitHandler para detectar o mouse
public class Card3D : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    // --- ALTERAÇÃO ---
    // Adicionamos esta referência pública para a imagem do retrato da UI.
    // Lembre-se de arrastar o objeto Image do seu prefab para este campo no Inspector.
    [Header("UI References")]
    public Image portraitImage;
    // --- FIM DA ALTERAÇÃO ---

    // Materiais privados recebidos do CardManager
    private Material actionMaterial;
    private Material utilityMaterial;
    private Material auraMaterial;
    private Material skillMaterial;
    private Material strategyMaterial;
    private Material freelancerMaterial;
    private Material borderMaterial;
    private Material backMaterial;

    // Estado da carta
    private CardData cardData;
    private CardManager cardManager;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private bool isSelected = false;
    
    // Configurações de seleção
    private float selectedYOffset = 15f;
    private float selectedZOffset = 100f;
    private float selectedScale = 1.2f;
    private float selectionAnimationDuration = 0.3f;
    
    // Controle de animação
    private Coroutine activeSelectionAnimation;

    // Componentes
    private MeshRenderer meshRenderer;
    private Card3DLayout cardLayout;
    private RectTransform rectTransform;
    private Hand3DContainer handContainer;


    private FreelancersUIContainer freelancerContainer;

    void Awake()
    {
        // Busca MeshRenderer no filho "CardMesh" ou em qualquer filho
        Transform cardMeshTransform = transform.Find("CardMesh");
        if (cardMeshTransform != null)
        {
            meshRenderer = cardMeshTransform.GetComponent<MeshRenderer>();
        }
        
        if (meshRenderer == null)
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        cardLayout = GetComponentInChildren<Card3DLayout>(true);
        
        // Ao acordar, a carta descobre a qual container de operador ela pertence
        freelancerContainer = GetComponentInParent<FreelancersUIContainer>();
    }

    // Chamado quando o cursor do mouse entra na área da carta
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Avisa o container que esta carta está sob o mouse
        if (freelancerContainer != null)
        {
            freelancerContainer.OnCardHovered(gameObject);
        }
    }

    // Chamado quando o cursor do mouse sai da área da carta
    public void OnPointerExit(PointerEventData eventData)
    {
        // Avisa o container que o mouse saiu
        if (freelancerContainer != null)
        {
            freelancerContainer.OnCardHoverExited(gameObject);
        }
    }

    // Método para receber materiais do CardManager
    public void SetMaterials(Material action, Material utility, Material aura, 
                            Material skill, Material strategy, Material freelancerMat,
                            Material border, Material back)
    {
        actionMaterial = action;
        utilityMaterial = utility;
        auraMaterial = aura;
        skillMaterial = skill;
        strategyMaterial = strategy;
        freelancerMaterial = freelancerMat;
        borderMaterial = border;
        backMaterial = back;
    }

    public void Setup(CardData data, CardManager manager)
    {
        if (cardLayout == null)
        {
            Debug.LogWarning("[Card3D] cardLayout era nulo no Setup. Tentando encontrar novamente...");
            cardLayout = GetComponentInChildren<Card3DLayout>(true);
            if (cardLayout == null)
            {
                Debug.LogError("[Card3D] FALHA CRÍTICA: Não foi possível encontrar o Card3DLayout. Verifique a estrutura do prefab.");
                return;
            }
        }

        Debug.Log($"[Card3D] Setup called with CardData: {data?.cardName}");
        Debug.Log($"[Card3D] CardData portrait: {(data?.portrait != null ? data.portrait.name : "NULL")}");
        
        cardData = data;
        cardManager = manager;
        
        handContainer = GetComponentInParent<Hand3DContainer>();
        
        Debug.Log("[Card3D] Calling cardLayout.SetupLayout...");
        cardLayout.SetupLayout(data);
        
        SetupMaterials(data.cardType);
    }

    private void SetupMaterials(CardType cardType)
    {
        if (meshRenderer == null) return;

        Material frontMaterial = cardType switch
        {
            CardType.Action => actionMaterial,
            CardType.Utility => utilityMaterial,
            CardType.Aura => auraMaterial,
            CardType.Skill => skillMaterial,
            CardType.Strategy => strategyMaterial,
            CardType.Freelancer => freelancerMaterial,
            _ => actionMaterial
        };

        if (frontMaterial == null) return;

        Material[] materials = new Material[meshRenderer.materials.Length];
        for (int i = 0; i < meshRenderer.materials.Length; i++)
        {
            materials[i] = meshRenderer.materials[i];
        }

        if (materials.Length >= 1 && backMaterial != null) 
        {
            materials[0] = backMaterial;
        }
        
        if (materials.Length >= 2) 
        {
            materials[1] = frontMaterial;
        }
        
        if (materials.Length >= 3 && borderMaterial != null) 
        {
            materials[2] = borderMaterial;
        }
        
        meshRenderer.materials = materials;
    }

    #region UI INTERACTION

         public void OnPointerClick(PointerEventData eventData)
    {
        // Se estivermos no modo de busca, o clique é gerenciado pelo CardSearchManager.
        if (ServiceLocator.Search != null && ServiceLocator.Search.IsInSearchMode)
        {
            ServiceLocator.Search.OnCardClicked(gameObject);
            return; // Impede que a lógica normal de clique continue
        }

        // Lógica original de clique (para mão, etc.)
        if (cardManager != null && cardData != null)
        {
            StartCoroutine(ProcessClickDelayed());
        }
    }

    private IEnumerator ProcessClickDelayed()
    {
        yield return new WaitForEndOfFrame();
        cardManager.OnCard3DClicked(cardData, gameObject);
    }

    #endregion

    #region SELECTION VISUAL

    public void SetSelected(bool selected)
    {
        if (isSelected == selected) return;
        
        isSelected = selected;
        
        if (activeSelectionAnimation != null)
        {
            StopCoroutine(activeSelectionAnimation);
        }
        
        activeSelectionAnimation = StartCoroutine(AnimateSelection(selected));
    }
    
    private IEnumerator AnimateSelection(bool selected)
    {
        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;
        Vector3 startScale = transform.localScale;
        
        Vector3 targetPos, targetScale;
        Quaternion targetRot;
        
        if (selected)
        {
            targetPos = originalPosition;
            targetPos.y += selectedYOffset;
            targetPos.z += selectedZOffset;
            
            targetRot = Quaternion.Euler(0, 0, 0);
            
            targetScale = new Vector3(
                originalScale.x * selectedScale,
                originalScale.y * selectedScale,
                originalScale.z * selectedScale
            );
        }
        else
        {
            targetPos = originalPosition;
            targetRot = originalRotation;
            targetScale = originalScale;
        }
        
        float elapsedTime = 0f;
        
        while (elapsedTime < selectionAnimationDuration)
        {
            float progress = elapsedTime / selectionAnimationDuration;
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            
            transform.localPosition = Vector3.Lerp(startPos, targetPos, easedProgress);
            transform.localRotation = Quaternion.Lerp(startRot, targetRot, easedProgress);
            transform.localScale = Vector3.Lerp(startScale, targetScale, easedProgress);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        transform.localPosition = targetPos;
        transform.localRotation = targetRot;
        transform.localScale = targetScale;
        
        activeSelectionAnimation = null;
    }
    


    #endregion

    #region UTILITY

    public void SetOriginalPosition(Vector2 position)
    {
        originalPosition = new Vector3(position.x, position.y, 0);
        originalRotation = transform.localRotation;
        originalScale = transform.localScale;

        if (!isSelected)
        {
            transform.localPosition = originalPosition;
        }
    }
    
    public void SetSelectionConfig(float yOffset, float zOffset, float scale, float animDuration)
    {
        selectedYOffset = yOffset;
        selectedZOffset = zOffset;
        selectedScale = scale;
        selectionAnimationDuration = animDuration;
    }

    public CardData GetCardData() => cardData;
    public bool IsSelected() => isSelected;
    public bool IsDragging() => false;

    #endregion

    #region CLEANUP

    void OnDestroy()
    {
        if (activeSelectionAnimation != null)
        {
            StopCoroutine(activeSelectionAnimation);
        }
    }

    #endregion
}