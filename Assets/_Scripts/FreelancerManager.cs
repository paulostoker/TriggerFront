// _Scripts/FreelancerManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FreelancerManager : MonoBehaviour
{
    #region Fields & Properties
    private List<FreelancerInstance> player1Freelancers = new List<FreelancerInstance>();
    private List<FreelancerInstance> player2Freelancers = new List<FreelancerInstance>();
    public List<Vector2Int> DeathPositions { get; private set; } = new List<Vector2Int>();
    private Dictionary<GameObject, FreelancerInstance> pieceToInstanceMap = new Dictionary<GameObject, FreelancerInstance>();
    private Dictionary<GameObject, FreelancerInstance> cardToInstanceMap = new Dictionary<GameObject, FreelancerInstance>();
    #endregion

    #region Initialization & Registration
    public void CreateFreelancerInstances(List<FreelancerData> p1Deck, List<FreelancerData> p2Deck)
    {
        player1Freelancers.Clear();
        foreach (var opData in p1Deck)
            player1Freelancers.Add(new FreelancerInstance(opData, true));
        player2Freelancers.Clear();
        foreach (var opData in p2Deck)
            player2Freelancers.Add(new FreelancerInstance(opData, false));
        Debug.Log($"<color=green>[FreelancerManager]</color> {player1Freelancers.Count} fichas de operador criadas para P1 e {player2Freelancers.Count} para P2.");
    }

    public void RegisterPieceInstance(GameObject piece, FreelancerData opData, bool isPlayer1)
    {
        var freelancerList = isPlayer1 ? player1Freelancers : player2Freelancers;
        FreelancerInstance instance = freelancerList.FirstOrDefault(inst => inst.BaseData == opData && inst.PieceGameObject == null);
        if (instance != null)
        {
            instance.PieceGameObject = piece;
            pieceToInstanceMap[piece] = instance;
        }
        else
        {
            Debug.LogError($"[FreelancerManager] Não foi possível encontrar uma FreelancerInstance vaga para registrar a peça de '{opData.name}'!");
        }
    }

    public void RegisterCardInstance(GameObject card, FreelancerData opData, bool isPlayer1)
    {
        var freelancerList = isPlayer1 ? player1Freelancers : player2Freelancers;
        FreelancerInstance instance = freelancerList.FirstOrDefault(inst => inst.BaseData == opData);
        if (instance != null)
        {
            instance.CardGameObject = card;
            cardToInstanceMap[card] = instance;
        }
        else
        {
            Debug.LogError($"[FreelancerManager] Não foi possível encontrar uma FreelancerInstance para registrar a carta de '{opData.name}'!");
        }
    }

    public void FullReset()
    {
        Debug.Log("<color=green>[FreelancerManager]</color> Executando reset completo para nova sessão...");
        player1Freelancers.Clear();
        player2Freelancers.Clear();
        pieceToInstanceMap.Clear();
        cardToInstanceMap.Clear();
        DeathPositions.Clear();
        Debug.Log("<color=green>[FreelancerManager]</color> Reset completo finalizado");
    }
    #endregion

    #region Core Management Methods
    public FreelancerInstance GetFreelancerInstance(GameObject piece)
    {
        pieceToInstanceMap.TryGetValue(piece, out FreelancerInstance instance);
        return instance;
    }

    public FreelancerInstance GetFreelancerInstanceByCard(GameObject cardObject)
    {
        cardToInstanceMap.TryGetValue(cardObject, out FreelancerInstance instance);
        return instance;
    }

    public void TakeDamage(GameObject piece, int damage)
    {
        FreelancerInstance instance = GetFreelancerInstance(piece);
        if (instance != null && instance.IsAlive)
        {
            instance.CurrentHP -= damage;
            if (instance.CurrentHP <= 0)
            {
                instance.CurrentHP = 0;
                instance.IsAlive = false;
                Tile deadTile = ServiceLocator.Grid.GetTileUnderPiece(piece);
                if (deadTile != null)
                    DeathPositions.Add(deadTile.GetGridPosition());
                ServiceLocator.Bomb.CancelDefuseIfDefuserDied(piece);
                if (instance.isBombCarrier)
                {
                    if (deadTile != null)
                        ServiceLocator.Bomb.DropBomb(deadTile);
                }
                if (ServiceLocator.Cards != null)
                    ServiceLocator.Cards.NotifyFreelancerDied(instance);
                ServiceLocator.Pieces.HandlePieceDeath(piece, damage);
            }
            else
            {
                PieceDisplay display = piece.GetComponentInChildren<PieceDisplay>();
                if (display != null)
                {
                    display.ShowDamagePopup(damage);
                    display.UpdateHP(instance.CurrentHP, instance.BaseData.HP);
                }
            }
        }
    }

    public void UpdatePieceReference(GameObject oldPiece, GameObject newPiece)
    {
        if (pieceToInstanceMap.TryGetValue(oldPiece, out FreelancerInstance instance))
        {
            pieceToInstanceMap.Remove(oldPiece);
            instance.PieceGameObject = newPiece;
            pieceToInstanceMap[newPiece] = instance;
            Debug.Log($"<color=green>[FreelancerManager]</color> Referência da peça de '{instance.BaseData.name}' atualizada para o novo GameObject '{newPiece.name}'.");
        }
    }

    public void UpdateOffAngleState(GameObject piece)
    {
        FreelancerInstance instance = GetFreelancerInstance(piece);
        if (instance == null) return;
        bool oldState = instance.IsInOffAngleState;
        bool hasCard = instance.ActiveEffects.Any(eff =>
            eff.Card is SupportData sd && sd.modifiers.Any(m => m.type == ModifierType.HasOffAngleCardActive));
        Tile currentTile = ServiceLocator.Grid.GetTileUnderPiece(piece);
        ObstacleProperties obstacle = currentTile?.GetObstacle();
        bool isOnBox = (obstacle != null && obstacle.obstacleType == ObstacleType.Box);
        bool newState = hasCard && isOnBox;
        instance.IsInOffAngleState = newState;
        if (oldState && !newState && hasCard)
        {
            ActiveEffect effectToRemove = instance.ActiveEffects.FirstOrDefault(eff =>
                eff.Card is SupportData sd && sd.modifiers.Any(m => m.type == ModifierType.HasOffAngleCardActive));
            if (effectToRemove != null)
            {
                instance.ActiveEffects.Remove(effectToRemove);
                Debug.Log($"<color=orange>[Off-Angle]</color> Efeito da carta removido de '{instance.BaseData.name}' por ter saído do Box.");
            }
        }
    }
    public void ModifyActionCharges(GameObject piece, int amount)
    {
        FreelancerInstance instance = GetFreelancerInstance(piece);
        if (instance != null)
        {
            instance.ActionCharges += amount;
            Debug.Log($"<color=yellow>[Action Charges]</color> {instance.BaseData.name} teve suas cargas alteradas em {amount}. Total agora: {instance.ActionCharges}");
        }
    }
    #endregion

    #region Energy Management

    // --- NOVO MÉTODO ADICIONADO ---
    public bool HasEnergyForAction(GameObject piece, ActionCost cost)
    {
        if (piece == null) return false;
        FreelancerInstance instance = GetFreelancerInstance(piece);
        if (instance == null) return false;

        int actionEnergy = GetEnergyCount(piece, CardType.Action);
        int utilityEnergy = GetEnergyCount(piece, CardType.Utility);
        int auraEnergy = GetEnergyCount(piece, CardType.Aura);

        return actionEnergy >= cost.action &&
               utilityEnergy >= cost.utility &&
               auraEnergy >= cost.aura;
    }

    public bool TransferEnergy(GameObject source, GameObject destination, CardData energyCard)
    {
        var sourceInstance = GetFreelancerInstance(source);
        var destInstance = GetFreelancerInstance(destination);
        if (sourceInstance == null || destInstance == null || energyCard == null) return false;
        if (!sourceInstance.EquippedEnergies.Contains(energyCard)) return false;
        sourceInstance.EquippedEnergies.Remove(energyCard);
        destInstance.EquippedEnergies.Add(energyCard);
        Debug.Log($"[FreelancerManager] Energia '{energyCard.cardName}' transferida de '{source.name}' para '{destination.name}'.");
        UpdateEcoStatus(sourceInstance);
        UpdateEcoStatus(destInstance);
        ServiceLocator.Pieces.UpdatePieceEnergyUI(source);
        ServiceLocator.Pieces.UpdatePieceEnergyUI(destination);
        int sourceAction = GetEnergyCount(source, CardType.Action);
        int sourceUtility = GetEnergyCount(source, CardType.Utility);
        int sourceAura = GetEnergyCount(source, CardType.Aura);
        ServiceLocator.Cards.UpdateFreelancerCardEnergy(sourceInstance.BaseData, sourceInstance.IsPlayer1, sourceAction, sourceUtility, sourceAura);
        int destAction = GetEnergyCount(destination, CardType.Action);
        int destUtility = GetEnergyCount(destination, CardType.Utility);
        int destAura = GetEnergyCount(destination, CardType.Aura);
        ServiceLocator.Cards.UpdateFreelancerCardEnergy(destInstance.BaseData, destInstance.IsPlayer1, destAction, destUtility, destAura);
        return true;
    }

    public bool EquipEnergy(GameObject piece, CardData card)
    {
        if (!GameConfig.Instance.allowMultipleEquipsPerTurn)
        {
            if (ServiceLocator.Game.CurrentState is PreparationState prepState)
            {
                if (prepState.HasPieceBeenEquipped(piece))
                {
                    Debug.LogWarning($"<color=orange>[FreelancerManager]</color> Tentativa de equipar peça ({piece.name}) que já equipou neste turno. Ação bloqueada.");
                    return false;
                }
                prepState.RegisterEquippedPiece(piece);
            }
        }
        FreelancerInstance instance = GetFreelancerInstance(piece);
        if (instance != null)
        {
            instance.EquippedEnergies.Add(card);
            ServiceLocator.Pieces.UpdatePieceEnergyUI(piece);
            UpdateEcoStatus(instance);
            int action = GetEnergyCount(piece, CardType.Action);
            int utility = GetEnergyCount(piece, CardType.Utility);
            int aura = GetEnergyCount(piece, CardType.Aura);
            ServiceLocator.Cards.UpdateFreelancerCardEnergy(instance.BaseData, instance.IsPlayer1, action, utility, aura);
            if (ServiceLocator.Audio != null)
                ServiceLocator.Audio.PlayEquipCardSound();
            return true;
        }
        return false;
    }

    private void UpdateEcoStatus(FreelancerInstance instance)
    {
        if (instance == null) return;
        int requiredEnergy = instance.BaseData.weaponCost.action;
        int currentEnergy = instance.EquippedEnergies.Count(card => card.cardType == CardType.Action);
        instance.IsInEcoMode = currentEnergy < requiredEnergy;
    }

    public bool IsInEcoMode(GameObject piece)
    {
        FreelancerInstance instance = GetFreelancerInstance(piece);
        return instance != null ? instance.IsInEcoMode : true;
    }

    public int GetEnergyCount(GameObject piece, CardType type)
    {
        FreelancerInstance instance = GetFreelancerInstance(piece);
        if (instance != null)
            return instance.EquippedEnergies.Count(card => card.cardType == type);
        return 0;
    }

    public void ConsumeEnergyForTechnique(GameObject piece, ActionCost cost)
    {
        if (piece == null) return;
        FreelancerInstance instance = GetFreelancerInstance(piece);
        if (instance == null) return;

        // Consome a quantidade exata de energias de Utilidade
        for (int i = 0; i < cost.utility; i++)
        {
            CardData utilityCardToRemove = instance.EquippedEnergies.FirstOrDefault(c => c.cardType == CardType.Utility);
            if (utilityCardToRemove != null)
            {
                instance.EquippedEnergies.Remove(utilityCardToRemove);
                ServiceLocator.Decks.AddToDiscard(utilityCardToRemove, instance.IsPlayer1); // <-- ADICIONADO
            }
        }

        // Consome a quantidade exata de energias de Aura
        for (int i = 0; i < cost.aura; i++)
        {
            CardData auraCardToRemove = instance.EquippedEnergies.FirstOrDefault(c => c.cardType == CardType.Aura);
            if (auraCardToRemove != null)
            {
                instance.EquippedEnergies.Remove(auraCardToRemove);
                ServiceLocator.Decks.AddToDiscard(auraCardToRemove, instance.IsPlayer1); // <-- ADICIONADO
            }
        }

        // Atualiza a UI para refletir as energias gastas
        ServiceLocator.Pieces.UpdatePieceEnergyUI(piece);
        int action = GetEnergyCount(piece, CardType.Action);
        int utility = GetEnergyCount(piece, CardType.Utility);
        int aura = GetEnergyCount(piece, CardType.Aura);
        ServiceLocator.Cards.UpdateFreelancerCardEnergy(instance.BaseData, instance.IsPlayer1, action, utility, aura);
        
        UpdateEcoStatus(instance);
    }

    public void SetStoredDiceResult(GameObject piece, int result)
    {
        FreelancerInstance instance = GetFreelancerInstance(piece);
        if (instance != null)
        {
            instance.StoredDiceResult = result;
            Debug.Log($"<color=cyan>[Pre-Fire]</color> Resultado de dado {result} armazenado para {instance.BaseData.name}.");
        }
    }

    public void ClearStoredDiceResult(GameObject piece)
    {
        FreelancerInstance instance = GetFreelancerInstance(piece);
        if (instance != null && instance.StoredDiceResult.HasValue)
        {
            instance.StoredDiceResult = null;
            Debug.Log($"<color=cyan>[Pre-Fire]</color> Resultado de dado armazenado foi limpo para {instance.BaseData.name}.");
        }
    }
    #endregion

    #region State Management
    public bool IsAlive(GameObject piece)
    {
        FreelancerInstance instance = GetFreelancerInstance(piece);
        return instance != null ? instance.IsAlive : false;
    }

    public int GetCurrentHP(GameObject piece)
    {
        FreelancerInstance instance = GetFreelancerInstance(piece);
        return instance != null ? instance.CurrentHP : 0;
    }

    public FreelancerData GetFreelancerData(GameObject piece)
    {
        FreelancerInstance instance = GetFreelancerInstance(piece);
        return instance?.BaseData;
    }

    public bool AreAllFreelancersDeadOnTeam(bool isPlayer1)
    {
        var freelancerList = isPlayer1 ? player1Freelancers : player2Freelancers;
        if (freelancerList.Count == 0) return true;
        return freelancerList.All(op => !op.IsAlive);
    }

    public int GetAlivePiecesCount(bool isPlayer1)
    {
        var freelancerList = isPlayer1 ? player1Freelancers : player2Freelancers;
        return freelancerList.Count(op => op.IsAlive);
    }

    public List<FreelancerInstance> GetAllFreelancerInstances()
    {
        var allFreelancers = new List<FreelancerInstance>(player1Freelancers);
        allFreelancers.AddRange(player2Freelancers);
        return allFreelancers;
    }
    #endregion

    #region Turn State Management
    public void ResetAllTeamTurnStates(bool isPlayer1Team)
    {
        var teamFreelancers = isPlayer1Team ? player1Freelancers : player2Freelancers;
        string teamName = isPlayer1Team ? "Player 1" : "Player 2";
        int resetCount = 0;
        int aliveCount = 0;

        Debug.Log($"<color=cyan>[FreelancerManager TEAM RESET]</color> Iniciando reset completo para {teamName} ({teamFreelancers.Count} freelancers)");

        foreach (var instance in teamFreelancers)
        {
            if (instance != null)
            {
                bool wasAlive = instance.IsAlive;

                bool needsReset = instance.HasMovedThisTurn || instance.HasActedThisTurn || instance.HasUsedSkillThisTurn;

                instance.HasMovedThisTurn = false;
                instance.HasActedThisTurn = false;
                instance.HasUsedSkillThisTurn = false;

                resetCount++;
                if (wasAlive) aliveCount++;

                if (needsReset)
                {
                    Debug.Log($"<color=yellow>[TEAM RESET]</color> {instance.BaseData.name} tinha estados ativos - resetado");
                }
            }
        }

        Debug.Log($"<color=green>[FreelancerManager TEAM RESET]</color> {teamName}: {resetCount} freelancers resetados ({aliveCount} vivos)");
    }
    public void ResetAllActionChargesForTeam(bool isPlayer1Team)
    {
        var teamFreelancers = isPlayer1Team ? player1Freelancers : player2Freelancers;
        foreach (var instance in teamFreelancers)
        {
            if (instance != null && instance.IsAlive)
            {
                instance.ActionCharges = 1;
            }
        }
        Debug.Log($"<color=yellow>[Action Charges]</color> Cargas de ação resetadas para o time {(isPlayer1Team ? "Player 1" : "Player 2")}.");
    }
    public void ResetTurnState(GameObject piece)
    {
        FreelancerInstance instance = GetFreelancerInstance(piece);
        if (instance != null)
        {
            instance.HasMovedThisTurn = false;
            instance.HasActedThisTurn = false;
            instance.HasUsedSkillThisTurn = false;
        }
        else
        {
            Debug.LogError($"<color=red>[FreelancerManager RESET ERROR]</color> Não foi possível encontrar FreelancerInstance para '{piece.name}'!");
        }
    }

    public void SetFreelancerMoved(GameObject piece)
    {
        FreelancerInstance instance = GetFreelancerInstance(piece);
        if (instance != null)
        {
            instance.HasMovedThisTurn = true;
            Debug.Log($"<color=cyan>[FreelancerManager]</color> {instance.BaseData.name} marcado como movido.");
        }
        else
        {
            Debug.LogError($"<color=red>[FreelancerManager ERROR]</color> Tentativa de marcar movimento em peça sem FreelancerInstance: {piece.name}");
        }
    }
        public void SetActionCharges(GameObject piece, int value)
    {
        FreelancerInstance instance = GetFreelancerInstance(piece);
        if (instance != null)
        {
            instance.ActionCharges = Mathf.Max(0, value);
            Debug.Log($"<color=yellow>[Action Charges]</color> Cargas de {instance.BaseData.name} definidas para {instance.ActionCharges}.");
        }
    }
    public void ConsumeActionCharge(GameObject piece)
    {
        FreelancerInstance instance = GetFreelancerInstance(piece);
        if (instance != null && instance.ActionCharges > 0)
        {
            instance.ActionCharges--;
            Debug.Log($"<color=red>[Action Charges]</color> {instance.BaseData.name} consumiu uma carga de ação. Restantes: {instance.ActionCharges}");
        }
        
        if (instance.ActionCharges <= 0)
        {
            instance.HasActedThisTurn = true;
        }
    }

    public void SetFreelancerUsedSkill(GameObject piece)
    {
        FreelancerInstance instance = GetFreelancerInstance(piece);
        if (instance != null)
        {
            if (GameConfig.Instance.allowMultipleSkillsPerTurn)
            {
                Debug.Log($"<color=magenta>[FreelancerManager]</color> {instance.BaseData.name} usou skill (múltiplas skills permitidas).");
            }
            else
            {
                instance.HasUsedSkillThisTurn = true;
                Debug.Log($"<color=magenta>[FreelancerManager]</color> {instance.BaseData.name} marcado como tendo usado skill.");
            }
        }
        else
        {
            Debug.LogError($"<color=red>[FreelancerManager ERROR]</color> Tentativa de marcar skill em peça sem FreelancerInstance: {piece.name}");
        }
    }

    public bool CanFreelancerMove(GameObject piece)
    {
        FreelancerInstance instance = GetFreelancerInstance(piece);
        if (instance == null)
        {
            Debug.LogError($"<color=red>[FreelancerManager]</color> CanFreelancerMove: FreelancerInstance não encontrada para {piece.name}");
            return false;
        }
        
        if (!instance.IsAlive) 
        {
            Debug.Log($"<color=gray>[FreelancerManager]</color> {instance.BaseData.name} não pode se mover - morto");
            return false;
        }
        
        bool canMove = !instance.HasMovedThisTurn;
        Debug.Log($"<color=cyan>[CAN MOVE CHECK]</color> {instance.BaseData.name}: HasMovedThisTurn={instance.HasMovedThisTurn} → CanMove={canMove}");
        return canMove;
    }
    public bool CanFreelancerAct(GameObject piece)
    {
        FreelancerInstance instance = GetFreelancerInstance(piece);
        if (instance == null) return false;
        
        if (!instance.IsAlive) return false;
        if (ServiceLocator.Effects.IsActionForbidden(piece, ModifierType.ForbidAttack))
            return false;
        
        return instance.ActionCharges > 0;
    }

    public bool CanFreelancerUseSkill(GameObject piece)
    {
        FreelancerInstance instance = GetFreelancerInstance(piece);
        if (instance == null) return false;
        if (!instance.IsAlive) return false;
        
        if (GameConfig.Instance.allowMultipleSkillsPerTurn) return true;
        
        bool canUseSkill = !instance.HasUsedSkillThisTurn;
        Debug.Log($"<color=magenta>[CAN SKILL CHECK]</color> {instance.BaseData.name}: HasUsedSkillThisTurn={instance.HasUsedSkillThisTurn} → CanUseSkill={canUseSkill}");
        return canUseSkill;
    }

    public bool HasFreelancerCompletedTurn(GameObject piece)
    {
        FreelancerInstance instance = GetFreelancerInstance(piece);
        if (instance == null) return true;
        
        bool completed = instance.HasMovedThisTurn && instance.HasActedThisTurn;
        Debug.Log($"<color=purple>[TURN COMPLETE CHECK]</color> {instance.BaseData.name}: Moved={instance.HasMovedThisTurn}, Acted={instance.HasActedThisTurn} → Complete={completed}");
        return completed;
    }
    public void ValidateAllTeamStates(bool isPlayer1Team)
    {
        var teamFreelancers = isPlayer1Team ? player1Freelancers : player2Freelancers;
        string teamName = isPlayer1Team ? "Player 1" : "Player 2";
        
        Debug.Log($"<color=magenta>[VALIDATION]</color> === VALIDAÇÃO DE ESTADOS - {teamName} ===");
        
        for (int i = 0; i < teamFreelancers.Count; i++)
        {
            var instance = teamFreelancers[i];
            if (instance != null && instance.IsAlive)
            {
                bool canMove = CanFreelancerMove(instance.PieceGameObject);
                bool canAct = CanFreelancerAct(instance.PieceGameObject);
                bool canSkill = CanFreelancerUseSkill(instance.PieceGameObject);
                
                string status = $"#{i+1} {instance.BaseData.name}: M:{canMove}/A:{canAct}/S:{canSkill}";
                
                if (canMove && canAct && canSkill)
                {
                    Debug.Log($"<color=green>[VALIDATION OK]</color> {status}");
                }
                else
                {
                    Debug.LogWarning($"<color=red>[VALIDATION FAIL]</color> {status} - Estados bloqueados detectados!");
                }
            }
        }
        
        Debug.Log($"<color=magenta>[VALIDATION]</color> === FIM DA VALIDAÇÃO ===");
    }
    #endregion
}