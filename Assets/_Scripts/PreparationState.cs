// _Scripts/PreparationState.cs - Refatorado para usar FreelancerManager
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static ServiceLocator;

public class PreparationState : IGameState
{
    private GameManager gameManager;
    private HashSet<GameObject> equippedPiecesThisTurn;
    public bool hasUsedStrategyCardThisTurn;

    public PreparationState(GameManager gm)
    {
        gameManager = gm;
    }

   public void Enter()
    {
        gameManager.ResetCameraForPreparation(); 
        ServiceLocator.Effects.ProcessEffectDurations();
        ServiceLocator.Bomb.TickDefuseTimer();
        ServiceLocator.Bomb.TickBombTimer();
        if (ServiceLocator.Bomb != null && ServiceLocator.Bomb.IsRoundOver())
        {
            return;
        }
        Debug.Log($"<color=lightblue>Entering Preparation Phase for {gameManager.GetCurrentPlayerName()}.</color>");
        
        if (ServiceLocator.Audio != null)
        {
            ServiceLocator.Audio.PlayPrepSound();
        }
        
        equippedPiecesThisTurn = new HashSet<GameObject>();
        hasUsedStrategyCardThisTurn = false; // <-- ADICIONE ESTA LINHA
        
        if (ServiceLocator.Turn != null)
        {
            ServiceLocator.Turn.DestroyCurrentIndicator();
        }
        
        if (ServiceLocator.UI != null)
        {
            ServiceLocator.UI.SetMainUIVisibility(true);
            ServiceLocator.UI.ShowPreparationUI(true);
        }

        if (ServiceLocator.Cards != null)
        {
            ServiceLocator.Cards.SetFreelancerHandVisibilityForCurrentPlayer(gameManager.IsPlayer1Turn());
            ServiceLocator.Cards.SetFreelancerHandsDisplayMode(FreelancersUIContainer.DisplayMode.Preparation);
        }

        // --- INÍCIO DA LÓGICA DE COMPRA DE CARTAS CORRIGIDA ---
        if (GameConfig.Instance.useInitialHandDraw)
        {
            // Usando o novo sistema: Mão Inicial + Compra por Turno
            // A chamada agora é para ServiceLocator.Turn.GetTurnsRemaining()
            if (ServiceLocator.Turn.GetTurnsRemaining() == GameConfig.Instance.maxTotalTurns || 
               (ServiceLocator.Turn.GetTurnsRemaining() == GameConfig.Instance.maxTotalTurns - 1 && !gameManager.IsPlayer1Turn()))
            {
                // É o primeiro turno de preparação de um dos jogadores
                gameManager.DrawCardsForCurrentPlayer(GameConfig.Instance.initialHandSize);
            }
            else
            {
                // São os turnos subsequentes
                gameManager.DrawCardsForCurrentPlayer(GameConfig.Instance.cardsPerTurnAfterFirst);
            }
        }
        else if (GameConfig.Instance.drawCardsBasedOnAliveFreelancers)
        {
            // Usando o sistema antigo: 1 carta por freelancer vivo
            int aliveFreelancers = ServiceLocator.Freelancers.GetAlivePiecesCount(gameManager.IsPlayer1Turn());
            gameManager.DrawCardsForCurrentPlayer(aliveFreelancers);
        }
        else
        {
            // Usando o sistema antigo: número fixo de cartas por turno
            gameManager.DrawCardsForCurrentPlayer(GameConfig.Instance.cardsDrawnPerPreparation);
        }
        // --- FIM DA LÓGICA DE COMPRA DE CARTAS CORRIGIDA ---
        
        Debug.Log($"<color=lightblue>Preparation phase setup complete for {gameManager.GetCurrentPlayerName()}.</color>");
    }

    public void Execute()
    {
        // A lógica de clique agora é centralizada no GameManager,
        // mas este método é mantido para a estrutura da interface IGameState.
    }

    public void Exit()
    {
        Debug.Log($"<color=lightblue>Exiting Preparation Phase for {gameManager.GetCurrentPlayerName()}.</color>");
        
        if (ServiceLocator.UI != null)
        {
            ServiceLocator.UI.ShowPreparationUI(false);
        }
        
        gameManager.CancelEquipMode();
        
        equippedPiecesThisTurn.Clear();
        
        Debug.Log("<color=lightblue>Preparation phase cleanup complete.</color>");
    }

    // Verifica se uma peça já foi equipada neste turno de preparação.
    public bool HasPieceBeenEquipped(GameObject piece)
    {
        if (piece == null)
        {
            return false;
        }
        
        return equippedPiecesThisTurn.Contains(piece);
    }

    // Registra que uma peça foi equipada neste turno de preparação.
    public void RegisterEquippedPiece(GameObject piece)
    {
        if (piece == null)
        {
            return;
        }
        
        if (equippedPiecesThisTurn.Contains(piece))
        {
            return;
        }
        
        equippedPiecesThisTurn.Add(piece);
    }
    
    // Retorna quantas peças foram equipadas neste turno.
    public int GetEquippedPiecesCount()
    {
        return equippedPiecesThisTurn.Count;
    }
    
    // Verifica se todas as peças do jogador atual foram equipadas.
    public bool AreAllPiecesEquipped()
    {
        if (ServiceLocator.Pieces == null || ServiceLocator.Freelancers == null) return false;
        
        // Obtém todas as peças (GameObjects) do jogador atual.
        List<GameObject> currentPlayerPieces = ServiceLocator.Pieces.GetPlayerPieces(gameManager.IsPlayer1Turn());
        
        // Filtra apenas as peças que estão vivas, perguntando ao FreelancerManager.
        // --- ALTERADO ---
        List<GameObject> alivePieces = currentPlayerPieces.FindAll(piece => 
            ServiceLocator.Freelancers.IsAlive(piece));
        
        // Verifica se todas as peças vivas foram equipadas.
        foreach (GameObject piece in alivePieces)
        {
            if (!HasPieceBeenEquipped(piece))
            {
                return false;
            }
        }
        
        return true;
    }
    
    // Força o fim da preparação (usado por botões de UI ou comandos).
    public void ForceEndPreparation()
    {
        Debug.Log("<color=lightblue>Preparation phase ended by player choice.</color>");
        gameManager.EndPreparation();
    }
    
    // Retorna uma lista das peças que ainda não foram equipadas.
    public List<GameObject> GetUnequippedPieces()
    {
        List<GameObject> unequippedPieces = new List<GameObject>();
        if (ServiceLocator.Pieces == null || ServiceLocator.Freelancers == null) return unequippedPieces;
        
        List<GameObject> currentPlayerPieces = ServiceLocator.Pieces.GetPlayerPieces(gameManager.IsPlayer1Turn());
        
        foreach (GameObject piece in currentPlayerPieces)
        {
            if (ServiceLocator.Freelancers.IsAlive(piece) && !HasPieceBeenEquipped(piece))
            {
                unequippedPieces.Add(piece);
            }
        }
        
        return unequippedPieces;
    }
}