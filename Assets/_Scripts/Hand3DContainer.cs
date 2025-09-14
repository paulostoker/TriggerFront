// _Scripts/Hand3DContainer.cs - Versão Completa com Filtro de Cartas
using UnityEngine;
using System.Collections.Generic;

public class Hand3DContainer : MonoBehaviour
{
    #region Fields & Settings
    [Header("Layout Settings")]
    public bool isPlayer1Hand = true;
    public bool useSharedPosition = true;

    // Configurações de layout (vem do CardManager)
    private float cardSize = 180f;
    private float baseSpacement = 95f;
    private float cardsHeight = -150f;
    private float spaceWidth = 760f;
    private float addRotation = 2f;
    private float selectedYOffset = 15f;
    private float selectedZOffset = 100f;
    private float selectedScale = 1.2f;
    private float selectionAnimationDuration = 0.3f;

    // Estado do container
    private List<GameObject> cards = new List<GameObject>();
    
    #endregion

    #region Initialization & Configuration
    void Start()
    {
        Debug.Log($"<color=green>[Hand3D]</color> {(isPlayer1Hand ? "Player 1" : "Player 2")} hand initialized");
    }

    public void SetLayoutConfig(float size, float spacement, float height, float maxWidth, float rotation, float selYOffset, float selZOffset, float selScale, float animDuration)
    {
        cardSize = size;
        baseSpacement = spacement;
        cardsHeight = height;
        spaceWidth = maxWidth;
        addRotation = rotation;
        selectedYOffset = selYOffset;
        selectedZOffset = selZOffset;
        selectedScale = selScale;
        selectionAnimationDuration = animDuration;
        
        if (cards.Count > 0) RepositionCards();
    }
    #endregion

    #region Card Management
    public void AddCard(GameObject card)
    {
        if (card == null || cards.Contains(card)) return;

        cards.Add(card);
        card.transform.SetParent(transform, false);
        
        RepositionCards();
    }

    public void RemoveCard(GameObject card)
    {
        if (card == null || !cards.Contains(card)) return;

        cards.Remove(card);
        
        if (card.transform.parent == transform)
        {
            card.transform.SetParent(null);
        }
        
        RepositionCards();
    }

    public void ClearAllCards()
    {
        foreach (var card in cards)
        {
            if (card != null) Destroy(card);
        }
        cards.Clear();
    }
    #endregion

    #region Positioning & Filtering
    public void RepositionCards()
    {
        // Pega todas as cartas ativas no container para reposicionar
        List<GameObject> visibleCards = new List<GameObject>();
        foreach (var card in cards)
        {
            if (card != null && card.activeSelf)
            {
                visibleCards.Add(card);
            }
        }

        if (visibleCards.Count == 0) return;

        // Calcula o espaçamento adaptativo baseado apenas nas cartas visíveis
        float finalSpacing = CalculateAdaptiveSpacing(visibleCards.Count);
        float totalWidth = (visibleCards.Count - 1) * finalSpacing;
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < visibleCards.Count; i++)
        {
            GameObject card = visibleCards[i];
            Vector3 targetLocalPos = new Vector3(startX + (i * finalSpacing), cardsHeight, 0);
            
            float progressiveRotation = addRotation * visibleCards.Count;
            Quaternion targetLocalRot = Quaternion.Euler(0, -progressiveRotation, 0);
            
            Vector3 targetLocalScale = new Vector3(cardSize, cardSize, cardSize);

            // Aplica as transformações
            card.transform.localPosition = targetLocalPos;
            card.transform.localRotation = targetLocalRot;
            card.transform.localScale = targetLocalScale;
            
            // Atualiza o Card3D com as posições e configurações corretas
            Card3D card3D = card.GetComponent<Card3D>();
            if (card3D != null)
            {
                card3D.SetOriginalPosition(new Vector2(targetLocalPos.x, targetLocalPos.y));
                card3D.SetSelectionConfig(selectedYOffset, selectedZOffset, selectedScale, selectionAnimationDuration);
            }
        }
    }

    /// COMEÇO DAS NOVAS ALTERAÇÕES
    public void FilterAndRepositionCards(System.Func<CardData, bool> filterPredicate)
    {
        // Se o filtro for nulo, simplesmente mostra todas as cartas
        if (filterPredicate == null)
        {
            foreach (var cardObject in cards)
            {
                cardObject.SetActive(true);
            }
            RepositionCards();
            return;
        }

        // Se houver um filtro, ativa/desativa as cartas conforme a condição
        foreach (var cardObject in cards)
        {
            if (cardObject == null) continue;
            
            CardData data = cardObject.GetComponent<Card3D>().GetCardData();
            bool shouldBeVisible = (data != null && filterPredicate(data));
            cardObject.SetActive(shouldBeVisible);
        }

        // Manda reposicionar apenas as cartas que ficaram visíveis
        RepositionCards();
    }

    // A lógica de cálculo agora é um método privado que aceita a contagem de cartas
    private float CalculateAdaptiveSpacing(int cardCount)
    {
        if (cardCount <= 1) return baseSpacement;
        
        float requiredWidth = (cardCount - 1) * baseSpacement;
        
        if (requiredWidth <= spaceWidth)
        {
            return baseSpacement;
        }
        else
        {
            float adaptiveSpacing = spaceWidth / (cardCount - 1);
            float minSpacing = baseSpacement * 0.3f;
            return Mathf.Max(adaptiveSpacing, minSpacing);
        }
    }

    // O método antigo, sem parâmetros, agora é um atalho para o novo
    private float CalculateAdaptiveSpacing()
    {
        return CalculateAdaptiveSpacing(cards.Count);
    }
    /// FIM DAS NOVAS ALTERAÇÕES
    #endregion

    #region Utility
    public int GetCardCount() => cards.Count;
    public List<GameObject> GetCards() => new List<GameObject>(cards);
    public bool ContainsCard(GameObject card) => cards.Contains(card);

    public void SetVisibility(bool visible)
    {
        gameObject.SetActive(visible);
        
        // Garante que, ao tornar visível, o filtro seja limpo e todas as cartas apareçam
        if(visible)
        {
            FilterAndRepositionCards(null);
        }
    }
    #endregion

    #region Debug
    void OnDrawGizmos()
    {
        Gizmos.color = isPlayer1Hand ? Color.blue : Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);

        if (cards.Count > 0)
        {
            float finalSpacing = CalculateAdaptiveSpacing(cards.Count);
            float totalWidth = (cards.Count - 1) * finalSpacing;
            float startX = -totalWidth * 0.5f;

            for (int i = 0; i < cards.Count; i++)
            {
                Vector3 cardPos = transform.TransformPoint(new Vector3(startX + (i * finalSpacing), cardsHeight, 0));
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(cardPos, Vector3.one * 0.3f);
            }
        }
    }
    #endregion
}