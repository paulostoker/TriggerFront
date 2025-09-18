// _Scripts/SetupState.cs - Novo Estado de Jogo
using UnityEngine;
using static ServiceLocator;

public class SetupState : IGameState
{
    private GameManager gameManager;

    public SetupState(GameManager gm)
    {
        gameManager = gm;
    }

    public void Enter()
    {
        Debug.Log($"<color=#FF7F50>Entering Setup Phase for Attacker ({gameManager.GetCurrentPlayerName()}).</color>");

        // Garante que a mão de cartas de energia/suporte esteja escondida
        if (ServiceLocator.Cards != null)
        {
            ServiceLocator.Cards.SetAllHandsVisibility(false);
        }

        // Mostra apenas as cartas de operador do time de Ataque
        bool isPlayer1Attacker = gameManager.isPlayer1Attacker;
        if (ServiceLocator.Cards != null)
        {
            // Mostra a mão de freelancers do atacante e esconde a do defensor
            ServiceLocator.Cards.SetFreelancerHandVisibility(isPlayer1Attacker, !isPlayer1Attacker);
            
            // Garante que elas estejam no modo de visualização de preparação
            ServiceLocator.Cards.SetFreelancerHandsDisplayMode(FreelancersUIContainer.DisplayMode.Setup);
        }
        
        // Exibe uma mensagem de instrução na tela
        if (ServiceLocator.UI != null)
        {
            ServiceLocator.UI.ShowGameMessage("Choose the Gift carrier", 0); // 0 = duração infinita
        }
        
        // A LINHA ABAIXO FOI REMOVIDA:
        // gameManager.ResetCameraForPreparation(); 
    }

    public void Execute()
    {
        // O processamento de cliques é feito pelo GameManager
    }

    public void Exit()
    {
        Debug.Log($"<color=#FF7F50>Exiting Setup Phase.</color>");
        
        // Limpa a mensagem da tela
        if (ServiceLocator.UI != null)
        {
            ServiceLocator.UI.HideGameMessage();
        }
    }
}