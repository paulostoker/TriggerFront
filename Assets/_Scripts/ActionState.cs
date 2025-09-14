// _Scripts/ActionState.cs - VERSÃO CORRIGIDA PRESERVANDO FLUXO ORIGINAL
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using static ServiceLocator;

public class ActionState : IGameState
{
    #region Fields & Properties
    private GameManager gameManager;
    private int activeFreelancerIndex = -1;
    public GameObject FirstFreelancerOfTurn { get; private set; }
    public static event Action OnActionPhaseBegan;
    private bool teamHasTakenAction = false;
    public bool IsProcessingAction { get; set; }
    public bool IsSkillModeActive { get; set; }
    private bool isWidePeekActive = false;
    private int widePeekMovesMade = 0;
    private int widePeekTotalMovement = 0;
    private int widePeekMovementSpent = 0;
    private int totalFreelancers => GameConfig.Instance.totalFreelancers;
    private int maxCheckedFreelancers => totalFreelancers * 2;
    #endregion

    #region State Management
    public ActionState(GameManager gm)
    {
        gameManager = gm;
    }

    public void Enter()
    {
        IsProcessingAction = false;
        Debug.Log($"<color=orange>Entering Action Phase for {gameManager.GetCurrentPlayerName()}.</color>");
        
        // --- CORREÇÃO APLICADA AQUI ---
        // Reseta as cargas de ação para 1 para todos no time no início da fase.
        ServiceLocator.Freelancers.ResetAllActionChargesForTeam(gameManager.IsPlayer1Turn());
        
        ResetAllTeamFreelancerStates();
        
        if (ServiceLocator.CameraManager != null)
            ServiceLocator.CameraManager.SetupForAction();
            
        int firstAliveIndex = FindFirstAliveFreelancerIndex(gameManager.IsPlayer1Turn());
        FirstFreelancerOfTurn = ServiceLocator.Pieces.GetActivePiece(firstAliveIndex, gameManager.IsPlayer1Turn());
        
        OnActionPhaseBegan?.Invoke();
        
        if (ServiceLocator.Cards != null)
        {
            bool isPlayer1 = gameManager.IsPlayer1Turn();
            ServiceLocator.Cards.SetFreelancerHandVisibilityForCurrentPlayer(isPlayer1);
            ServiceLocator.Cards.SetFreelancerHandsDisplayMode(FreelancersUIContainer.DisplayMode.Action);
            ServiceLocator.Cards.SetActiveFreelancer(firstAliveIndex, isPlayer1);
        }
        
        activeFreelancerIndex = -1;
        EndFreelancerTurn();
    }
    public void Execute()
    {
        if (IsProcessingAction) return;
        gameManager.HandleActionPhaseClick();
    }

    public void Exit()
    {
        Debug.Log($"<color=orange>Exiting Action Phase for {gameManager.GetCurrentPlayerName()}.</color>");
        if (ServiceLocator.Pieces != null)
            ServiceLocator.Pieces.DeselectCurrentPiece();
        if (ServiceLocator.Cards != null)
            ServiceLocator.Cards.SetFreelancerHandVisibility(false, false);
        IsProcessingAction = false;
        activeFreelancerIndex = -1;
        Debug.Log("<color=orange>Action phase cleanup complete.</color>");
    }
    #endregion

    #region Limpeza Robusta de Estados (Timing Correto)
    /// <summary>
    /// CORREÇÃO PRINCIPAL: Limpeza robusta feita APENAS no início do ActionState
    /// Não interfere no fluxo natural de turnos
    /// </summary>
    private void ResetAllTeamFreelancerStates()
    {
        if (ServiceLocator.Freelancers == null)
        {
            Debug.LogError("[ActionState] FreelancerManager não disponível para reset de estados!");
            return;
        }

        bool isPlayer1 = gameManager.IsPlayer1Turn();
        string teamName = isPlayer1 ? "Player 1" : "Player 2";
        int resetCount = 0;

        Debug.Log($"<color=yellow>[ActionState RESET]</color> Iniciando limpeza robusta de estados para {teamName}");

        // Obtém todos os freelancers do time atual
        for (int i = 0; i < totalFreelancers; i++)
        {
            GameObject freelancerPiece = ServiceLocator.Pieces.GetActivePiece(i, isPlayer1);
            if (freelancerPiece != null)
            {
                // Força o reset do estado, independente de outras condições
                ServiceLocator.Freelancers.ResetTurnState(freelancerPiece);
                
                // Validação adicional - verifica se o reset foi bem sucedido
                ValidateFreelancerStateReset(freelancerPiece, i);
                resetCount++;
            }
        }

        Debug.Log($"<color=green>[ActionState RESET]</color> Estados resetados com sucesso para {resetCount} freelancers de {teamName}");
    }

    /// <summary>
    /// Valida se o reset do freelancer foi bem sucedido e força correção se necessário
    /// </summary>
    private void ValidateFreelancerStateReset(GameObject freelancer, int index)
    {
        if (ServiceLocator.Freelancers.CanFreelancerMove(freelancer) && 
            ServiceLocator.Freelancers.CanFreelancerAct(freelancer) && 
            ServiceLocator.Freelancers.CanFreelancerUseSkill(freelancer))
        {
            // Estado correto - freelancer pode fazer tudo
            Debug.Log($"<color=green>[VALIDATION]</color> Freelancer #{index + 1} ({freelancer.name}) - Estado OK");
        }
        else
        {
            // Estado incorreto - força reset manual
            Debug.LogWarning($"<color=red>[VALIDATION FAILED]</color> Freelancer #{index + 1} ({freelancer.name}) - Estado incorreto detectado! Forçando reset...");
            
            FreelancerInstance instance = ServiceLocator.Freelancers.GetFreelancerInstance(freelancer);
            if (instance != null)
            {
                // Reset forçado manual
                instance.HasMovedThisTurn = false;
                instance.HasActedThisTurn = false;
                instance.HasUsedSkillThisTurn = false;
                
                Debug.Log($"<color=yellow>[FORCED RESET]</color> Reset forçado aplicado para {freelancer.name}");
            }
        }
    }
    #endregion

    #region Turn Management (FLUXO ORIGINAL RESTAURADO)
    private void StartFreelancerTurn()
    {
        IsProcessingAction = false;
        IsSkillModeActive = false;
        isWidePeekActive = false;
        widePeekMovesMade = 0;
        widePeekMovementSpent = 0;
        widePeekTotalMovement = 0;
        
        GameObject activePiece = GetActiveFreelancer();
        if (activePiece != null)
        {
            // Reset individual (não interfere no fluxo)
            ServiceLocator.Freelancers.ResetTurnState(activePiece);
            
            // Log de validação do estado do freelancer ativo
            Debug.Log($"<color=cyan>[TURN START]</color> Freelancer {activePiece.name}:");
            Debug.Log($"  - Can Move: {ServiceLocator.Freelancers.CanFreelancerMove(activePiece)}");
            Debug.Log($"  - Can Act: {ServiceLocator.Freelancers.CanFreelancerAct(activePiece)}");
            Debug.Log($"  - Can Use Skill: {ServiceLocator.Freelancers.CanFreelancerUseSkill(activePiece)}");
        }
        
        if (ServiceLocator.Pieces != null)
            ServiceLocator.Pieces.DeselectCurrentPiece();
            
        if (ServiceLocator.Turn != null && activePiece != null)
            ServiceLocator.Turn.AnnounceFreelancerTurn(activePiece, activeFreelancerIndex, gameManager.GetCurrentPlayerName());
            
        if (ServiceLocator.Audio != null && ServiceLocator.Freelancers != null && activePiece != null)
        {
            WeaponType weaponType = WeaponType.Pistol;
            if (!ServiceLocator.Freelancers.IsInEcoMode(activePiece))
            {
                FreelancerData freelancerData = ServiceLocator.Freelancers.GetFreelancerData(activePiece);
                if (freelancerData != null)
                    weaponType = freelancerData.weaponType;
            }
            ServiceLocator.Audio.PlayEquipSound(weaponType);
            if (GameConfig.Instance.enableCombatLogs)
                Debug.Log($"<color=purple>[ActionState]</color> Playing equip sound for {weaponType} weapon");
        }
        
        Debug.Log($"<color=orange>Started turn for freelancer #{activeFreelancerIndex + 1}</color>");
    }

    // RESTAURA a lógica original de EndFreelancerTurn que funcionava
    public void EndFreelancerTurn()
    {
        Debug.Log($"<color=orange>Ending turn for freelancer #{activeFreelancerIndex + 1}</color>");
        GameObject finishedFreelancer = GetActiveFreelancer();
        if (finishedFreelancer != null)
        {
            // --- LÓGICA DE LIMPEZA DA PRE-FIRE ---
            // Garante que qualquer resultado de dado não utilizado seja descartado
            ServiceLocator.Freelancers.ClearStoredDiceResult(finishedFreelancer);
            // --- FIM DA LÓGICA ---

            ServiceLocator.Effects.ProcessSingleFreelancerDurations(finishedFreelancer);
            ServiceLocator.Effects.CleanUpSelfTargetingEffects(finishedFreelancer);
        }
            
        if (activeFreelancerIndex != -1)
        {
            if (ServiceLocator.Cards != null)
                ServiceLocator.Cards.CycleActiveFreelancer(gameManager.IsPlayer1Turn());
        }
        
        int checkedFreelancers = 0;
        GameObject nextPiece = null;
        do
        {
            activeFreelancerIndex++;
            checkedFreelancers++;
            if (checkedFreelancers > maxCheckedFreelancers)
            {
                gameManager.EndGame(gameManager.IsPlayer1Turn() ? "Player 2" : "Player 1");
                return;
            }
            
            if (activeFreelancerIndex >= totalFreelancers)
            {
                gameManager.SwitchPlayerAndEnterPreparation();
                return;
            }
            
            nextPiece = GetActiveFreelancer();
        } while (nextPiece == null || !IsFreelancerAlive(nextPiece));
        
        if (nextPiece != null)
            StartFreelancerTurn();
        else
        {
            gameManager.EndGame(gameManager.IsPlayer1Turn() ? "Player 2" : "Player 1");
        }
    }

     public void SkipCurrentFreelancerTurn()
    {
        Debug.Log($"<color=yellow>Skipping turn for freelancer #{activeFreelancerIndex + 1}</color>");
        if (ServiceLocator.Pieces != null)
            ServiceLocator.Pieces.DeselectCurrentPiece();
        EndFreelancerTurn();
    }

     public void ForceCompleteTurn()
    {
        Debug.Log($"<color=yellow>Forcing turn completion for freelancer #{activeFreelancerIndex + 1}</color>");
        GameObject activeFreelancer = GetActiveFreelancer();
        if (activeFreelancer != null)
        {
            ServiceLocator.Freelancers.SetFreelancerMoved(activeFreelancer);
            // Em vez de 'SetFreelancerActed', agora zeramos as cargas de ação restantes.
            ServiceLocator.Freelancers.SetActionCharges(activeFreelancer, 0);
        }
        EndFreelancerTurn();
    }
    #endregion



    #region Wide Peek System
    public void ActivateWidePeek()
    {
        isWidePeekActive = true;
        widePeekMovesMade = 0;
        widePeekMovementSpent = 0;
        GameObject activeFreelancer = GetActiveFreelancer();
        if (activeFreelancer != null)
        {
            FreelancerData opData = ServiceLocator.Freelancers.GetFreelancerData(activeFreelancer);
            int baseMovement = opData != null ? opData.baseMovement : 0;
            int bonusMovement = ServiceLocator.Effects.GetStatModifier(activeFreelancer, ModifierType.Movement);
            widePeekTotalMovement = baseMovement + bonusMovement;
            Debug.Log($"<color=magenta>[Wide Peek]</color> Modo ativado para '{activeFreelancer.name}' com {widePeekTotalMovement} pontos de movimento.");
        }
    }

    public bool IsInWidePeekMode() => isWidePeekActive;

    public int GetRemainingWidePeekMovement()
    {
        if (!isWidePeekActive) return 0;
        return widePeekTotalMovement - widePeekMovementSpent;
    }

    public void RegisterWidePeekMove(int distanceMoved)
    {
        if (!isWidePeekActive) return;
        widePeekMovesMade++;
        widePeekMovementSpent += distanceMoved;
        Debug.Log($"<color=magenta>[Wide Peek]</color> Movimento {widePeekMovesMade}/2 realizado. Gasto: {widePeekMovementSpent}/{widePeekTotalMovement} pontos.");
        if (widePeekMovesMade >= 2 || GetRemainingWidePeekMovement() <= 0)
        {
            GameObject activeFreelancer = GetActiveFreelancer();
            if (activeFreelancer != null)
                ServiceLocator.Freelancers.SetFreelancerMoved(activeFreelancer);
            isWidePeekActive = false;
            Debug.Log($"<color=magenta>[Wide Peek]</color> Ação de movimento concluída.");
        }
    }
    #endregion

    #region Freelancer State Management
    public void SetMoved()
    {
        if (isWidePeekActive) return;
        GameObject activeFreelancer = GetActiveFreelancer();
        if (activeFreelancer != null)
        {
            ServiceLocator.Freelancers.SetFreelancerMoved(activeFreelancer);
            teamHasTakenAction = true;
        }
    }

    public void SetActed()
    {
        GameObject activeFreelancer = GetActiveFreelancer();
        if (activeFreelancer != null)
        {
            ServiceLocator.Freelancers.ConsumeActionCharge(activeFreelancer);
            teamHasTakenAction = true;
        }
    }

    public void SetTechniqueUsed()
    {
        GameObject activeFreelancer = GetActiveFreelancer();
        if (activeFreelancer != null)
        {
            ServiceLocator.Freelancers.ConsumeActionCharge(activeFreelancer);
            teamHasTakenAction = true;
            
            // Após usar a técnica, verifica se o turno do freelancer acabou
            if (IsTurnComplete())
            {
                EndFreelancerTurn();
            }
        }
    }

    public void SetSkillUsed()
    {
        GameObject activeFreelancer = GetActiveFreelancer();
        if (activeFreelancer != null)
        {
            ServiceLocator.Freelancers.SetFreelancerUsedSkill(activeFreelancer);
            teamHasTakenAction = true;
        }
    }

    public bool CanMove()
    {
        if (isWidePeekActive)
            return widePeekMovesMade < 2 && GetRemainingWidePeekMovement() > 0;
        GameObject activeFreelancer = GetActiveFreelancer();
        return activeFreelancer != null ? ServiceLocator.Freelancers.CanFreelancerMove(activeFreelancer) : false;
    }

    public bool CanAct()
    {
        GameObject activeFreelancer = GetActiveFreelancer();
        return activeFreelancer != null ? ServiceLocator.Freelancers.CanFreelancerAct(activeFreelancer) : false;
    }

    public bool CanUseSkill()
    {
        GameObject activeFreelancer = GetActiveFreelancer();
        return activeFreelancer != null ? ServiceLocator.Freelancers.CanFreelancerUseSkill(activeFreelancer) : false;
    }

    public bool HasTeamTakenAction() => teamHasTakenAction;

    public bool IsTurnComplete()
    {
        GameObject activeFreelancer = GetActiveFreelancer();
        return activeFreelancer != null ? ServiceLocator.Freelancers.HasFreelancerCompletedTurn(activeFreelancer) : true;
    }
    #endregion

    #region Utility Methods
    private int FindFirstAliveFreelancerIndex(bool isPlayer1)
    {
        for (int i = 0; i < totalFreelancers; i++)
        {
            GameObject piece = ServiceLocator.Pieces.GetActivePiece(i, isPlayer1);
            if (piece != null && ServiceLocator.Freelancers.IsAlive(piece))
                return i;
        }
        return 0;
    }

    private bool IsFreelancerAlive(GameObject freelancerPiece)
    {
        if (freelancerPiece == null) return false;
        if (ServiceLocator.Freelancers == null)
        {
            Debug.LogError("ActionState: FreelancerManager not available via ServiceLocator!");
            return false;
        }
        return ServiceLocator.Freelancers.IsAlive(freelancerPiece);
    }

    public GameObject GetNextAliveFreelancerInTurnOrder()
    {
        int startIndex = activeFreelancerIndex + 1;
        int freelancersChecked = 0;
        int maxChecks = totalFreelancers * 2;
        for (int i = startIndex; freelancersChecked < maxChecks; i++)
        {
            freelancersChecked++;
            int currentIndex = i % totalFreelancers;
            GameObject piece = ServiceLocator.Pieces.GetActivePiece(currentIndex, gameManager.IsPlayer1Turn());
            if (piece != null && ServiceLocator.Freelancers.IsAlive(piece))
                return piece;
        }
        return null;
    }

    public int GetActiveFreelancerIndex() => activeFreelancerIndex;

    public GameObject GetActiveFreelancer()
    {
        if (activeFreelancerIndex < 0 || activeFreelancerIndex >= totalFreelancers)
            return null;
        if (ServiceLocator.Pieces == null) return null;
        return ServiceLocator.Pieces.GetActivePiece(activeFreelancerIndex, gameManager.IsPlayer1Turn());
    }

    public string GetCurrentFreelancerState()
    {
        if (activeFreelancerIndex < 0) return "No active freelancer";
        GameObject activeOp = GetActiveFreelancer();
        string freelancerName = activeOp != null ? activeOp.name : "Unknown";
        bool hasMoved = activeOp != null && !ServiceLocator.Freelancers.CanFreelancerMove(activeOp);
        bool hasActed = activeOp != null && !ServiceLocator.Freelancers.CanFreelancerAct(activeOp);
        return $"Freelancer #{activeFreelancerIndex + 1} ({freelancerName}): " +
               $"Moved={hasMoved}, Acted={hasActed}, Processing={IsProcessingAction}";
    }

    public bool HasAliveFreelancers()
    {
        if (ServiceLocator.Freelancers == null) return false;
        return !ServiceLocator.Freelancers.AreAllFreelancersDeadOnTeam(gameManager.IsPlayer1Turn());
    }

    public int GetRemainingFreelancers() => Mathf.Max(0, totalFreelancers - (activeFreelancerIndex + 1));

    public void ResetActionState()
    {
        Debug.Log("<color=yellow>ActionState: Resetting action state completely.</color>");
        activeFreelancerIndex = -1;
        IsProcessingAction = false;
        if (ServiceLocator.Pieces != null)
            ServiceLocator.Pieces.DeselectCurrentPiece();
        if (ServiceLocator.Cards != null)
            ServiceLocator.Cards.SetFreelancerHandVisibility(false, false);
    }
    #endregion

    #region Card Management
    public void RefreshActiveFreelancerCard() { }

    public bool AreFreelancerCardsVisible()
    {
        if (ServiceLocator.Cards == null) return false;
        bool isPlayer1 = gameManager.IsPlayer1Turn();
        FreelancersUIContainer freelancerHand = ServiceLocator.Cards.GetFreelancerHand(isPlayer1);
        return freelancerHand != null && freelancerHand.gameObject.activeInHierarchy;
    }

    public void LogFreelancerCardState()
    {
        if (ServiceLocator.Cards == null)
        {
            Debug.Log("ActionState: CardManager not available");
            return;
        }
        Debug.Log("=== OPERATOR CARD STATE ===");
        Debug.Log($"Active Freelancer Index: {activeFreelancerIndex}");
        Debug.Log($"Current Player: {gameManager.GetCurrentPlayerName()}");
        Debug.Log($"Freelancer Cards Initialized: {ServiceLocator.Cards.AreFreelancerCardsInitialized()}");
        Debug.Log($"Freelancer Cards Visible: {AreFreelancerCardsVisible()}");
        bool isPlayer1 = gameManager.IsPlayer1Turn();
        FreelancersUIContainer freelancerHand = ServiceLocator.Cards.GetFreelancerHand(isPlayer1);
        if (freelancerHand != null)
        {
            Debug.Log($"Freelancer Hand Card Count: {freelancerHand.GetCardCount()}");
            Debug.Log($"Active Freelancer in Hand: {freelancerHand.GetActiveFreelancerIndex()}");
        }
    }

    public static void ResetStaticData()
    {
        OnActionPhaseBegan = null;
        Debug.Log("<color=orange>[ActionState]</color> Static events cleared for new game session");
    }
    #endregion
}