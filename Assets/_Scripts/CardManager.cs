// _Scripts/CardManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class CardManager : MonoBehaviour
{
    #region Fields & Properties
    [Header("3D Card Settings")]
    public GameObject card3DPrefab;
    public Hand3DContainer player1Hand3D;
    public Hand3DContainer player2Hand3D;

    [Header("Freelancer Card Settings")]
    public FreelancersUIContainer player1FreelancersUI;
    public FreelancersUIContainer player2FreelancersUI;

    [Header("Card 3D Materials")]
    public Material actionMaterial;
    public Material utilityMaterial;
    public Material auraMaterial;
    public Material skillMaterial;
    public Material strategyMaterial;
    public Material freelancerMaterial;
    public Material borderMaterial;
    public Material backMaterial;

    [Header("Card Layout Configuration")]
    [Tooltip("Tamanho das cartas (escala)")]
    public float cardSize = 180f;
    [Tooltip("Espaçamento normal entre cartas")]
    public float cardSpacement = 95f;
    [Tooltip("Altura das cartas na tela (Y position)")]
    public float cardsHeight = -150f;
    [Tooltip("Largura máxima horizontal disponível para cartas")]
    public float spaceWidth = 760f;
    [Tooltip("Rotação adicionada progressivamente (A * B * número_de_cartas)")]
    public float addRotation = 2f;

    [Header("Card Selection Configuration")]
    [Tooltip("Deslocamento Y da carta quando selecionada (altura)")]
    public float selectedYOffset = 15f;
    [Tooltip("Deslocamento Z da carta quando selecionada (profundidade)")]
    public float selectedZOffset = 100f;
    [Tooltip("Escala da carta quando selecionada")]
    public float selectedScale = 1.2f;
    [Tooltip("Duração da animação de seleção (segundos)")]
    public float selectionAnimationDuration = 0.3f;

    [Header("Manager Mode")]
    public Hand3DContainer managerHand3D;

    private bool isInManagerMode = false;
    private List<CardData> player1Hand = new List<CardData>();
    private List<CardData> player2Hand = new List<CardData>();
    private CardData selectedCardToEquip = null;
    private GameObject selectedCard3DObject = null;
    private bool isInEquipMode = false;
    private bool isSkillModeActive = false;
    private GameObject selectedSkillCardObject = null;
    private Dictionary<GameObject, Card3D> card3DMap = new Dictionary<GameObject, Card3D>();
    private List<CardData> player1UsedCards = new List<CardData>();
    private List<CardData> player2UsedCards = new List<CardData>();
    private bool freelancerCardsInitialized = false;
    private bool isAllySelectionMode = false;
    private Action<GameObject> onAllySelectedCallback;
    private CardData sourceCardForSelection;
    public static event Action<CardData, bool> OnCardDrawn;
    public static event Action<CardData, GameObject, GameObject> OnCardEquipped;
    public static event Action<CardData, GameObject> OnCardUsed;
    public static event Action<CardData, bool> OnCardDiscarded;
    public static event Action<bool> OnEquipModeEntered;
    public static event Action OnEquipModeExited;
    public static event Action<bool> OnHandRefreshed;
    public static event Action<int, bool> OnFreelancerCardHighlighted;
    public static event Action<GameObject> OnFreelancerCardSelectedForSetup;
    private bool isEnergyEquipMode = false;
    private GameObject selectedEnergyCardObject = null;
    private GameObject sourceSkillCardObject = null; // Para guardar a carta "Overload"
    #endregion

    #region Unity Lifecycle
    void Start() => Initialize();

    void OnDestroy()
    {
        if (player1Hand3D != null) player1Hand3D.ClearAllCards();
        if (player2Hand3D != null) player2Hand3D.ClearAllCards();
        if (player1FreelancersUI != null) player1FreelancersUI.ClearAllCards();
        if (player2FreelancersUI != null) player2FreelancersUI.ClearAllCards();
        player1Hand.Clear();
        player2Hand.Clear();
        player1UsedCards.Clear();
        player2UsedCards.Clear();
        card3DMap.Clear();
        OnCardDrawn = null;
        OnCardEquipped = null;
        OnCardUsed = null;
        OnCardDiscarded = null;
        OnEquipModeEntered = null;
        OnEquipModeExited = null;
        OnHandRefreshed = null;
        OnFreelancerCardHighlighted = null;
        OnFreelancerCardSelectedForSetup = null;
    }
    #endregion

    #region Initialization
    private void Initialize()
    {
        if (card3DPrefab == null) Debug.LogError("CardManager: card3DPrefab not assigned!");
        if (player1Hand3D == null || player2Hand3D == null) Debug.LogError("CardManager: Hand3D containers not assigned!");
        if (player1FreelancersUI == null || player2FreelancersUI == null) Debug.LogError("CardManager: FreelancersUI containers not assigned!");
        ValidateMaterials();
        SetupHand3DContainers();
        Debug.Log("<color=gold>[CardManager]</color> Initialized successfully for 3D cards and freelancer cards");
    }

    private void ValidateMaterials()
    {
        List<string> missingMaterials = new List<string>();
        if (actionMaterial == null) missingMaterials.Add("Action Material");
        if (utilityMaterial == null) missingMaterials.Add("Utility Material");
        if (auraMaterial == null) missingMaterials.Add("Aura Material");
        if (skillMaterial == null) missingMaterials.Add("Skill Material");
        if (strategyMaterial == null) missingMaterials.Add("Strategy Material");
        if (freelancerMaterial == null) missingMaterials.Add("Freelancer Material");
        if (borderMaterial == null) missingMaterials.Add("Border Material");
        if (backMaterial == null) missingMaterials.Add("Back Material");
        if (missingMaterials.Count > 0)
            Debug.LogError($"CardManager: Missing materials: {string.Join(", ", missingMaterials)}");
    }

    private void SetupHand3DContainers()
    {
        if (player1Hand3D != null)
        {
            player1Hand3D.isPlayer1Hand = true;
            player1Hand3D.SetLayoutConfig(cardSize, cardSpacement, cardsHeight, spaceWidth, addRotation, selectedYOffset, selectedZOffset, selectedScale, selectionAnimationDuration);
        }
        if (player2Hand3D != null)
        {
            player2Hand3D.isPlayer1Hand = false;
            player2Hand3D.SetLayoutConfig(cardSize, cardSpacement, cardsHeight, spaceWidth, addRotation, selectedYOffset, selectedZOffset, selectedScale, selectionAnimationDuration);
        }
    }
    #endregion

    #region Freelancer Card System
    public void InitializeFreelancerCards(List<FreelancerData> player1Freelancers, List<FreelancerData> player2Freelancers)
    {
        if (freelancerCardsInitialized)
        {
            Debug.LogWarning("CardManager: Freelancer cards already initialized!");
            return;
        }
        if (player1FreelancersUI != null && player1Freelancers != null)
            player1FreelancersUI.InitializeFreelancerCards(player1Freelancers, card3DPrefab, this);
        if (player2FreelancersUI != null && player2Freelancers != null)
            player2FreelancersUI.InitializeFreelancerCards(player2Freelancers, card3DPrefab, this);
        freelancerCardsInitialized = true;
        SetFreelancerHandVisibility(false, false);
    }

    public void SetActiveFreelancer(int index, bool isPlayer1)
    {
        var hand = isPlayer1 ? player1FreelancersUI : player2FreelancersUI;
        if (hand != null) hand.SetActiveFreelancer(index);
    }

    public void SetFreelancerHandVisibility(bool showPlayer1, bool showPlayer2)
    {
        if (player1FreelancersUI != null) player1FreelancersUI.SetVisibility(showPlayer1);
        if (player2FreelancersUI != null) player2FreelancersUI.SetVisibility(showPlayer2);
    }

    public void SetFreelancerHandVisibilityForCurrentPlayer(bool isPlayer1Turn)
    {
        SetFreelancerHandVisibility(isPlayer1Turn, !isPlayer1Turn);
    }

    public void SetFreelancerHandsDisplayMode(FreelancersUIContainer.DisplayMode mode)
    {
        if (player1FreelancersUI != null)
            player1FreelancersUI.SetDisplayMode(mode);
        if (player2FreelancersUI != null)
            player2FreelancersUI.SetDisplayMode(mode);
    }

    public void UpdateFreelancerCardEnergy(FreelancerData opData, bool isPlayer1, int action, int utility, int aura)
    {
        FreelancersUIContainer targetContainer = isPlayer1 ? player1FreelancersUI : player2FreelancersUI;
        if (targetContainer != null)
            targetContainer.UpdateEnergyOnCard(opData, action, utility, aura);
    }

    public void NotifyFreelancerDied(FreelancerInstance deadFreelancer)
    {
        if (deadFreelancer == null) return;
        FreelancersUIContainer hand = deadFreelancer.IsPlayer1 ? player1FreelancersUI : player2FreelancersUI;
        if (hand != null)
            hand.RemoveFreelancerCard(deadFreelancer.BaseData);
    }

    public void CycleActiveFreelancer(bool isPlayer1)
    {
        var hand = isPlayer1 ? player1FreelancersUI : player2FreelancersUI;
        if (hand != null)
            hand.CycleActiveFreelancer();
    }

    public void SetBombIndicator(FreelancerInstance freelancerInstance, bool visible)
    {
        if (freelancerInstance == null) return;
        FreelancersUIContainer targetHand = freelancerInstance.IsPlayer1 ? player1FreelancersUI : player2FreelancersUI;
        if (targetHand != null)
            targetHand.UpdateBombIndicator(freelancerInstance.BaseData, visible);
    }
    #endregion

    #region Card Drawing System
    public void DrawCardsForCurrentPlayer(int amount, bool isPlayer1)
    {
        var targetHand = isPlayer1 ? player1Hand : player2Hand;
        var targetHand3D = isPlayer1 ? player1Hand3D : player2Hand3D;
        if (ServiceLocator.Decks == null)
        {
            Debug.LogError("[CardManager] DeckManager not found via ServiceLocator! Cannot draw cards.");
            return;
        }
        for (int i = 0; i < amount; i++)
        {
            if (targetHand.Count >= GameConfig.Instance.maxHandSize)
            {
                Debug.Log($"[CardManager] Hand for {(isPlayer1 ? "Player 1" : "Player 2")} is full. Stopping draw.");
                break;
            }
            CardData drawnCard = ServiceLocator.Decks.DrawCard(isPlayer1);
            if (drawnCard != null)
            {
                targetHand.Add(drawnCard);
                CreateCard3D(drawnCard, targetHand3D, isPlayer1);
                OnCardDrawn?.Invoke(drawnCard, isPlayer1);
            }
            else
            {
                Debug.LogWarning($"[CardManager] Deck for {(isPlayer1 ? "Player 1" : "Player 2")} is empty. Stopping draw.");
                break;
            }
        }
    }
    #endregion

    #region Card Interaction System
     public void OnCard3DClicked(CardData card, GameObject card3DObject)
    {
        if (card == null || card3DObject == null) return;
        bool isPlayer1Turn = ServiceLocator.Game.IsPlayer1Turn();

        // --- NOVA LÓGICA PARA CARTAS DE ESTRATÉGIA ---
        if (ServiceLocator.Game.CurrentState is PreparationState prepState && card.cardType == CardType.Strategy)
        {
            if (prepState.hasUsedStrategyCardThisTurn)
            {
                ServiceLocator.Audio.PlayConditionFailSound();
                Debug.LogWarning("[CardManager] Já foi usada uma carta de Estratégia neste turno.");
                return;
            }

            if (selectedCard3DObject == card3DObject) // Segundo clique, para ativar
            {
                UseStrategyCard(card3DObject);
                return;
            }
            
            // Primeiro clique, para selecionar
            if (selectedCard3DObject != null)
                selectedCard3DObject.GetComponent<Card3D>()?.SetSelected(false);
            
            selectedCardToEquip = card;
            selectedCard3DObject = card3DObject;
            card3DObject.GetComponent<Card3D>()?.SetSelected(true);
            return;
        }
        // --- FIM DA NOVA LÓGICA ---

        if (isAllySelectionMode)
        {
            if (card.cardType == CardType.Freelancer)
            {
                var targetInstance = ServiceLocator.Freelancers.GetFreelancerInstanceByCard(card3DObject);
                if (targetInstance != null && onAllySelectedCallback != null)
                {
                    onAllySelectedCallback.Invoke(targetInstance.PieceGameObject);
                }
            }
            ExitAllySelectionMode();
            return;
        }

        if (isEnergyEquipMode)
        {
            if (!card.IsEnergyCard()) return; 

            if (selectedEnergyCardObject == card3DObject)
            {
                EquipEnergyFromHand(card3DObject);
                return;
            }
            
            if (selectedEnergyCardObject != null)
                selectedEnergyCardObject.GetComponent<Card3D>()?.SetSelected(false);
            
            selectedEnergyCardObject = card3DObject;
            selectedEnergyCardObject.GetComponent<Card3D>()?.SetSelected(true);
            return;
        }

        if (isSkillModeActive)
        {
            if (card.cardType != CardType.Skill) return;
            if (selectedSkillCardObject == card3DObject)
            {
                UseSkillCard(card3DObject);
                return;
            }
            if (selectedSkillCardObject != null)
                selectedSkillCardObject.GetComponent<Card3D>()?.SetSelected(false);
            selectedSkillCardObject = card3DObject;
            selectedSkillCardObject.GetComponent<Card3D>()?.SetSelected(true);
            return;
        }

        if (isInManagerMode)
        {
            if (isInEquipMode && card.cardType == CardType.Freelancer)
            {
                var targetInstance = ServiceLocator.Freelancers.GetFreelancerInstanceByCard(card3DObject);
                var sourceInstance = ServiceLocator.Freelancers.GetFreelancerInstance((ServiceLocator.Game.CurrentState as ActionState).GetActiveFreelancer());
                if (targetInstance != null && sourceInstance != null)
                {
                    if (targetInstance == sourceInstance)
                    {
                        Debug.LogWarning("[CardManager] A transferência de energia para o próprio doador foi bloqueada.");
                        ExitEquipMode();
                        return;
                    }
                    if (ServiceLocator.Freelancers.TransferEnergy(sourceInstance.PieceGameObject, targetInstance.PieceGameObject, selectedCardToEquip))
                    {
                        managerHand3D.RemoveCard(selectedCard3DObject);
                        Destroy(selectedCard3DObject);
                        ExitEquipMode();
                    }
                }
                return;
            }
            if (IsEquippableCard(card.cardType) && managerHand3D.ContainsCard(card3DObject))
            {
                EnterEquipMode(card, card3DObject);
            }
            return;
        }

        if (ServiceLocator.Game.CurrentState is SetupState && card.cardType == CardType.Freelancer)
        {
            FreelancerInstance targetInstance = ServiceLocator.Freelancers.GetFreelancerInstanceByCard(card3DObject);
            if (targetInstance != null && targetInstance.PieceGameObject != null)
            {
                OnFreelancerCardSelectedForSetup?.Invoke(targetInstance.PieceGameObject);
            }
            return;
        }

        if (isInEquipMode && card.cardType == CardType.Freelancer)
        {
            FreelancerInstance targetInstance = ServiceLocator.Freelancers.GetFreelancerInstanceByCard(card3DObject);
            if (targetInstance != null && targetInstance.PieceGameObject != null)
            {
                GameObject targetPiece = targetInstance.PieceGameObject;
                if (targetInstance.IsPlayer1 == isPlayer1Turn)
                    TryEquipSelectedCard(targetPiece, isPlayer1Turn);
            }
            return;
        }

        if (card.cardType == CardType.Freelancer) return;

        if (isInEquipMode && IsEquippableCard(card.cardType))
        {
            if (selectedCard3DObject != card3DObject)
            {
                if (selectedCard3DObject != null)
                    selectedCard3DObject.GetComponent<Card3D>()?.SetSelected(false);
                selectedCardToEquip = card;
                selectedCard3DObject = card3DObject;
                card3DObject.GetComponent<Card3D>()?.SetSelected(true);
                return;
            }
            else
            {
                ExitEquipMode();
                return;
            }
        }

        if (IsEquippableCard(card.cardType))
            EnterEquipMode(card, card3DObject);
        else
            HandleNonEquippableCard(card, card3DObject);
    }

    private void HandleNonEquippableCard(CardData card, GameObject card3DObject)
    {
        Debug.Log($"<color=gold>[CardManager]</color> {card.cardName} is not equippable (Type: {card.cardType})");
    }
    #endregion

    #region Equipment System
    private bool IsEquippableCard(CardType cardType)
    {
        return cardType == CardType.Action || cardType == CardType.Utility || cardType == CardType.Aura;
    }

    private void EnterEquipMode(CardData card, GameObject card3DObject)
    {
        if (selectedCard3DObject != null)
            selectedCard3DObject.GetComponent<Card3D>()?.SetSelected(false);
        selectedCardToEquip = card;
        selectedCard3DObject = card3DObject;
        isInEquipMode = true;
        card3DObject.GetComponent<Card3D>()?.SetSelected(true);
        bool isPlayer1 = player1Hand3D != null && player1Hand3D.ContainsCard(card3DObject);
        OnEquipModeEntered?.Invoke(isPlayer1);
    }

    public void ExitEquipMode()
    {
        if (selectedCard3DObject != null)
            selectedCard3DObject.GetComponent<Card3D>()?.SetSelected(false);
        selectedCardToEquip = null;
        selectedCard3DObject = null;
        isInEquipMode = false;
        OnEquipModeExited?.Invoke();
    }

    public bool TryEquipCard3D(CardData card, GameObject targetPiece, GameObject card3DObject)
    {
        if (card == null || targetPiece == null || ServiceLocator.Freelancers == null)
            return false;
        if (!IsEquippableCard(card.cardType))
            return false;
        bool isPlayer1 = player1Hand3D != null && player1Hand3D.ContainsCard(card3DObject);
        if (!ServiceLocator.Pieces.IsPieceOnTeam(targetPiece, isPlayer1))
            return false;
        bool success = ServiceLocator.Freelancers.EquipEnergy(targetPiece, card);
        if (success)
        {
            var targetHand = isPlayer1 ? player1Hand : player2Hand;
            var targetHand3D = isPlayer1 ? player1Hand3D : player2Hand3D;
            targetHand.Remove(card);
            if (targetHand3D != null)
                targetHand3D.RemoveCard(card3DObject);
            card3DMap.Remove(card3DObject);
            Destroy(card3DObject);
            OnCardEquipped?.Invoke(card, targetPiece, card3DObject);
            ExitEquipMode();
            return true;
        }
        else
        {
            ExitEquipMode();
            return false;
        }
    }

    public bool TryEquipSelectedCard(GameObject targetPiece, bool isPlayer1)
    {
        if (!isInEquipMode || selectedCardToEquip == null || selectedCard3DObject == null)
            return false;
        return TryEquipCard3D(selectedCardToEquip, targetPiece, selectedCard3DObject);
    }
    private void UseStrategyCard(GameObject cardObject)
    {
        var card3D = cardObject.GetComponent<Card3D>();
        var cardData = card3D?.GetCardData() as SupportData;
        var prepState = ServiceLocator.Game.CurrentState as PreparationState;

        if (cardData == null || prepState == null) return;

        foreach (var condition in cardData.conditions)
        {
            if (!condition.Check(null, null))
            {
                ServiceLocator.Audio.PlayConditionFailSound();
                return;
            }
        }

        bool isPlayer1 = player1Hand3D.ContainsCard(cardObject);
        bool effectTriggered = false;

        // Itera pelos modificadores para encontrar e executar a lógica correta
        foreach (var modifier in cardData.modifiers)
        {
            switch (modifier.logic)
            {
                case ModifierLogic.SearchDeck:
                    TriggerSearchDeck(modifier.value, modifier.cardTypeToSearch, isPlayer1);
                    effectTriggered = true;
                    break;
                
                // Futuramente, podemos adicionar outros cases aqui se necessário
                default:
                    // Para efeitos simples como "DrawCards", podemos delegar ao EffectManager
                    ServiceLocator.Effects.ExecuteStrategyModifier(modifier, isPlayer1);
                    effectTriggered = true;
                    break;
            }
        }
        
        if (!effectTriggered)
        {
            Debug.LogWarning($"A carta de estratégia '{cardData.cardName}' foi usada mas não possui um modificador com lógica implementada.");
            return;
        }

        ServiceLocator.Audio.PlaySkillSuccessSound();

        prepState.hasUsedStrategyCardThisTurn = true;
        
        var playerHand = isPlayer1 ? player1Hand : player2Hand;
        var hand3D = isPlayer1 ? player1Hand3D : player2Hand3D;
        
        playerHand.Remove(cardData);
        card3DMap.Remove(cardObject);
        hand3D.RemoveCard(cardObject);
        ServiceLocator.Decks.AddToDiscard(cardData, isPlayer1);
        Destroy(cardObject);
        
        ExitEquipMode(); 
    }
    // _Scripts/CardManager.cs

    // _Scripts/CardManager.cs

    public void TriggerSearchDeck(int amount, CardType? type, bool isPlayer1)
    {
        List<CardData> deckForSearch = ServiceLocator.Decks.GetDeckForSearch(isPlayer1);

        ServiceLocator.Search.StartSearch(deckForSearch, amount, type, false, (selectedCards) => {
            
            // Lógica de Confirmação (se houver cartas selecionadas)
            if (selectedCards != null && selectedCards.Count > 0)
            {
                Debug.Log($"Seleção concluída! {selectedCards.Count} cartas selecionadas.");
                
                foreach(var card in selectedCards)
                {
                    // Remove a carta permanentemente do baralho de compra original
                    ServiceLocator.Decks.RemoveFromDrawPile(card, isPlayer1);
                    
                    // Adiciona a carta à mão
                    var targetHand = isPlayer1 ? player1Hand : player2Hand;
                    var targetHand3D = isPlayer1 ? player1Hand3D : player2Hand3D;
                    
                    targetHand.Add(card);
                    CreateCard3D(card, targetHand3D, isPlayer1);
                }
            }

            // --- CORREÇÃO AQUI ---
            // O baralho agora é reembaralhado em TODAS as circunstâncias ao sair da busca.
            Debug.Log("Saindo do modo de busca. Reembaralhando o baralho restante.");
            ServiceLocator.Decks.ShuffleDeck(isPlayer1);
        });
    }
    
    #endregion

    #region Skill System
    public void EnterSkillMode(bool enter)
    {
        isSkillModeActive = enter;
        if (ServiceLocator.Game.CurrentState is ActionState actionState)
            actionState.IsSkillModeActive = enter;

        var currentHand3D = ServiceLocator.Game.IsPlayer1Turn() ? player1Hand3D : player2Hand3D;

        if (enter)
        {
            ServiceLocator.UI.HideActionMenu();
            currentHand3D.gameObject.SetActive(true);
            // Atualizado para usar o novo predicate
            currentHand3D.FilterAndRepositionCards(card => card.cardType == CardType.Skill);
        }
        else
        {
            if (selectedSkillCardObject != null)
            {
                selectedSkillCardObject.GetComponent<Card3D>()?.SetSelected(false);
                selectedSkillCardObject = null;
            }
            // Atualizado para usar o novo predicate (null para resetar)
            currentHand3D.FilterAndRepositionCards(null);
            currentHand3D.gameObject.SetActive(false);
        }
    }

    private void UseSkillCard(GameObject cardObject)
    {
        if (cardObject == null) return;
        var card3D = cardObject.GetComponent<Card3D>();
        var cardData = card3D?.GetCardData() as SupportData;
        var actionState = ServiceLocator.Game.CurrentState as ActionState;

        if (cardData == null || actionState == null) return;

        GameObject activeFreelancer = actionState.GetActiveFreelancer();

        bool triggersPreFire = cardData.modifiers.Any(m => m.type == ModifierType.TriggersPreFireRoll);
        if (triggersPreFire)
        {
            // A lógica agora é encapsulada na nova corrotina.
            StartCoroutine(UsePreFireCardCoroutine(cardObject));
            return;
        }

        bool triggersEnergyEquip = cardData.modifiers.Any(m => m.type == ModifierType.TriggersEnergyEquipMode);
        if (triggersEnergyEquip)
        {
            EnterEnergyEquipMode(cardObject);
            return;
        }

        foreach (var condition in cardData.conditions)
        {
            if (!condition.Check(activeFreelancer, null))
            {
                FinalizeSkillCardPlay(false, cardObject, cardData, actionState);
                return;
            }
        }

        bool requiresTarget = cardData.modifiers.Any(m => m.type == ModifierType.RequiresAllyTarget);

        if (requiresTarget)
        {
            EnterAllySelectionMode(cardData, (selectedAlly) =>
            {
                foreach (var modifier in cardData.modifiers)
                {
                    if (modifier.logic == ModifierLogic.ApplyEffect && modifier.effectToApply != null)
                    {
                        GameObject finalTarget = null;
                        if (modifier.target == EffectTarget.Self) finalTarget = activeFreelancer;
                        else if (modifier.target == EffectTarget.TargetedAlly) finalTarget = selectedAlly;

                        if (finalTarget != null)
                        {
                            SupportData subCard = modifier.effectToApply as SupportData;
                            if(subCard == null) continue;

                            var chargeModifier = subCard.modifiers.FirstOrDefault(m => m.type == ModifierType.ModifyActionCharges);
                            
                            if (chargeModifier != null)
                            {
                                ServiceLocator.Freelancers.ModifyActionCharges(finalTarget, chargeModifier.value);
                            }
                            else
                            {
                                ServiceLocator.Effects.ApplyEffect(finalTarget, subCard, activeFreelancer);
                            }
                        }
                    }
                }
                FinalizeSkillCardPlay(true, cardObject, cardData, actionState);
                EnterSkillMode(false); // Esconde a mão após a seleção
            });
        }
        else
        {
            bool effectApplied = ServiceLocator.Effects.ApplyEffect(activeFreelancer, cardData, activeFreelancer);
            FinalizeSkillCardPlay(effectApplied, cardObject, cardData, actionState);
            EnterSkillMode(false); // Esconde a mão após o uso
        }
    }
       private void FinalizeSkillCardPlay(bool success, GameObject cardObject, CardData cardData, ActionState actionState)
    {
        if (success)
        {
            ServiceLocator.Audio.PlaySkillSuccessSound();
            ServiceLocator.Bomb.CancelDefuseIfDefuserActs(actionState.GetActiveFreelancer());
            
            bool isPlayer1 = player1Hand3D.ContainsCard(cardObject);
            var playerHand = isPlayer1 ? player1Hand : player2Hand;
            var hand3D = isPlayer1 ? player1Hand3D : player2Hand3D;
            
            // A carta é removida da mão e da UI, mas NÃO é enviada para o descarte.
            // Ela agora "vive" dentro do ActiveEffect que foi criado.
            playerHand.Remove(cardData);
            card3DMap.Remove(cardObject);
            hand3D.RemoveCard(cardObject);
            Destroy(cardObject);
            
            actionState.SetSkillUsed();
            
            if (cardData is SupportData supportCard)
            {
                if (supportCard.modifiers.Any(m => m.type == ModifierType.CanWidePeek))
                    actionState.ActivateWidePeek();
                if (supportCard.modifiers.Any(m => m.type == ModifierType.CanTransferEnergy))
                {
                    EnterManagerMode(actionState.GetActiveFreelancer());
                    return;
                }
            }

            if (ServiceLocator.Pieces != null)
            {
                ServiceLocator.Pieces.SelectPiece(actionState.GetActiveFreelancer(),
                    () => actionState.CanMove(),
                    () => actionState.CanAct(),
                    () => actionState.CanUseSkill());
            }
        }
        else
        {
            ServiceLocator.Audio.PlayConditionFailSound();
            if (selectedSkillCardObject != null)
            {
                selectedSkillCardObject.GetComponent<Card3D>()?.SetSelected(false);
                selectedSkillCardObject = null;
            }
        }
    }
    private System.Collections.IEnumerator UsePreFireCardCoroutine(GameObject cardObject)
    {
        var card3D = cardObject.GetComponent<Card3D>();
        var cardData = card3D?.GetCardData() as SupportData;
        var actionState = ServiceLocator.Game.CurrentState as ActionState;
        if (cardData == null || actionState == null) yield break;

        // 1. Esconde a mão de skills IMEDIATAMENTE.
        EnterSkillMode(false);

        GameObject activeFreelancer = actionState.GetActiveFreelancer();
        int bonus = cardData.modifiers.FirstOrDefault(m => m.type == ModifierType.TriggersPreFireRoll)?.value ?? 0;
        
        // Objeto para receber o resultado da corrotina do dado.
        var dieRollResult = new CoroutineResult<int>();
        
        // 2. CHAMA e ESPERA a corrotina do dado terminar.
        yield return StartCoroutine(ServiceLocator.Combat.RollDie(activeFreelancer, null, bonus, (result) => {
            dieRollResult.result = result;
        }));

        // 3. Após a animação do dado terminar, armazena o resultado.
        ServiceLocator.Freelancers.SetStoredDiceResult(activeFreelancer, dieRollResult.result);
        
        // 4. Finaliza o uso da carta de skill.
        FinalizeSkillCardPlay(true, cardObject, cardData, actionState);
    }
    #endregion

    #region Manager System
    public void EnterManagerMode(GameObject sourceFreelancer)
    {
        var sourceInstance = ServiceLocator.Freelancers.GetFreelancerInstance(sourceFreelancer);
        if (sourceInstance == null || sourceInstance.EquippedEnergies.Count == 0)
        {
            Debug.LogWarning("[CardManager] Freelancer não tem energias para transferir.");
            return;
        }
        isInManagerMode = true;
        ServiceLocator.UI.HideActionMenu();
        managerHand3D.ClearAllCards();
        foreach (var energyCard in sourceInstance.EquippedEnergies)
            CreateCard3D(energyCard, managerHand3D, sourceInstance.IsPlayer1);
        managerHand3D.gameObject.SetActive(true);
        bool isP1 = sourceInstance.IsPlayer1;
        player1FreelancersUI.SetDisplayMode(isP1 ? FreelancersUIContainer.DisplayMode.Manager : FreelancersUIContainer.DisplayMode.Action);
        player2FreelancersUI.SetDisplayMode(!isP1 ? FreelancersUIContainer.DisplayMode.Manager : FreelancersUIContainer.DisplayMode.Action);
        ServiceLocator.UI.ShowEndDropButton(true);
    }

    public void ExitManagerMode()
    {
        isInManagerMode = false;
        managerHand3D.ClearAllCards();
        managerHand3D.gameObject.SetActive(false);
        SetFreelancerHandsDisplayMode(FreelancersUIContainer.DisplayMode.Action);
        ServiceLocator.UI.endDropButton.gameObject.SetActive(false);
        ExitEquipMode();
    } 
    private void EnterAllySelectionMode(CardData sourceCard, Action<GameObject> onSelected)
    {
        isAllySelectionMode = true;
        onAllySelectedCallback = onSelected;
        sourceCardForSelection = sourceCard;

        // Esconde a mão de cartas de skill
        var currentHand = ServiceLocator.Game.IsPlayer1Turn() ? player1Hand3D : player2Hand3D;
        if(currentHand != null) currentHand.SetVisibility(false);

        if (ServiceLocator.UI != null)
        {
            ServiceLocator.UI.ShowGameMessage("Select an allied freelancer", 0);
        }

        var actionState = ServiceLocator.Game.CurrentState as ActionState;
        if (actionState == null) { ExitAllySelectionMode(); return; }

        bool isPlayer1 = ServiceLocator.Game.IsPlayer1Turn();
        var allAllies = ServiceLocator.Pieces.GetPlayerPieces(isPlayer1);
        int activeIndex = actionState.GetActiveFreelancerIndex();
        var activeFreelancer = actionState.GetActiveFreelancer();

        var validTargets = allAllies.Where(ally =>
        {
            if (ally == activeFreelancer) return false;
            if (ServiceLocator.Pieces.GetFreelancerIndex(ally) <= activeIndex) return false;
            if (ServiceLocator.Effects.HasEffectOfType(ally, ModifierType.ForbidAttack)) return false;
            return true;
        }).ToList();

        var container = isPlayer1 ? player1FreelancersUI : player2FreelancersUI;
        if (container != null)
        {
            container.FilterForTargetSelection(validTargets);
            container.SetDisplayMode(FreelancersUIContainer.DisplayMode.Manager);
        }
    }

     public void ExitAllySelectionMode()
    {
        isAllySelectionMode = false;
        onAllySelectedCallback = null;
        sourceCardForSelection = null;

        // --- CORREÇÃO APLICADA AQUI ---
        // Em vez de mostrar a mão, garantimos que ela fique escondida.
        var currentHand = ServiceLocator.Game.IsPlayer1Turn() ? player1Hand3D : player2Hand3D;
        if (currentHand != null) currentHand.SetVisibility(false);

        if (ServiceLocator.UI != null)
        {
            ServiceLocator.UI.HideGameMessage();
        }
        
        var container = ServiceLocator.Game.IsPlayer1Turn() ? player1FreelancersUI : player2FreelancersUI;
        if (container != null)
        {
            container.FilterForTargetSelection(null); 
            container.SetDisplayMode(FreelancersUIContainer.DisplayMode.Action);
        }
    }
    private void EnterEnergyEquipMode(GameObject overloadCardObject)
    {
        isEnergyEquipMode = true;
        sourceSkillCardObject = overloadCardObject;
        
        if(ServiceLocator.UI != null) ServiceLocator.UI.HideActionMenu();

        var currentHand3D = ServiceLocator.Game.IsPlayer1Turn() ? player1Hand3D : player2Hand3D;
        if (currentHand3D != null)
        {
            currentHand3D.gameObject.SetActive(true);
            // Usamos o novo filtro para mostrar apenas cartas de energia
            currentHand3D.FilterAndRepositionCards(card => card.IsEnergyCard());
        }
    }

    public void ExitEnergyEquipMode()
    {
        isEnergyEquipMode = false;
        if(selectedEnergyCardObject != null)
        {
            selectedEnergyCardObject.GetComponent<Card3D>()?.SetSelected(false);
            selectedEnergyCardObject = null;
        }
        
        var currentHand3D = ServiceLocator.Game.IsPlayer1Turn() ? player1Hand3D : player2Hand3D;
        if (currentHand3D != null)
        {
            // Apenas reseta o filtro e esconde a mão de cartas de energia.
            currentHand3D.FilterAndRepositionCards(null);
            currentHand3D.SetVisibility(false);
        }
        // Não há mais nenhuma chamada para SelectPiece ou DeselectCurrentPiece aqui.
    }

    private void EquipEnergyFromHand(GameObject energyCardObject)
    {
        var card3D = energyCardObject.GetComponent<Card3D>();
        var cardData = card3D?.GetCardData();
        var actionState = ServiceLocator.Game.CurrentState as ActionState;
        if (cardData == null || actionState == null) return;

        var activeFreelancer = actionState.GetActiveFreelancer();

        // Tenta equipar a energia
        bool success = ServiceLocator.Freelancers.EquipEnergy(activeFreelancer, cardData);

        if (success)
        {
            // Remove a carta de energia da mão
            bool isPlayer1 = player1Hand3D.ContainsCard(energyCardObject);
            var playerHand = isPlayer1 ? player1Hand : player2Hand;
            var hand3D = isPlayer1 ? player1Hand3D : player2Hand3D;
            playerHand.Remove(cardData);
            card3DMap.Remove(energyCardObject);
            hand3D.RemoveCard(energyCardObject);
            Destroy(energyCardObject);

            // Finaliza o uso da carta "Overload" original
            var sourceCardData = sourceSkillCardObject.GetComponent<Card3D>().GetCardData();
            FinalizeSkillCardPlay(true, sourceSkillCardObject, sourceCardData, actionState);
        }
        else
        {
             ServiceLocator.Audio.PlayConditionFailSound();
        }

        // Sai do modo de equipar energia
        ExitEnergyEquipMode();
    }
    #endregion

    #region 3D Card Management
    private void CreateCard3D(CardData card, Hand3DContainer hand3DContainer, bool isPlayer1)
    {
        if (card3DPrefab == null || hand3DContainer == null) return;
        GameObject card3DObject = Instantiate(card3DPrefab, hand3DContainer.transform.position, hand3DContainer.transform.rotation);
        card3DObject.name = $"Card3D_{card.cardName}_{card.cardType}_{(isPlayer1 ? "P1" : "P2")}";
        Card3D card3D = card3DObject.GetComponent<Card3D>();
        if (card3D != null)
        {
            card3D.SetMaterials(
                actionMaterial, utilityMaterial, auraMaterial,
                skillMaterial, strategyMaterial, freelancerMaterial,
                borderMaterial, backMaterial
            );
            card3D.Setup(card, this);
            card3DMap[card3DObject] = card3D;
        }
        hand3DContainer.AddCard(card3DObject);
    }

    public void RefreshPlayerHand3D(bool isPlayer1)
    {
        var targetHand = isPlayer1 ? player1Hand : player2Hand;
        var targetHand3D = isPlayer1 ? player1Hand3D : player2Hand3D;
        List<GameObject> cardsToRemove = new List<GameObject>(targetHand3D.GetCards());
        foreach (var cardObj in cardsToRemove)
            card3DMap.Remove(cardObj);
        targetHand3D.ClearAllCards();
        foreach (var cardData in targetHand)
            CreateCard3D(cardData, targetHand3D, isPlayer1);
        OnHandRefreshed?.Invoke(isPlayer1);
    }

    public void SetHandVisibility(bool isPlayer1Turn)
    {
        if (player1Hand3D != null) player1Hand3D.SetVisibility(isPlayer1Turn);
        if (player2Hand3D != null) player2Hand3D.SetVisibility(!isPlayer1Turn);
    }

    public void SetAllHandsVisibility(bool showHands)
    {
        if (player1Hand3D != null)
            player1Hand3D.SetVisibility(showHands && ServiceLocator.Game.IsPlayer1Turn());
        if (player2Hand3D != null)
            player2Hand3D.SetVisibility(showHands && !ServiceLocator.Game.IsPlayer1Turn());
    }
    #endregion

    #region Utility Methods
    public bool IsInEquipMode() => isInEquipMode;
     public bool IsInEnergyEquipMode()
    {
        return isEnergyEquipMode;
    }

    public bool IsInAllySelectionMode()
    {
        return isAllySelectionMode;
    }
    public CardData GetSelectedCardToEquip() => selectedCardToEquip;
    public int GetHandSize(bool isPlayer1) => isPlayer1 ? player1Hand.Count : player2Hand.Count;
    public List<CardData> GetPlayerHand(bool isPlayer1) => new List<CardData>(isPlayer1 ? player1Hand : player2Hand);
    public bool AreFreelancerCardsInitialized() => freelancerCardsInitialized;
    public FreelancersUIContainer GetFreelancerHand(bool isPlayer1) => isPlayer1 ? player1FreelancersUI : player2FreelancersUI;
    private bool IsSupportCard(CardType cardType) => cardType == CardType.Skill || cardType == CardType.Strategy;

    public static void ResetStaticData()
    {
        OnCardDrawn = null;
        OnCardEquipped = null;
        OnCardUsed = null;
        OnCardDiscarded = null;
        OnEquipModeEntered = null;
        OnEquipModeExited = null;
        OnHandRefreshed = null;
        OnFreelancerCardHighlighted = null;
        OnFreelancerCardSelectedForSetup = null;
        Debug.Log("<color=gold>[CardManager]</color> Static events cleared for new game session");
    }
    #endregion
}