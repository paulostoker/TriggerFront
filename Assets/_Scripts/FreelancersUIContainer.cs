// _Scripts/FreelancersUIContainer.cs - Versão Final com Lógica de Carrossel Robusta
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class FreelancersUIContainer : MonoBehaviour
{
    public enum DisplayMode { Preparation, Action, Setup, Manager }
    private DisplayMode currentMode = DisplayMode.Action;

    [Header("Container Settings")]
    public bool isPlayer1Hand = true;

    [Header("General Layout")]
    public float cardSize = 180f;
    [Tooltip("Largura visual da carta, para calcular o alinhamento pela borda.")]
    public float cardWidth = 120f;
    [Tooltip("Posição da borda lateral esquerda da coluna de cartas na FASE DE PREPARAÇÃO.")]
    public float cardsXPosition = -400f;

    [Header("Action Mode Settings (Carousel)")]
    [Tooltip("Posição X e Y customizável para a CARTA ATIVA.")]
    public Vector2 activeCardPosition = new Vector2(-400, -200);
    [Tooltip("Rotação em Y customizável para a CARTA ATIVA.")]
    public float activeCardRotationY = 0f;
    public float activeCardScale = 1.3f;
    public float inactiveCardScale = 1.0f;
    public float carouselCardSpacing = 80f;
    [Tooltip("Posição X e Y inicial da pilha de cartas inativas.")]
    public Vector2 carouselStartPosition = new Vector2(-400, -120);
    [Tooltip("Espaçamento adicional em X para cada carta na pilha inativa.")]
    public float carouselSpacingXStep = 0f;
    [Tooltip("Espaçamento adicional em Z (profundidade) para cada carta na pilha inativa.")]
    public float carouselSpacingZStep = -15f;
    [Tooltip("Graus de rotação em Y para TODAS as cartas na pilha inativa.")]
    public float carouselRotationYStep = 5f;

    [Header("Preparation Mode Settings")]
    [Tooltip("Escala base das cartas na fase de preparação.")]
    public float prepCardScale = 1.0f;
    public float prepCardSpacing = 140f;
    [Tooltip("Posição central (Y) da coluna de cartas na fase de preparação.")]
    public Vector2 prepColumnCenterPosition = new Vector2(0, 0);
    public float prepHoverScaleMultiplier = 1.2f;
    public float prepHoverSpacingPush = 40f;
    [Tooltip("Deslocamento horizontal da carta ao passar o mouse para simular o pivô.")]
    public float prepHoverPositionOffset = 20f;

    [Header("Setup Mode Settings")]
    [Tooltip("Escala das cartas na fase de Setup.")]
    public float setupCardScale = 1.2f;
    [Tooltip("Espaçamento horizontal entre as cartas.")]
    public float setupCardSpacing = 150f;
    [Tooltip("Posição central (X, Y) de toda a linha de cartas.")]
    public Vector2 setupStartPosition = new Vector2(0, -100f);
    [Tooltip("Multiplicador de escala da carta ao passar o mouse.")]
    public float setupHoverScaleMultiplier = 1.2f;
    [Tooltip("Distância que as cartas vizinhas são 'empurradas' ao passar o mouse.")]
    public float setupHoverSpacingPush = 40f;

    [Header("Animation Settings")]
    public float transitionDuration = 0.3f;


    [Header("Manager Mode Settings")]
    [Tooltip("Escala das cartas na fase de Manager.")]
    public float managerCardScale = 1.2f;
    [Tooltip("Espaçamento horizontal entre as cartas.")]
    public float managerCardSpacing = 150f;
    [Tooltip("Posição central (X, Y) de toda a linha de cartas.")]
    public Vector2 managerStartPosition = new Vector2(0, -100f);
    [Tooltip("Multiplicador de escala da carta ao passar o mouse.")]
    public float managerHoverScaleMultiplier = 1.2f;
    [Tooltip("Distância que as cartas vizinhas são 'empurradas' ao passar o mouse.")]
    public float managerHoverSpacingPush = 40f;

     [Header("Selection Mode")]
    public float invalidTargetXOffset = -200f;

    private List<FreelancerCardData> freelancerCards = new List<FreelancerCardData>();
    private int activeFreelancerIndex = -1;
    private GameObject hoveredCard = null;

    private class FreelancerCardData
    {
        public GameObject cardObject;
        public FreelancerData freelancerData;
        public int originalIndex;
        public Vector3 targetPosition;
        public Vector3 targetScale;
        public Quaternion targetRotation;
        public Coroutine activeAnimation;
        public bool isSelectableTarget = true;
    }

    public void SetDisplayMode(DisplayMode newMode)
    {
        if (currentMode == newMode && freelancerCards.Count > 0) return;
        currentMode = newMode;
        hoveredCard = null;
        if (currentMode == DisplayMode.Preparation)
        {
            activeFreelancerIndex = -1;
        }
        RepositionCards();
    }

    public void OnCardHovered(GameObject card)
    {
        if (currentMode != DisplayMode.Preparation && currentMode != DisplayMode.Setup) return;
        hoveredCard = card;
        RepositionCards();
    }

    public void OnCardHoverExited(GameObject card)
    {
        if (currentMode != DisplayMode.Preparation && currentMode != DisplayMode.Setup) return;
        if (hoveredCard == card)
        {
            hoveredCard = null;
            RepositionCards();
        }
    }

    public void InitializeFreelancerCards(List<FreelancerData> freelancerDataList, GameObject cardPrefab, CardManager cardManager)
    {
        ClearAllCards();
        for (int i = 0; i < freelancerDataList.Count; i++)
        {
            FreelancerData opData = freelancerDataList[i];
            GameObject cardObject = Instantiate(cardPrefab, transform);
            cardObject.name = $"FreelancerCard_{opData.name}_{(isPlayer1Hand ? "P1" : "P2")}";
            ServiceLocator.Freelancers.RegisterCardInstance(cardObject, opData, isPlayer1Hand);
            Card3D card3D = cardObject.GetComponent<Card3D>();
            if (card3D != null)
            {
                CardData freelancerCardData = CreateCardDataFromFreelancer(opData);
                card3D.SetMaterials(cardManager.actionMaterial, cardManager.utilityMaterial, cardManager.auraMaterial, cardManager.skillMaterial, cardManager.strategyMaterial, cardManager.freelancerMaterial, cardManager.borderMaterial, cardManager.backMaterial);
                card3D.Setup(freelancerCardData, cardManager);

                if (card3D.portraitImage != null && opData.portrait != null)
                {
                    Animator animator = card3D.portraitImage.GetComponent<Animator>();
                    if (animator == null)
                    {
                        animator = card3D.portraitImage.gameObject.AddComponent<Animator>();
                    }
                    animator.runtimeAnimatorController = opData.portrait;
                }
            }
            freelancerCards.Add(new FreelancerCardData { cardObject = cardObject, freelancerData = opData, originalIndex = i });
        }
        RepositionCards();
    }

    public void SetActiveFreelancer(int freelancerIndex)
    {
        if (freelancerCards.Count > 0 && freelancerCards[0].originalIndex == freelancerIndex)
        {
            return;
        }
        freelancerCards = freelancerCards.OrderBy(c => c.originalIndex).ToList();
        FreelancerCardData targetCard = freelancerCards.FirstOrDefault(c => c.originalIndex == freelancerIndex);
        if (targetCard != null)
        {
            freelancerCards.Remove(targetCard);
            freelancerCards.Insert(0, targetCard);
        }
        activeFreelancerIndex = freelancerIndex; 
        RepositionCards();
    }

    public void CycleActiveFreelancer()
    {
        if (freelancerCards.Count <= 1) return;

        FreelancerCardData cardToMove = freelancerCards[0];
        freelancerCards.RemoveAt(0);
        freelancerCards.Add(cardToMove);

        RepositionCards();
    }

    private void RepositionCards()
    {
        if (!gameObject.activeInHierarchy || freelancerCards.Count == 0) return;

        var visibleCards = freelancerCards.Where(c => c.cardObject != null && c.cardObject.activeSelf).ToList();

        for (int i = 0; i < visibleCards.Count; i++)
        {
            FreelancerCardData cardData = visibleCards[i];
            CalculateTargetTransforms(cardData, visibleCards, i);
            
            if (cardData.activeAnimation != null) StopCoroutine(cardData.activeAnimation);
            cardData.activeAnimation = StartCoroutine(AnimateCardTransform(cardData));
        }
    }
    
    private void CalculateTargetTransforms(FreelancerCardData cardData, List<FreelancerCardData> visibleCards, int visibleIndex)
    {
        float finalY;

        if (currentMode == DisplayMode.Setup)
        {
            int cardCount = visibleCards.Count;
            int cardIndex = visibleIndex;

            bool isThisCardHovered = cardData.cardObject == hoveredCard;
            int hoveredIndex = (hoveredCard != null) ? visibleCards.FindIndex(c => c.cardObject == hoveredCard) : -1;

            float totalWidth = (cardCount - 1) * setupCardSpacing;
            float startX = setupStartPosition.x - (totalWidth / 2f);
            finalY = setupStartPosition.y;
            float finalX = startX + (cardIndex * setupCardSpacing);

            if (hoveredIndex != -1)
            {
                if (cardIndex > hoveredIndex) finalX += setupHoverSpacingPush;
                else if (cardIndex < hoveredIndex) finalX -= setupHoverSpacingPush;
            }

            cardData.targetPosition = new Vector3(finalX, finalY, 0);
            cardData.targetRotation = Quaternion.identity;
            float finalScale = isThisCardHovered ? setupCardScale * setupHoverScaleMultiplier : setupCardScale;
            cardData.targetScale = Vector3.one * cardSize * finalScale;
        }
        else if (currentMode == DisplayMode.Manager)
        {
            var selectableCards = freelancerCards.Where(c => c.isSelectableTarget).ToList();
            int cardCount = selectableCards.Count;
            int cardIndex = selectableCards.IndexOf(cardData);

            if (!cardData.isSelectableTarget)
            {
                float totalHeight = (freelancerCards.Count - 1) * prepCardSpacing;
                float startY = prepColumnCenterPosition.y - (totalHeight / 2f);
                int originalIndex = freelancerCards.IndexOf(cardData);
                finalY = startY + (originalIndex * prepCardSpacing);

                cardData.targetPosition = new Vector3(cardsXPosition + invalidTargetXOffset, finalY, 0);
                cardData.targetRotation = Quaternion.identity;
                cardData.targetScale = Vector3.one * cardSize * prepCardScale;
                return;
            }
            
            bool isThisCardHovered = cardData.cardObject == hoveredCard;
            int hoveredIndex = (hoveredCard != null) ? selectableCards.FindIndex(c => c.cardObject == hoveredCard) : -1;

            float totalWidth = (cardCount - 1) * managerCardSpacing;
            float startX = managerStartPosition.x - (totalWidth / 2f);
            finalY = managerStartPosition.y;
            float finalX = startX + (cardIndex * managerCardSpacing);

            if (hoveredIndex != -1)
            {
                if (cardIndex > hoveredIndex) finalX += managerHoverSpacingPush;
                else if (cardIndex < hoveredIndex) finalX -= managerHoverSpacingPush;
            }

            cardData.targetPosition = new Vector3(finalX, finalY, 0);
            cardData.targetRotation = Quaternion.identity;
            float finalScale = isThisCardHovered ? managerCardScale * managerHoverScaleMultiplier : managerCardScale;
            cardData.targetScale = Vector3.one * cardSize * finalScale;
        }
        else if (currentMode == DisplayMode.Action)
        {
            int cardListIndex = freelancerCards.IndexOf(cardData);
            bool isThisCardActive = cardListIndex == 0;
            if (isThisCardActive)
            {
                cardData.targetPosition = activeCardPosition;
                cardData.targetScale = Vector3.one * cardSize * activeCardScale;
                cardData.targetRotation = Quaternion.Euler(0, activeCardRotationY, 0);
            }
            else
            {
                int inactiveIndex = cardListIndex - 1;
                if (inactiveIndex < 0) inactiveIndex = 0;
                float finalX = carouselStartPosition.x + (inactiveIndex * carouselSpacingXStep);
                float yPos = carouselStartPosition.y + (inactiveIndex * carouselCardSpacing);
                float zPos = inactiveIndex * carouselSpacingZStep;
                cardData.targetPosition = new Vector3(finalX, yPos, zPos);
                cardData.targetScale = Vector3.one * cardSize * inactiveCardScale;
                cardData.targetRotation = Quaternion.Euler(0, carouselRotationYStep, 0);
            }
        }
        else // DisplayMode.Preparation
        {
            int cardCount = visibleCards.Count;
            int cardIndex = visibleIndex;
            
            bool isThisCardHovered = cardData.cardObject == hoveredCard;
            int hoveredIndex = (hoveredCard != null) ? visibleCards.FindIndex(c => c.cardObject == hoveredCard) : -1;

            float totalHeight = (cardCount - 1) * prepCardSpacing;
            float startY = prepColumnCenterPosition.y - (totalHeight / 2f);
            float finalX = cardsXPosition + (cardWidth / 2f);

            if (isThisCardHovered)
            {
                finalX += prepHoverPositionOffset;
            }
            
            finalY = startY + (cardIndex * prepCardSpacing);

            if (hoveredIndex != -1)
            {
                if (cardIndex > hoveredIndex) finalY += prepHoverSpacingPush;
                else if (cardIndex < hoveredIndex) finalY -= prepHoverSpacingPush;
            }

            cardData.targetPosition = new Vector3(finalX, finalY, 0);
            cardData.targetRotation = Quaternion.identity;
            float scaleMultiplier = isThisCardHovered ? prepCardScale * prepHoverScaleMultiplier : prepCardScale;
            cardData.targetScale = Vector3.one * cardSize * scaleMultiplier;
        }
    }

    private System.Collections.IEnumerator AnimateCardTransform(FreelancerCardData cardData)
    {
        if (cardData.cardObject == null) yield break;
        Vector3 startPos = cardData.cardObject.transform.localPosition;
        Vector3 startScale = cardData.cardObject.transform.localScale;
        Quaternion startRot = cardData.cardObject.transform.localRotation;
        float elapsedTime = 0f;
        while (elapsedTime < transitionDuration)
        {
            float t = elapsedTime / transitionDuration;
            float easedT = t * t * (3f - 2f * t);
            cardData.cardObject.transform.localPosition = Vector3.Lerp(startPos, cardData.targetPosition, easedT);
            cardData.cardObject.transform.localScale = Vector3.Lerp(startScale, cardData.targetScale, easedT);
            cardData.cardObject.transform.localRotation = Quaternion.Lerp(startRot, cardData.targetRotation, easedT);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        cardData.cardObject.transform.localPosition = cardData.targetPosition;
        cardData.cardObject.transform.localScale = cardData.targetScale;
        cardData.cardObject.transform.localRotation = cardData.targetRotation;
        cardData.activeAnimation = null;
    }

    public void RemoveFreelancerCard(FreelancerData freelancerDataToRemove)
    {
        FreelancerCardData cardToRemove = freelancerCards.FirstOrDefault(c => c.freelancerData == freelancerDataToRemove);
        if (cardToRemove != null)
        {
            if (cardToRemove.originalIndex == activeFreelancerIndex)
            {
                activeFreelancerIndex = -1;
            }
            freelancerCards.Remove(cardToRemove);
            if (cardToRemove.cardObject != null)
            {
                Destroy(cardToRemove.cardObject);
            }
            RepositionCards();
        }
    }
    
    public void UpdateEnergyOnCard(FreelancerData freelancerData, int actionCount, int utilityCount, int auraCount)
    {
        var cardToUpdate = freelancerCards.FirstOrDefault(card => card.freelancerData == freelancerData);
        if (cardToUpdate != null && cardToUpdate.cardObject != null)
        {
            Card3DLayout layout = cardToUpdate.cardObject.GetComponentInChildren<Card3DLayout>();
            if (layout != null)
            {
                layout.UpdateFreelancerEnergyDisplay(actionCount, utilityCount, auraCount);
            }
        }
    }

    public void UpdateBombIndicator(FreelancerData freelancerData, bool visible)
    {
        var cardToUpdate = freelancerCards.FirstOrDefault(card => card.freelancerData == freelancerData);

        if (cardToUpdate != null && cardToUpdate.cardObject != null)
        {
            Card3DLayout layout = cardToUpdate.cardObject.GetComponentInChildren<Card3DLayout>();
            if (layout != null && layout.bombIndicatorText != null)
            {
                layout.bombIndicatorText.gameObject.SetActive(visible);
                layout.bombIndicatorText.text = visible ? "💣" : "";
            }
        }
    }

    public void FilterForTargetSelection(List<GameObject> validTargets)
    {
        if (validTargets == null)
        {
            foreach (var card in freelancerCards)
            {
                card.isSelectableTarget = true;
            }
            return;
        }

        foreach (var card in freelancerCards)
        {
            bool isValid = validTargets.Any(validPiece => 
                ServiceLocator.Freelancers.GetFreelancerInstance(validPiece)?.CardGameObject == card.cardObject
            );
            card.isSelectableTarget = isValid;
        }
    }
    
    #region UTILITY METHODS
    public int GetCardCount() => freelancerCards.Count;
    public int GetActiveFreelancerIndex() => activeFreelancerIndex;
    #endregion

    #region Métodos de Criação e Limpeza
    public void ClearAllCards()
    {
        foreach (var cardData in freelancerCards)
        {
            if (cardData.cardObject != null) Destroy(cardData.cardObject);
        }
        freelancerCards.Clear();
        activeFreelancerIndex = -1;
    }
    
    public void SetVisibility(bool visible)
    {
        gameObject.SetActive(visible);
    }
    
    private CardData CreateCardDataFromFreelancer(FreelancerData freelancerData)
    {
        CardData cardData = ScriptableObject.CreateInstance<CardData>();
        cardData.cardName = freelancerData.name;
        cardData.cardType = CardType.Freelancer;
        cardData.portrait = null;
        cardData.freelancerHP = freelancerData.HP;
        cardData.weaponName = freelancerData.weaponName;
        cardData.weaponCost = FormatActionCost(freelancerData.weaponCost);
        cardData.weaponDamage = freelancerData.weaponStats.damage;
        cardData.weaponInfo = freelancerData.weaponInfo;
        
        // --- INÍCIO DA CORREÇÃO ---
        // Assumindo que você renomeou 'AbilityData' para 'TechniqueData' e 'abilities' para 'techniques'
        if (freelancerData.techniques != null && freelancerData.techniques.Count > 0 && freelancerData.techniques[0] != null)
        {
            TechniqueData technique = freelancerData.techniques[0];
            cardData.techniqueName = technique.techniqueName;
            cardData.techniqueCost = FormatActionCost(technique.cost);
            cardData.techniqueInfo = technique.description;
        }

        if (freelancerData.ultimate != null)
        {
            cardData.ultimateName = freelancerData.ultimate.techniqueName;
            cardData.ultimateCost = FormatActionCost(freelancerData.ultimate.cost);
            cardData.ultimateInfo = freelancerData.ultimate.description;
        }
        // --- FIM DA CORREÇÃO ---

        cardData.footer = freelancerData.footerInfo;
        return cardData;
    }
    
    private string FormatActionCost(ActionCost cost)
    {
        const string ACTION_EMOJI = "⚡";
        const string UTILITY_EMOJI = "🧰";
        const string AURA_EMOJI = "🌀";
        System.Text.StringBuilder costBuilder = new System.Text.StringBuilder();
        if (cost.action > 0) { for (int i = 0; i < cost.action; i++) costBuilder.Append(ACTION_EMOJI); }
        if (cost.utility > 0) { if (costBuilder.Length > 0) costBuilder.Append(" "); for (int i = 0; i < cost.utility; i++) costBuilder.Append(UTILITY_EMOJI); }
        if (cost.aura > 0) { if (costBuilder.Length > 0) costBuilder.Append(" "); for (int i = 0; i < cost.aura; i++) costBuilder.Append(AURA_EMOJI); }
        if (costBuilder.Length == 0) return "0";
        return costBuilder.ToString();
    }
    #endregion
}