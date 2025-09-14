// _Scripts/CardSearchManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class CardSearchManager : MonoBehaviour
{
    public static CardSearchManager Instance { get; private set; }

    [Header("UI References (3D Canvas)")]
    public GameObject searchPanel;
    public Transform cardGridContainer;
    public Transform previewContainer;

    [Header("UI References (2D Canvas)")]
    public TextMeshProUGUI instructionText;
    public Button endSelectionButton;
    public Button cancelButton; // Manter a referência para desativá-lo

    [Header("Prefabs")]
    public GameObject card3DPrefab;

    [Header("Layout Settings")]
    public float gridMaxWidth = 1800f;
    public float gridMaxHeight = 900f;
    public int maxColumns = 12;
    public float selectedScaleMultiplier = 1.5f;
    [Range(0.5f, 1f)]
    public float gridSpacingMultiplier = 0.9f;
    [Tooltip("Quanto a carta selecionada se move para frente (eixo Z negativo).")]
    public float selectedZOffset = -50f;

    private CardManager cardManagerReference;

    // --- Estado Interno ---
    private List<CardData> sourceDeck = new List<CardData>();
    private List<GameObject> selectedCardsObjects = new List<GameObject>();
    private Dictionary<GameObject, CardData> cardGameObjects = new Dictionary<GameObject, CardData>();
    private GameObject currentPreviewCard;

    // --- Critérios da Busca ---
    private int requiredAmount;
    private CardType? requiredTypeFilter;
    private Action<List<CardData>> onCompleteCallback;
    private bool isSearchFromDiscard;

    public bool IsInSearchMode { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (searchPanel != null)
        {
            searchPanel.SetActive(false);
        }
        cardManagerReference = ServiceLocator.Cards;
    }

    void OnDestroy()
    {
        CloseSearch(false); 
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // _Scripts/CardSearchManager.cs

    public void StartSearch(List<CardData> cardsToDisplay, int amountToSelect, CardType? typeFilter, bool fromDiscard, Action<List<CardData>> onComplete)
    {
        if (cardManagerReference == null)
        {
            cardManagerReference = ServiceLocator.Cards;
            if (cardManagerReference == null)
            {
                Debug.LogError("[CardSearchManager] Referência para o CardManager não encontrada! Abortando a busca.");
                return;
            }
        }
        
        if(instructionText != null) instructionText.gameObject.SetActive(true);
        if(endSelectionButton != null) endSelectionButton.gameObject.SetActive(true);
        if(cancelButton != null) cancelButton.gameObject.SetActive(false);

        ServiceLocator.UI.SetMainUIVisibility(false);
        ServiceLocator.Cards.SetAllHandsVisibility(false);
        ServiceLocator.Cards.SetFreelancerHandVisibility(false, false);

        sourceDeck = new List<CardData>(cardsToDisplay);
        requiredAmount = amountToSelect;
        requiredTypeFilter = typeFilter;
        onCompleteCallback = onComplete;
        isSearchFromDiscard = fromDiscard;
        
        selectedCardsObjects.Clear();
        ClearGrid();

        UpdateInstructionText();
        
        // --- CORREÇÃO AQUI: O botão agora começa e permanece habilitado ---
        endSelectionButton.interactable = true;
        
        endSelectionButton.onClick.RemoveAllListeners();
        
        endSelectionButton.onClick.AddListener(ConfirmSelection);

        StartCoroutine(PopulateGrid());
        searchPanel.SetActive(true);
        IsInSearchMode = true;
    }

    private IEnumerator PopulateGrid()
    {
        if (sourceDeck.Count == 0) yield break;

        float cardAspectRatio = 1.4f;
        int numColumns = (sourceDeck.Count < maxColumns && sourceDeck.Count > 0) ? sourceDeck.Count : maxColumns;
        if (numColumns == 0) numColumns = 1;

        float cardWidth = gridMaxWidth / numColumns;
        float cardHeight = cardWidth * cardAspectRatio;
        int numRows = Mathf.CeilToInt((float)sourceDeck.Count / numColumns);
        
        float totalGridWidth = (numColumns - 1) * (cardWidth * gridSpacingMultiplier);
        float totalGridHeight = (numRows - 1) * (cardHeight * gridSpacingMultiplier);
        Vector3 startPosition = new Vector3(-totalGridWidth / 2f, totalGridHeight / 2f, 0);

        for (int i = 0; i < sourceDeck.Count; i++)
        {
            CardData cardData = sourceDeck[i];
            int row = i / numColumns;
            int col = i % numColumns;
            
            Vector3 position = startPosition + new Vector3(col * (cardWidth * gridSpacingMultiplier), -row * (cardHeight * gridSpacingMultiplier), 0f);
            
            GameObject cardInstance = Instantiate(card3DPrefab, cardGridContainer);
            
            // --- CORREÇÃO BUG DA CARTA EM BRANCO: Espera um frame para garantir que tudo foi inicializado ---
            yield return null;

            cardInstance.transform.localPosition = position;
            cardInstance.transform.localRotation = Quaternion.identity;
            cardInstance.transform.localScale = Vector3.one * cardWidth;

            Card3D card3D = cardInstance.GetComponent<Card3D>();
            if (card3D != null)
            {
                card3D.SetMaterials(
                    cardManagerReference.actionMaterial, cardManagerReference.utilityMaterial, cardManagerReference.auraMaterial, 
                    cardManagerReference.skillMaterial, cardManagerReference.strategyMaterial, cardManagerReference.freelancerMaterial, 
                    cardManagerReference.borderMaterial, cardManagerReference.backMaterial
                );
                card3D.Setup(cardData, null);
            }
            
            cardGameObjects.Add(cardInstance, cardData);
        }
    }
    
    public void OnCardClicked(GameObject cardObject)
    {
        if (!cardGameObjects.ContainsKey(cardObject)) return;

        CardData clickedCardData = cardGameObjects[cardObject];

        // --- LÓGICA DE FILTRO ATUALIZADA ---
        bool isCardValidForSelection = false;
        if (!requiredTypeFilter.HasValue) // Se não houver filtro, qualquer carta é válida
        {
            isCardValidForSelection = true;
        }
        else if (requiredTypeFilter.Value == CardType.Energy) // Se o filtro for 'Energy'
        {
            // Usa o método auxiliar IsEnergyCard() para verificar se a carta pertence ao grupo
            if (clickedCardData.IsEnergyCard()) 
            {
                isCardValidForSelection = true;
            }
        }
        else // Para todos os outros filtros específicos (Action, Skill, etc.)
        {
            // Compara o tipo diretamente
            if (clickedCardData.cardType == requiredTypeFilter.Value)
            {
                isCardValidForSelection = true;
            }
        }

        if (!isCardValidForSelection)
        {
            ServiceLocator.Audio.PlayConditionFailSound();
            return;
        }
        // --- FIM DA LÓGICA DE FILTRO ---

        Vector3 currentPos = cardObject.transform.localPosition;

        if (selectedCardsObjects.Contains(cardObject))
        {
            // Desselecionar
            selectedCardsObjects.Remove(cardObject);
            cardObject.transform.localScale /= selectedScaleMultiplier;
            cardObject.transform.localPosition = new Vector3(currentPos.x, currentPos.y, 0f);
            
            if (currentPreviewCard != null && currentPreviewCard.GetComponent<Card3D>().GetCardData() == clickedCardData)
            {
                Destroy(currentPreviewCard);
                currentPreviewCard = null;

                if (selectedCardsObjects.Count > 0)
                {
                    GameObject lastSelectedObject = selectedCardsObjects.Last();
                    UpdatePreview(cardGameObjects[lastSelectedObject]);
                }
            }
        }
        else
        {
            // Selecionar
            if (selectedCardsObjects.Count < requiredAmount)
            {
                selectedCardsObjects.Add(cardObject);
                cardObject.transform.localScale *= selectedScaleMultiplier;
                cardObject.transform.localPosition = new Vector3(currentPos.x, currentPos.y, selectedZOffset);
                UpdatePreview(clickedCardData);
            }
            else
            {
                ServiceLocator.Audio.PlayConditionFailSound();
            }
        }

        UpdateInstructionText();
        endSelectionButton.interactable = (selectedCardsObjects.Count == requiredAmount);
    }

    private void UpdatePreview(CardData cardData)
    {
        if (currentPreviewCard != null) Destroy(currentPreviewCard);

        currentPreviewCard = Instantiate(card3DPrefab, previewContainer);
        currentPreviewCard.transform.localPosition = Vector3.zero;
        currentPreviewCard.transform.localRotation = Quaternion.identity;
        currentPreviewCard.transform.localScale = Vector3.one * 400f;

        Card3D card3D = currentPreviewCard.GetComponent<Card3D>();
        if (card3D != null)
        {
            card3D.SetMaterials(
                cardManagerReference.actionMaterial, cardManagerReference.utilityMaterial, cardManagerReference.auraMaterial, 
                cardManagerReference.skillMaterial, cardManagerReference.strategyMaterial, cardManagerReference.freelancerMaterial, 
                cardManagerReference.borderMaterial, cardManagerReference.backMaterial
            );
            card3D.Setup(cardData, null);
        }
    }
    
     private void ConfirmSelection()
    {
        // Converte a lista de GameObjects selecionados de volta para uma lista de CardData
        List<CardData> finalSelectedCards = selectedCardsObjects.Select(go => cardGameObjects[go]).ToList();
        
        // Simplesmente invoca o callback com as cartas selecionadas
        onCompleteCallback?.Invoke(finalSelectedCards);
        CloseSearch();
    }

    private void CancelSearch()
    {
        // Simplesmente invoca o callback com uma lista vazia para sinalizar o cancelamento
        onCompleteCallback?.Invoke(new List<CardData>());
        CloseSearch();
    }
    
    private void CloseSearch(bool restoreGameUI = true)
    {
        IsInSearchMode = false;
        if(searchPanel != null) searchPanel.SetActive(false);
        
        if(instructionText != null) instructionText.gameObject.SetActive(false);
        if(endSelectionButton != null) endSelectionButton.gameObject.SetActive(false);
        if(cancelButton != null) cancelButton.gameObject.SetActive(false);
        
        ClearGrid();
        
        if (endSelectionButton != null) endSelectionButton.onClick.RemoveAllListeners();
        if (cancelButton != null) cancelButton.onClick.RemoveAllListeners();
        
        onCompleteCallback = null;

        if (restoreGameUI)
        {
            ServiceLocator.UI.SetMainUIVisibility(true);
            ServiceLocator.UI.ShowPreparationUI(true); 
            // --- CORREÇÃO BUG DA UI DOS FREELANCERS ---
            ServiceLocator.Cards.SetFreelancerHandVisibilityForCurrentPlayer(ServiceLocator.Game.IsPlayer1Turn());
        }
    }

    private void ClearGrid()
    {
        foreach (var cardObject in cardGameObjects.Keys)
        {
            if(cardObject != null) Destroy(cardObject);
        }
        cardGameObjects.Clear();

        if (currentPreviewCard != null)
        {
            Destroy(currentPreviewCard);
            currentPreviewCard = null;
        }
    }
    
    private void UpdateInstructionText()
    {
        string typeString = requiredTypeFilter.HasValue ? requiredTypeFilter.Value.ToString() : "any";
        instructionText.text = $"Select ({selectedCardsObjects.Count}/{requiredAmount}) {typeString} cards";
    }
}