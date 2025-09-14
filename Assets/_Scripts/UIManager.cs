using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    #region Serialized Fields
    [Header("Core References")]
    public GameManager gameManager;

    [Header("UI Panels")]
    public GameObject mainUIPanel;
    public GameObject endPreparationButton;
    public GameObject freelancerCardsCanvas;
    public Button endDropButton;

    [Header("Pause Menu")]
    public GameObject pauseMenuPanel;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI winnerText;
    public Button newRoundButton;

    [Header("Turn Counters")]
    public GameObject turnContainer;
    public GameObject turnBombContainer;
    public GameObject turnDefuseContainer;

    [Header("Prefabs & Parents")]
    public GameObject actionMenuPrefab;
    public GameObject actionSubMenuPrefab;
    public Transform mainCanvasTransform;

    [Header("Game Messages")]
    public TextMeshProUGUI gameMessageText;
    #endregion

    #region Private Fields
    private TextMeshProUGUI turnCounterText;
    private TextMeshProUGUI bombCounterText;
    private TextMeshProUGUI defuseCounterText;
    private GameObject activeActionMenu;
    private GameObject activeActionSubMenu;
    private Coroutine hideMessageCoroutine;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        if (gameManager == null || mainUIPanel == null || gameOverPanel == null || winnerText == null || newRoundButton == null)
        {
            Debug.LogError("UIManager is missing one or more references in the Inspector!");
            return;
        }

        gameOverPanel.SetActive(false);
        newRoundButton.onClick.AddListener(gameManager.StartNewRound);

        if (endPreparationButton != null)
        {
            endPreparationButton.GetComponent<Button>().onClick.AddListener(gameManager.EndPreparation);
            endPreparationButton.SetActive(false);
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        if (endDropButton != null)
        {
            endDropButton.onClick.AddListener(() => ServiceLocator.Cards.ExitManagerMode());
            endDropButton.gameObject.SetActive(false);
        }

        if (turnContainer != null) turnCounterText = turnContainer.GetComponentInChildren<TextMeshProUGUI>();
        if (turnBombContainer != null) bombCounterText = turnBombContainer.GetComponentInChildren<TextMeshProUGUI>();
        if (turnDefuseContainer != null) defuseCounterText = turnDefuseContainer.GetComponentInChildren<TextMeshProUGUI>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }
    #endregion

    #region Pause Menu Management
    public void TogglePauseMenu()
    {
        if (pauseMenuPanel == null) return;

        bool isCurrentlyActive = pauseMenuPanel.activeSelf;
        pauseMenuPanel.SetActive(!isCurrentlyActive);
    }

    public void ResetBattle()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NewBattle()
    {
        SceneManager.LoadScene("CharacterSelect");
    }
    
    public void ExitBattle()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Debug.Log("Saindo do jogo...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    #endregion

    #region Message System
    public void ShowGameMessage(string message, float duration = 3f)
    {
        if (gameMessageText == null) return;

        if (hideMessageCoroutine != null)
        {
            StopCoroutine(hideMessageCoroutine);
        }

        gameMessageText.text = message;
        gameMessageText.gameObject.SetActive(true);

        if (duration > 0)
        {
            hideMessageCoroutine = StartCoroutine(HideMessageAfterDelay(duration));
        }
    }

    public void HideGameMessage()
    {
        if (gameMessageText == null) return;

        if (hideMessageCoroutine != null)
        {
            StopCoroutine(hideMessageCoroutine);
            hideMessageCoroutine = null;
        }

        gameMessageText.gameObject.SetActive(false);
    }

    private IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameMessageText.gameObject.SetActive(false);
        hideMessageCoroutine = null;
    }
    #endregion

    #region Timer Management
    public void UpdateTimerDisplay()
    {
        if (turnContainer == null || turnBombContainer == null || turnDefuseContainer == null) return;

        var bombState = ServiceLocator.Bomb.GetCurrentBombState();

        bool showTurn = (bombState == BombManager.BombState.Unassigned || bombState == BombManager.BombState.Carried || bombState == BombManager.BombState.Dropped);
        bool showBomb = (bombState == BombManager.BombState.Planted);
        bool showDefuse = (bombState == BombManager.BombState.Defusing);

        turnContainer.SetActive(showTurn);
        turnBombContainer.SetActive(showBomb);
        turnDefuseContainer.SetActive(showDefuse);

        if (showBomb && bombCounterText != null)
        {
            bombCounterText.text = ServiceLocator.Bomb.GetBombTimeRemaining().ToString();
        }
        else if (showDefuse && defuseCounterText != null)
        {
            defuseCounterText.text = ServiceLocator.Bomb.GetDefuseTimeRemaining().ToString();
        }
    }
    #endregion

    #region UI Visibility Management
    public void SetMainUIVisibility(bool show)
    {
        IntroAnimationManager introManager = FindFirstObjectByType<IntroAnimationManager>();
        bool introActive = introManager != null && introManager.IsAnimationInProgress;

        if (introActive && show)
        {
            Debug.Log("<color=yellow>[UIManager]</color> Intro active, skipping SetMainUIVisibility(true)");
            return;
        }

        if (show && mainUIPanel != null)
        {
            mainUIPanel.SetActive(true);
            Debug.Log("<color=green>[UIManager]</color> Main UI panel activated (no intro interference)");
        }
        else if (!show && mainUIPanel != null)
        {
            mainUIPanel.SetActive(false);
            Debug.Log("<color=yellow>[UIManager]</color> Main UI panel deactivated");
        }

        ShowPreparationUI(show);
    }

    public void ShowPreparationUI(bool show)
    {
        if (endPreparationButton != null)
        {
            endPreparationButton.SetActive(show);
        }

        if (ServiceLocator.Cards != null)
        {
            if (show)
            {
                bool isP1Turn = ServiceLocator.Game != null ? ServiceLocator.Game.IsPlayer1Turn() : true;
                ServiceLocator.Cards.SetHandVisibility(isP1Turn);
            }
            else
            {
                ServiceLocator.Cards.SetAllHandsVisibility(false);
            }
        }
    }

    public void ForceShowAfterIntro()
    {
        if (mainUIPanel != null)
        {
            mainUIPanel.SetActive(true);
            Debug.Log("<color=green>[UIManager]</color> Main UI panel forced visible after intro");
        }

        ShowPreparationUI(true);
    }
    #endregion

    #region Action Menu Management
    public void ShowActionMenu(Vector3 pieceWorldPosition, bool canMove, bool canAct, bool canUseSkill, bool canSkip)
    {
        HideActionMenu();
        Vector3 screenPos = Camera.main.WorldToScreenPoint(pieceWorldPosition);

        activeActionMenu = Instantiate(actionMenuPrefab, mainCanvasTransform);
        activeActionMenu.transform.position = screenPos;

        // --- LÓGICA RESTAURADA PARA O MENU PRINCIPAL ---

        // Botão de Movimento
        Button moveButton = activeActionMenu.transform.Find("Move_Button")?.GetComponent<Button>();
        if (moveButton != null)
        {
            moveButton.onClick.AddListener(() => { ServiceLocator.Audio.PlayButtonClickSound(); ServiceLocator.Game.EnterMoveMode(); });
            moveButton.interactable = canMove;
        }

        // Botão de Ação (que abre o sub-menu)
        Button actButton = activeActionMenu.transform.Find("Act_Button")?.GetComponent<Button>();
        if (actButton != null)
        {
            actButton.onClick.AddListener(() => { ServiceLocator.Audio.PlayButtonClickSound(); ServiceLocator.Game.EnterActionMode(); });
            actButton.interactable = canAct;
        }

        // Botão de Skill (Habilidade de Carta)
        Button skillButton = activeActionMenu.transform.Find("Skill_Button")?.GetComponent<Button>();
        if (skillButton != null)
        {
            skillButton.onClick.AddListener(() => { ServiceLocator.Audio.PlayButtonClickSound(); ServiceLocator.Cards.EnterSkillMode(true); });
            skillButton.interactable = canUseSkill;
        }

        // Botão de Pular Turno
        Button skipButton = activeActionMenu.transform.Find("Skip_Button")?.GetComponent<Button>();
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(() => { ServiceLocator.Audio.PlayButtonClickSound(); ServiceLocator.Game.SkipFreelancerTurn(); });
            skipButton.interactable = canSkip;
        }
    }

    public void HideActionMenu()
    {
        if (activeActionMenu != null)
        {
            Destroy(activeActionMenu);
            activeActionMenu = null;
        }
        HideActionSubMenu();
    }

   public void ShowActionSubMenu(Vector3 pieceWorldPosition, bool canPlant, bool canDefuse, GameObject activePiece)
    {
        HideActionSubMenu();
        Vector3 screenPos = Camera.main.WorldToScreenPoint(pieceWorldPosition);
        activeActionSubMenu = Instantiate(actionSubMenuPrefab, mainCanvasTransform);
        activeActionSubMenu.transform.position = screenPos;

        FreelancerData freelancerData = ServiceLocator.Freelancers.GetFreelancerData(activePiece);

        // --- INÍCIO DA CORREÇÃO DE LISTENERS DUPLICADOS ---

        Button attackButton = activeActionSubMenu.transform.Find("Attack_Button")?.GetComponent<Button>();
        if (attackButton != null)
        {
            attackButton.onClick.RemoveAllListeners(); // LIMPA LISTENERS
            attackButton.onClick.AddListener(() => { ServiceLocator.Audio.PlayButtonClickSound(); ServiceLocator.Game.EnterAttackMode(); });
            attackButton.interactable = true;
        }

        Button techniqueButton = activeActionSubMenu.transform.Find("Technique_Button")?.GetComponent<Button>();
        if (techniqueButton != null)
        {
            techniqueButton.onClick.RemoveAllListeners(); // LIMPA LISTENERS
            TextMeshProUGUI techniqueText = techniqueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (freelancerData != null && freelancerData.techniques != null && freelancerData.techniques.Count > 0 && freelancerData.techniques[0] != null)
            {
                TechniqueData techData = freelancerData.techniques[0];
                if (techniqueText != null) techniqueText.text = techData.techniqueName;
                bool canAfford = ServiceLocator.Freelancers.HasEnergyForAction(activePiece, techData.cost);
                techniqueButton.interactable = canAfford;
                techniqueButton.onClick.AddListener(() => { ServiceLocator.Audio.PlayButtonClickSound(); ServiceLocator.Game.EnterTechniqueMode(0); });
            }
            else
            {
                if (techniqueText != null) techniqueText.text = "Technique";
                techniqueButton.interactable = false;
            }
        }

        Button ultimateButton = activeActionSubMenu.transform.Find("Ultimate_Button")?.GetComponent<Button>();
        if (ultimateButton != null)
        {
            ultimateButton.onClick.RemoveAllListeners(); // LIMPA LISTENERS
            TextMeshProUGUI ultimateText = ultimateButton.GetComponentInChildren<TextMeshProUGUI>();
            if (freelancerData != null && freelancerData.ultimate != null)
            {
                TechniqueData ultimateData = freelancerData.ultimate;
                if (ultimateText != null) ultimateText.text = ultimateData.techniqueName;
                bool canAfford = ServiceLocator.Freelancers.HasEnergyForAction(activePiece, ultimateData.cost);
                ultimateButton.interactable = canAfford;
                ultimateButton.onClick.AddListener(() => { ServiceLocator.Audio.PlayButtonClickSound(); ServiceLocator.Game.EnterUltimateMode(); });
            }
            else
            {
                if (ultimateText != null) ultimateText.text = "Ultimate";
                ultimateButton.interactable = false;
            }
        }
        
        Button plantButton = activeActionSubMenu.transform.Find("Plant_Button")?.GetComponent<Button>();
        if (plantButton != null) 
        {
            plantButton.onClick.RemoveAllListeners(); // LIMPA LISTENERS
            plantButton.gameObject.SetActive(canPlant);
            if (canPlant)
            {
                plantButton.onClick.AddListener(() => { ServiceLocator.Audio.PlayButtonClickSound(); ServiceLocator.Game.PlantBomb(); });
            }
        }
        
        Button defuseButton = activeActionSubMenu.transform.Find("Defuse_Button")?.GetComponent<Button>();
        if (defuseButton != null)
        {
            defuseButton.onClick.RemoveAllListeners(); // LIMPA LISTENERS
            defuseButton.gameObject.SetActive(canDefuse);
            if(canDefuse)
            {
                defuseButton.onClick.AddListener(() => { ServiceLocator.Audio.PlayButtonClickSound(); ServiceLocator.Game.DefuseBomb(); });
            }
        }
        // --- FIM DA CORREÇÃO DE LISTENERS ---
    }

    public void HideActionSubMenu()
    {
        if (activeActionSubMenu != null)
        {
            Destroy(activeActionSubMenu);
            activeActionSubMenu = null;
        }
    }

    public void ShowEndDropButton(bool show)
    {
        if (endDropButton != null)
        {
            endDropButton.gameObject.SetActive(show);
        }
    }

    public bool IsActionMenuVisible()
    {
        return activeActionMenu != null;
    }
    #endregion

    #region Game Over Management
    public void ShowGameOverUI(string winnerName)
    {
        if (gameOverPanel == null || winnerText == null) return;

        if (mainUIPanel != null)
        {
            mainUIPanel.SetActive(false);
        }

        if (freelancerCardsCanvas != null)
        {
            freelancerCardsCanvas.SetActive(false);
        }

        HideActionMenu();

        if (endPreparationButton != null)
        {
            endPreparationButton.SetActive(false);
        }

        winnerText.text = $"{winnerName} Wins!";
        gameOverPanel.SetActive(true);
    }
    #endregion
}