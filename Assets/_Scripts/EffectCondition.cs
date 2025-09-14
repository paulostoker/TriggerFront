// _Scripts/Effects/EffectCondition.cs
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class EffectCondition
{
    #region Fields
    [Tooltip("O tipo de condição a ser verificada.")]
    public ConditionType type;
    [Tooltip("Valor numérico para a condição (ex: distância, quantidade). Deixe 0 se não for usado.")]
    public int value;
    [Tooltip("A condição precisa ser verdadeira (true) ou falsa (false) para o efeito ser ativado?")]
    public bool requiredState = true;
    [Tooltip("Se marcado, esta condição é re-verificada a todo momento em que o bônus seria aplicado, em vez de apenas no uso da carta.")]
    public bool isPassiveCondition = false;
    [Tooltip("Ícone a ser exibido quando esta condição passiva estiver 'adormecida'.")]
    public string passiveIcon = "💭"; 
    #endregion

    #region Core Logic
    public bool Check(GameObject source, GameObject target)
{
    bool conditionMet = false;
    switch (type)
    {
        case ConditionType.NoKillsInRound:
            int p1Total = GameConfig.Instance.totalFreelancers;
            int p2Total = GameConfig.Instance.totalFreelancers;
            int p1Alive = ServiceLocator.Freelancers.GetAlivePiecesCount(true);
            int p2Alive = ServiceLocator.Freelancers.GetAlivePiecesCount(false);
            conditionMet = (p1Alive == p1Total && p2Alive == p2Total);
            break;

        case ConditionType.LastManStanding:
            FreelancerInstance selfOpForLastMan = ServiceLocator.Freelancers.GetFreelancerInstance(source);
            if (selfOpForLastMan == null)
            {
                conditionMet = false;
                break;
            }
            int aliveAlliesCountForLastMan = ServiceLocator.Freelancers.GetAlivePiecesCount(selfOpForLastMan.IsPlayer1);
            conditionMet = (aliveAlliesCountForLastMan == 1);
            break;

        case ConditionType.ProximityToAllyDeath:
            Tile tileOfSource = ServiceLocator.Grid.GetTileUnderPiece(source);
            if (tileOfSource == null || ServiceLocator.Freelancers.DeathPositions.Count == 0)
            {
                conditionMet = false;
                break;
            }
            conditionMet = false;
            foreach (var deathPos in ServiceLocator.Freelancers.DeathPositions)
            {
                int distance = ServiceLocator.Grid.GetManhattanDistance(tileOfSource.GetGridPosition(), deathPos);
                if (distance <= this.value)
                {
                    conditionMet = true;
                    break;
                }
            }
            break;

        case ConditionType.MinDistanceToAllies:
            FreelancerInstance sourceOp = ServiceLocator.Freelancers.GetFreelancerInstance(source);
            if (sourceOp == null) { conditionMet = false; break; }
            List<FreelancerInstance> allies = ServiceLocator.Freelancers.GetAllFreelancerInstances()
                .Where(op => op.IsPlayer1 == sourceOp.IsPlayer1 && op != sourceOp && op.IsAlive)
                .ToList();
            if (allies.Count == 0) { conditionMet = true; break; }
            Tile sourceTile = ServiceLocator.Grid.GetTileUnderPiece(source);
            if (sourceTile == null) { conditionMet = false; break; }
            conditionMet = true;
            foreach (var ally in allies)
            {
                Tile allyTile = ServiceLocator.Grid.GetTileUnderPiece(ally.PieceGameObject);
                if (allyTile != null)
                {
                    int distance = ServiceLocator.Grid.GetManhattanDistance(sourceTile, allyTile);
                    if (distance < this.value)
                    {
                        conditionMet = false;
                        break;
                    }
                }
            }
            break;

        case ConditionType.AllAlliesAlive:
            FreelancerInstance selfOpForAllies = ServiceLocator.Freelancers.GetFreelancerInstance(source);
            if (selfOpForAllies == null)
            {
                conditionMet = false;
                break;
            }
            int totalTeamSize = GameConfig.Instance.totalFreelancers;
            int aliveAlliesCount = ServiceLocator.Freelancers.GetAlivePiecesCount(selfOpForAllies.IsPlayer1);
            conditionMet = (aliveAlliesCount == totalTeamSize);
            break;
        
        case ConditionType.HasNotActed:
            if (ServiceLocator.Game.CurrentState is ActionState actionState)
                conditionMet = actionState.CanAct();
            break;

        case ConditionType.HasNotMoved:
            if (ServiceLocator.Game.CurrentState is ActionState stateForMove)
                conditionMet = stateForMove.CanMove();
            break;

        case ConditionType.HasEnergyEquipped:
            if (source != null)
            {
                var instance = ServiceLocator.Freelancers.GetFreelancerInstance(source);
                if (instance != null)
                    conditionMet = instance.EquippedEnergies.Count >= value;
            }
            break;
            
        case ConditionType.FirstTeamAction:
            if (ServiceLocator.Game.CurrentState is ActionState state)
                conditionMet = (source == state.FirstFreelancerOfTurn);
            break;

        // --- INÍCIO DA CORREÇÃO ---
        
        case ConditionType.HasCarryTarget: // Para a carta "Carry"
            if (ServiceLocator.Game.CurrentState is ActionState carryState)
            {
                var allAllies = ServiceLocator.Pieces.GetPlayerPieces(ServiceLocator.Game.IsPlayer1Turn());
                int activeIndex = carryState.GetActiveFreelancerIndex();
                conditionMet = allAllies.Any(ally => 
                        ally != source &&
                        ServiceLocator.Pieces.GetFreelancerIndex(ally) > activeIndex &&
                        !ServiceLocator.Effects.HasEffectOfType(ally, ModifierType.ForbidAttack)
                    );
            }
            break;

        case ConditionType.HasInGameLeaderTarget: // Para a carta "In-Game Leader"
             if (ServiceLocator.Game.CurrentState is ActionState iglState)
            {
                var allAllies = ServiceLocator.Pieces.GetPlayerPieces(ServiceLocator.Game.IsPlayer1Turn());
                int activeIndex = iglState.GetActiveFreelancerIndex();
                conditionMet = allAllies.Any(ally => 
                        ally != source &&
                        ServiceLocator.Pieces.GetFreelancerIndex(ally) > activeIndex
                    );
            }
            break;   
            case ConditionType.IsUsingAutomaticWeapon:
                if (source != null)
                {
                    var freelancerData = ServiceLocator.Freelancers.GetFreelancerData(source);
                    if (freelancerData != null)
                    {
                        conditionMet = WeaponData.IsAutomatic(freelancerData.weaponType);
                    }
                }
                break;

            case ConditionType.IsNotInEcoMode:
                if (source != null)
                {
                    conditionMet = !ServiceLocator.Freelancers.IsInEcoMode(source);
                }
                break;     
    }
    return conditionMet == requiredState;
}
    #endregion
}