using UnityEngine;
using TMPro;

public class TurnDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI turnText;        // Para "40 turns left"
    public TextMeshProUGUI playerNameText;  // Para "Player 1" separado
    
    [Header("Display Settings")]
    public string turnSuffix = " turns left";
    public string player1Name = "Attackers";
    public string player2Name = "Defenders";
    
    [Header("Turn Limit Settings")]
    public int maxTurns = 40;
    
    private int turnsRemaining = 40;
    private bool isIntroActive = false;
    
    void Start()
    {
        if (turnText == null)
            turnText = GetComponentInChildren<TextMeshProUGUI>();
            
        turnsRemaining = maxTurns;
        
        // NOVO: Verifica se intro está ativa antes de mostrar
        CheckIntroState();
    }
    
    private void CheckIntroState()
    {
        // Procura pelo IntroAnimationManager
        IntroAnimationManager introManager = FindFirstObjectByType<IntroAnimationManager>();
        
        if (introManager != null && introManager.IsAnimationInProgress)
        {
            isIntroActive = true;
            // Esconde tudo durante a intro
            SetVisibility(false);
            
            // Inicia corrotina para aguardar intro terminar
            StartCoroutine(WaitForIntroToComplete(introManager));
        }
        else
        {
            // Sem intro ou intro já terminada
            isIntroActive = false;
            UpdateTurnDisplay(true);
        }
    }
    
    private System.Collections.IEnumerator WaitForIntroToComplete(IntroAnimationManager introManager)
    {
        // Aguarda intro terminar
        while (introManager != null && introManager.IsAnimationInProgress)
        {
            yield return null;
        }
        
        // Intro terminou, agora pode mostrar
        isIntroActive = false;
        SetVisibility(true);
        UpdateTurnDisplay(true);
        
        Debug.Log("<color=cyan>[TurnDisplay]</color> Intro completed, display now visible");
    }
    
    private void SetVisibility(bool visible)
    {
        if (turnText != null)
            turnText.gameObject.SetActive(visible);
            
        if (playerNameText != null)
            playerNameText.gameObject.SetActive(visible);
            
        // Também desativa o GameObject pai se existir
        gameObject.SetActive(visible);
    }
    
    public void UpdateTurnDisplay(bool isPlayer1Turn)
    {
        // NOVO: Não atualiza se intro estiver ativa
        if (isIntroActive)
        {
            Debug.Log("<color=yellow>[TurnDisplay]</color> Intro active, skipping display update");
            return;
        }
        
        // Atualiza contador de turnos
        if (turnText != null)
        {
            turnText.text = $"{turnsRemaining}{turnSuffix}";
            
            // Muda cor quando restam poucos turnos
            if (turnsRemaining <= 5)
            {
                turnText.color = Color.red;
            }
            else if (turnsRemaining <= 10)
            {
                turnText.color = Color.yellow;
            }
            else
            {
                turnText.color = Color.white;
            }
        }
        
        // Atualiza nome do jogador separadamente
        if (playerNameText != null)
        {
            string currentPlayerName = isPlayer1Turn ? player1Name : player2Name;
            playerNameText.text = currentPlayerName;
            
            // Opcional: cores diferentes para cada jogador
            playerNameText.color = isPlayer1Turn ? Color.red : Color.cyan;
        }
    }
    
    public void SetTurnsRemaining(int turns)
    {
        turnsRemaining = turns;
    }
    
    public int GetTurnsRemaining()
    {
        return turnsRemaining;
    }
    
    // Método para forçar visibilidade após intro
    public void ForceShowAfterIntro()
    {
        isIntroActive = false;
        SetVisibility(true);
        UpdateTurnDisplay(true);
    }
}