// _Scripts/Effects/EffectManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EffectManager : MonoBehaviour
{
    private List<SpawnedEffect> activeSpawnedEffects = new List<SpawnedEffect>();

    #region Event Management
    private void OnEnable()
    {
        PieceManager.OnAnyPieceKilled += HandleAnyPieceKilled;
    }

    private void OnDisable()
    {
        PieceManager.OnAnyPieceKilled -= HandleAnyPieceKilled;
    }

    private void HandleAnyPieceKilled()
    {
        var allFreelancers = ServiceLocator.Freelancers.GetAllFreelancerInstances();
        foreach (var opInstance in allFreelancers)
        {
            if (opInstance.IsAlive && opInstance.PieceGameObject != null)
            {
                CleanUpActionEffects(opInstance.PieceGameObject, ActionType.OnAnyKill);
                CleanUpInvalidPassiveEffects(opInstance.PieceGameObject);
            }
        }
    }
    #endregion

    #region Core Effect Logic
    public bool ApplyEffect(GameObject target, CardData card, GameObject source)
    {
        // --- DEBUGGER ADICIONADO ---
        Debug.Log($"[DEBUG-IGL] EffectManager.ApplyEffect CALLED: Tentando aplicar '{card.cardName}' no alvo '{target.name}'.");

        if (target == null || card == null) return false;

        if (card is SupportData supportCard)
        {
            FreelancerInstance targetInstance = ServiceLocator.Freelancers.GetFreelancerInstance(target);
            if (targetInstance == null) return false;

            if (!supportCard.canStack)
            {
                if (targetInstance.ActiveEffects.Any(activeEffect => activeEffect.Card.cardName == supportCard.cardName))
                {
                    Debug.Log($"<color=orange>[EffectManager]</color> Falha ao aplicar '{supportCard.cardName}'. O efeito não pode ser empilhado (canStack=false) e já está ativo em '{target.name}'.");
                    return false;
                }
            }

            foreach (var condition in supportCard.conditions)
            {
                if (condition != null && !condition.isPassiveCondition && !condition.Check(source, target))
                {
                    Debug.Log($"<color=orange>[EffectManager]</color> Condição de ativação '{condition.type}' não satisfeita para a carta '{card.cardName}'. A CARTA NÃO FOI USADA.");
                    return false;
                }
            }
            
            var newActiveEffect = new ActiveEffect(card, source);
            targetInstance.ActiveEffects.Add(newActiveEffect);
            Debug.Log($"<color=green>[EffectManager]</color> Efeito da carta '{card.cardName}' aplicado a '{target.name}'.");
            ServiceLocator.Pieces.UpdatePieceStatusUI(target);
            return true;
        }
        
        return false;
    }
    public bool FindAndConsumeEffect(GameObject piece, ModifierType effectType)
    {
        if (piece == null) return false;
        var instance = ServiceLocator.Freelancers.GetFreelancerInstance(piece);
        if (instance == null || instance.ActiveEffects.Count == 0) return false;

        var effectToConsume = instance.ActiveEffects.FirstOrDefault(effect =>
            effect.Card is SupportData sd &&
            sd.modifiers.Any(m => m.type == effectType)
        );

        if (effectToConsume != null)
        {
            // ANTES de remover o efeito, enviamos sua carta para o descarte.
            DiscardEffectCard(effectToConsume, instance);
            
            instance.ActiveEffects.Remove(effectToConsume);
            Debug.Log($"<color=cyan>[EffectManager]</color> Efeito '{effectType}' consumido para {piece.name}.");
            ServiceLocator.Pieces.UpdatePieceStatusUI(piece);
            return true;
        }
        return false;
    }

    public void CleanUpActionEffects(GameObject target, ActionType actionTaken)
    {
        if (target == null) return;
        FreelancerInstance targetInstance = ServiceLocator.Freelancers.GetFreelancerInstance(target);
        if (targetInstance == null || targetInstance.ActiveEffects.Count == 0) return;
        
        List<ActiveEffect> effectsToRemove = new List<ActiveEffect>();
        foreach (var activeEffect in targetInstance.ActiveEffects)
        {
            if (activeEffect.Card is SupportData supportCard)
            {
                // A consumedBy Any não deve ser confundida com a Any de OnAnyKill, etc.
                // A flag Any aqui significa que qualquer uma das ações (Ataque, Movimento, etc) vai consumir o efeito.
                bool isConsumedBySpecificAction = (supportCard.modifiers.Any(m => (m.consumedBy & actionTaken) != 0));
                bool isConsumedByAnyAction = (supportCard.modifiers.Any(m => (m.consumedBy & ActionType.Any) != 0)) && (actionTaken != ActionType.OnAnyKill && actionTaken != ActionType.StartDefuse);

                if (isConsumedBySpecificAction || isConsumedByAnyAction)
                {
                    effectsToRemove.Add(activeEffect);
                }
            }
        }
        
        if (effectsToRemove.Count > 0)
        {
            foreach (var effect in effectsToRemove)
            {
                // ANTES de remover o efeito, enviamos sua carta para o descarte.
                DiscardEffectCard(effect, targetInstance);
                targetInstance.ActiveEffects.Remove(effect);
            }
            Debug.Log($"<color=cyan>[EffectManager]</color> Limpou {effectsToRemove.Count} efeito(s) de '{actionTaken}' de {target.name}.");
            ServiceLocator.Pieces.UpdatePieceStatusUI(target);
        }
    }
    
    public void CleanUpInvalidPassiveEffects(GameObject target)
    {
        if (target == null) return;
        FreelancerInstance targetInstance = ServiceLocator.Freelancers.GetFreelancerInstance(target);
        if (targetInstance == null || targetInstance.ActiveEffects.Count == 0) return;
        List<ActiveEffect> effectsToRemove = new List<ActiveEffect>();
        foreach (var activeEffect in targetInstance.ActiveEffects)
        {
            if (activeEffect.Card is SupportData supportCard)
            {
                if (supportCard.conditions.Any(c => c.isPassiveCondition))
                {
                    bool allPassiveConditionsStillMet = true;
                    foreach (var condition in supportCard.conditions)
                    {
                        if (condition.isPassiveCondition && !condition.Check(target, null))
                        {
                            allPassiveConditionsStillMet = false;
                            break;
                        }
                    }
                    if (!allPassiveConditionsStillMet)
                        effectsToRemove.Add(activeEffect);
                }
            }
        }
        if (effectsToRemove.Count > 0)
        {
            foreach (var effect in effectsToRemove)
                targetInstance.ActiveEffects.Remove(effect);
            Debug.Log($"<color=orange>[EffectManager]</color> Removeu {effectsToRemove.Count} efeito(s) passivo(s) de {target.name} pois suas condições não são mais válidas.");
            ServiceLocator.Pieces.UpdatePieceStatusUI(target);
        }
    }
    public void CleanUpSelfTargetingEffects(GameObject source)
    {
        if (source == null) return;
        FreelancerInstance sourceInstance = ServiceLocator.Freelancers.GetFreelancerInstance(source);
        if (sourceInstance == null) return;

        List<ActiveEffect> effectsToRemove = new List<ActiveEffect>();
        foreach (var effect in sourceInstance.ActiveEffects)
        {
            if (effect.Card is SupportData supportCard && effect.Source == source)
            {
                if (supportCard.modifiers.Any(m => m.type == ModifierType.ForbidAttack))
                    effectsToRemove.Add(effect);
            }
        }

        foreach (var effect in effectsToRemove)
            sourceInstance.ActiveEffects.Remove(effect);
    }
    public bool HasEffectOfType(GameObject piece, ModifierType effectType)
    {
        if (piece == null) return false;
        FreelancerInstance instance = ServiceLocator.Freelancers.GetFreelancerInstance(piece);
        if (instance == null || instance.ActiveEffects.Count == 0) return false;

        // Verifica se qualquer efeito ativo na peça contém um modificador do tipo especificado
        return instance.ActiveEffects.Any(effect =>
            effect.Card is SupportData supportData &&
            supportData.modifiers.Any(modifier => modifier.type == effectType)
        );
    }
    
      public void ExecuteStrategyModifier(EffectModifier modifier, bool isPlayer1)
    {
        if (modifier == null) return;

        switch (modifier.type)
        {
            case ModifierType.DrawCards:
                if (modifier.value > 0)
                {
                    ServiceLocator.Game.DrawCardsForCurrentPlayer(modifier.value);
                }
                break;
            
            // O case para SearchDeckForType foi REMOVIDO daqui, pois essa lógica
            // agora é tratada pelo CardManager com base na ModifierLogic.
            
            default:
                Debug.LogWarning($"[EffectManager] A lógica para o modificador de estratégia '{modifier.type}' ainda não foi implementada.");
                break;
        }
    }

    #endregion

    #region Duration Management
    public void ProcessEffectDurations()
    {
        Debug.Log("<color=cyan>[EffectManager]</color> Processando durações de efeitos no final do turno.");
        
        var allFreelancers = ServiceLocator.Freelancers.GetAllFreelancerInstances();
        List<GameObject> piecesToUpdateUI = new List<GameObject>();
        
        foreach (var instance in allFreelancers)
        {
            if (!instance.IsAlive || instance.ActiveEffects.Count == 0) continue;
            
            List<ActiveEffect> effectsToRemove = new List<ActiveEffect>();
            foreach (var effect in instance.ActiveEffects)
            {
                if (effect.Card is SupportData sd && sd.duration < 99)
                {
                    effect.TurnsRemaining--;
                }
                if (effect.TurnsRemaining <= 0)
                {
                    effectsToRemove.Add(effect);
                }
            }

            if (effectsToRemove.Count > 0)
            {
                foreach (var effect in effectsToRemove)
                {
                    // ANTES de remover o efeito, enviamos sua carta para o descarte.
                    DiscardEffectCard(effect, instance);
                    instance.ActiveEffects.Remove(effect);
                    Debug.Log($"<color=cyan>[EffectManager]</color> Efeito '{effect.Card.cardName}' expirou para {instance.BaseData.name}.");
                }
                if (instance.PieceGameObject != null)
                {
                    piecesToUpdateUI.Add(instance.PieceGameObject);
                }
            }
        }
        
        foreach (var piece in piecesToUpdateUI)
        {
            ServiceLocator.Pieces.UpdatePieceStatusUI(piece);
        }

        // --- Lógica para Efeitos Spawnados (permanece igual) ---
        List<SpawnedEffect> expiredSpawnedEffects = new List<SpawnedEffect>();
        foreach (var spawnedEffect in activeSpawnedEffects)
        {
            if (spawnedEffect != null)
            {
                spawnedEffect.turnsRemaining--;
                if (spawnedEffect.turnsRemaining <= 0)
                {
                    expiredSpawnedEffects.Add(spawnedEffect);
                }
            }
        }

        foreach (var expiredEffect in expiredSpawnedEffects)
        {
            activeSpawnedEffects.Remove(expiredEffect);
            Debug.Log($"<color=cyan>[EffectManager]</color> Efeito spawnado '{expiredEffect.name}' expirou e foi removido.");
            Destroy(expiredEffect.gameObject);
        }
    }

    public void ProcessSingleFreelancerDurations(GameObject piece)
    {
        if (piece == null) return;
        var instance = ServiceLocator.Freelancers.GetFreelancerInstance(piece);
        if (instance == null || !instance.IsAlive || instance.ActiveEffects.Count == 0) return;
        List<ActiveEffect> effectsToRemove = new List<ActiveEffect>();
        foreach (var effect in instance.ActiveEffects)
        {
            if (effect.Card is SupportData sd && sd.duration <= 0)
                effectsToRemove.Add(effect);
        }
        if (effectsToRemove.Count > 0)
        {
            foreach (var effect in effectsToRemove)
            {
                instance.ActiveEffects.Remove(effect);
                Debug.Log($"<color=cyan>[EffectManager]</color> Efeito de duração zero '{effect.Card.cardName}' expirou para {instance.BaseData.name}.");
            }
            ServiceLocator.Pieces.UpdatePieceStatusUI(piece);
        }
    }
private void DiscardEffectCard(ActiveEffect effect, FreelancerInstance instance)
    {
        if (effect.Card is SupportData) // Garante que apenas cartas de Suporte (Skill/Strategy) sejam descartadas aqui.
        {
            ServiceLocator.Decks.AddToDiscard(effect.Card, instance.IsPlayer1);
        }
    }

    #endregion

    #region Damage & Combat Effects

    public bool FindAndConsumeCounterAttackEffect(GameObject piece)
    {
        if (piece == null) return false;

        FreelancerInstance instance = ServiceLocator.Freelancers.GetFreelancerInstance(piece);
        if (instance == null || instance.ActiveEffects.Count == 0) return false;

        ActiveEffect counterEffect = instance.ActiveEffects.FirstOrDefault(effect =>
            effect.Card is SupportData supportData &&
            supportData.modifiers.Any(modifier => modifier.type == ModifierType.CanCounterAttack)
        );

        if (counterEffect != null)
        {
            instance.ActiveEffects.Remove(counterEffect);
            Debug.Log($"<color=orange>[EffectManager]</color> Efeito 'Counter' consumido para {piece.name}.");
            ServiceLocator.Pieces.UpdatePieceStatusUI(piece);
            return true;
        }

        return false;
    }
    public bool TryGetDamageOverride(GameObject attacker, out int overrideDamage)
    {
        overrideDamage = 0;
        if (attacker == null) return false;
        FreelancerInstance targetInstance = ServiceLocator.Freelancers.GetFreelancerInstance(attacker);
        if (targetInstance == null || targetInstance.ActiveEffects.Count == 0) return false;
        foreach (var activeEffect in targetInstance.ActiveEffects)
        {
            if (activeEffect.Card is SupportData supportCard)
            {
                bool allPassiveConditionsMet = true;
                foreach (var condition in supportCard.conditions)
                {
                    if (condition.isPassiveCondition && !condition.Check(attacker, null))
                    {
                        allPassiveConditionsMet = false;
                        break;
                    }
                }
                if (allPassiveConditionsMet)
                {
                    foreach (var modifier in supportCard.modifiers)
                    {
                        if (modifier.logic == ModifierLogic.SetDamage)
                        {
                            if (ServiceLocator.Freelancers.IsInEcoMode(attacker))
                            {
                                overrideDamage = 50;
                            }
                            else
                            {
                                FreelancerData attackerData = ServiceLocator.Freelancers.GetFreelancerData(attacker);
                                if (attackerData != null)
                                    overrideDamage = attackerData.weaponStats.damage;
                            }
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    public int ApplyResultMapModifiers(GameObject target, int initialRoll)
    {
        if (target == null) return initialRoll;
        FreelancerInstance targetInstance = ServiceLocator.Freelancers.GetFreelancerInstance(target);
        if (targetInstance == null || targetInstance.ActiveEffects.Count == 0) return initialRoll;
        int finalRoll = initialRoll;
        foreach (var activeEffect in targetInstance.ActiveEffects)
        {
            if (activeEffect.Card is SupportData supportCard)
            {
                foreach (var modifier in supportCard.modifiers)
                {
                    if (modifier.logic == ModifierLogic.ResultMap && initialRoll >= modifier.minRoll && initialRoll <= modifier.maxRoll)
                    {
                        finalRoll = modifier.newResult;
                        return finalRoll;
                    }
                }
            }
        }
        return finalRoll;
    }
    #endregion

    #region Triggered Effects
    public void ProcessTriggeredEffects(GameObject source, ActionType trigger)
    {
        if (source == null) return;
        var sourceInstance = ServiceLocator.Freelancers.GetFreelancerInstance(source);
        if (sourceInstance == null || sourceInstance.ActiveEffects.Count == 0) return;
        var effectsToCheck = new List<ActiveEffect>(sourceInstance.ActiveEffects);
        List<ActiveEffect> triggeredEffectsToRemove = new List<ActiveEffect>();
        foreach (var activeEffect in effectsToCheck)
        {
            if (activeEffect.Card is SupportData supportCard)
            {
                foreach (var modifier in supportCard.modifiers)
                {
                    if (modifier.logic == ModifierLogic.ApplyEffect && (modifier.triggerOn & trigger) != 0)
                    {
                        List<GameObject> targets = new List<GameObject>();
                        switch (modifier.target)
                        {
                            case EffectTarget.Self:
                                targets.Add(source);
                                break;
                            case EffectTarget.AllAllies:
                                targets = ServiceLocator.Pieces.GetPlayerPieces(sourceInstance.IsPlayer1)
                                    .Where(p => p != source && ServiceLocator.Freelancers.IsAlive(p)).ToList();
                                break;
                            case EffectTarget.AllEnemies:
                                targets = ServiceLocator.Pieces.GetPlayerPieces(!sourceInstance.IsPlayer1)
                                    .Where(p => ServiceLocator.Freelancers.IsAlive(p)).ToList();
                                break;
                            case EffectTarget.NextAlly:
                                if (ServiceLocator.Game.CurrentState is ActionState actionState)
                                {
                                    GameObject nextAlly = actionState.GetNextAliveFreelancerInTurnOrder();
                                    if (nextAlly != null) targets.Add(nextAlly);
                                }
                                break;
                        }
                        if (targets.Count > 0 && modifier.effectToApply != null)
                        {
                            foreach (var target in targets)
                                ApplyEffect(target, modifier.effectToApply, source);
                            triggeredEffectsToRemove.Add(activeEffect);
                        }
                    }
                }
            }
        }
        if (triggeredEffectsToRemove.Count > 0)
        {
            foreach (var effectToRemove in triggeredEffectsToRemove)
                sourceInstance.ActiveEffects.Remove(effectToRemove);
            ServiceLocator.Pieces.UpdatePieceStatusUI(source);
        }
    }
 public void RegisterSpawnedEffect(SpawnedEffect effect)
    {
        if (effect != null && !activeSpawnedEffects.Contains(effect))
        {
            activeSpawnedEffects.Add(effect);
        }
    }

    #endregion

    #region Public Getters
    public bool IsActionForbidden(GameObject target, ModifierType type)
    {
        if (target == null) return false;
        FreelancerInstance targetInstance = ServiceLocator.Freelancers.GetFreelancerInstance(target);
        if (targetInstance == null || targetInstance.ActiveEffects.Count == 0) return false;
        foreach (var activeEffect in targetInstance.ActiveEffects)
        {
            if (activeEffect.Card is SupportData supportCard)
            {
                bool allPassiveConditionsMet = true;
                foreach (var condition in supportCard.conditions)
                {
                    if (condition.isPassiveCondition && !condition.Check(target, null))
                    {
                        allPassiveConditionsMet = false;
                        break;
                    }
                }
                if (allPassiveConditionsMet)
                {
                    if (supportCard.modifiers.Any(m => m.type == type))
                        return true;
                }
            }
        }
        return false;
    }

    public List<EffectModifier> GetAllModifiers(GameObject target)
    {
        var allModifiers = new List<EffectModifier>();
        if (target == null) return allModifiers;
        FreelancerInstance targetInstance = ServiceLocator.Freelancers.GetFreelancerInstance(target);
        if (targetInstance == null || targetInstance.ActiveEffects.Count == 0) return allModifiers;
        foreach (var activeEffect in targetInstance.ActiveEffects)
        {
            if (activeEffect.Card is SupportData supportCard)
            {
                bool allPassiveConditionsMet = true;
                foreach (var condition in supportCard.conditions)
                {
                    if (condition.isPassiveCondition && !condition.Check(target, null))
                    {
                        allPassiveConditionsMet = false;
                        break;
                    }
                }
                if (allPassiveConditionsMet)
                    allModifiers.AddRange(supportCard.modifiers);
            }
        }
        return allModifiers;
    }

    public int GetDiceModifier(GameObject target, ModifierType type)
    {
        return GetStatModifier(target, type);
    }
    
    public int GetStatModifier(GameObject target, ModifierType type)
    {
        if (target == null) return 0;
        FreelancerInstance targetInstance = ServiceLocator.Freelancers.GetFreelancerInstance(target);
        if (targetInstance == null || targetInstance.ActiveEffects.Count == 0) return 0;
        int totalModifier = 0;
        foreach (var activeEffect in targetInstance.ActiveEffects)
        {
            if (activeEffect.Card is SupportData supportCard)
            {
                bool allPassiveConditionsMet = true;
                foreach (var condition in supportCard.conditions)
                {
                    if (condition.isPassiveCondition && !condition.Check(target, null))
                    {
                        allPassiveConditionsMet = false;
                        break;
                    }
                }
                if (!allPassiveConditionsMet) continue;
                foreach (var modifier in supportCard.modifiers)
                {
                    if (modifier.logic == ModifierLogic.Additive && modifier.type == type)
                    {
                        Debug.Log($"<color=pink>[GET STAT MODIFIER]</color> Found active modifier '{supportCard.name}' on '{target.name}'. Type: {modifier.type}, Value: {modifier.value}.");
                        totalModifier += modifier.value;
                    }
                }
            }
        }
        return totalModifier;
    }
    #endregion
}