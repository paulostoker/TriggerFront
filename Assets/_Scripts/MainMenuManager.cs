// _Scripts/UI/MainMenuManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class AnimatedUIElement
{
    public CanvasGroup elementCanvasGroup;
    public float startDelay = 0f;
    public float duration = 1f;
    public Vector2 startPositionOffset;
}

[RequireComponent(typeof(AudioSource))]
public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Navigation")]
    public string characterSelectSceneName = "CharacterSelect";

    [Header("Audio")]
    public AudioClip backgroundMusic;
    public AudioClip buttonClickSound;

    [Header("UI Elements & Fades")]
    // --- ALTERADO ---
    [Tooltip("Painel BRANCO com um CanvasGroup para o fade-in inicial.")]
    public CanvasGroup fadeFromWhitePanel;
    [Tooltip("Painel PRETO com um CanvasGroup para o fade-out ao sair.")]
    public CanvasGroup fadeToBlackPanel;
    // --- FIM DA ALTERAÇÃO ---

    [Header("Animation Settings")]
    public List<AnimatedUIElement> animatedElements;
    [Tooltip("Duração do fade-in inicial (do branco para o transparente).")]
    public float fadeInDuration = 1.5f;

        [Header("Multiplayer UI")]
public GameObject multiplayerPanel;
public TMP_InputField joinCodeInput;
public TextMeshProUGUI feedbackText;
public TextMeshProUGUI createdJoinCodeText;

    private AudioSource audioSource;



    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        StartCoroutine(StartupSequenceCoroutine());
    }

    private IEnumerator StartupSequenceCoroutine()
    {
        // Garante que a tela comece branca e o painel preto esteja invisível.
        if(fadeFromWhitePanel != null) fadeFromWhitePanel.alpha = 1f;
        if(fadeToBlackPanel != null) fadeToBlackPanel.alpha = 0f;

        yield return null;

        PlayBackgroundMusic();
        AnimateAllElements();
        StartCoroutine(FadeInFromWhite()); // Renomeado para clareza
    }

    private void PlayBackgroundMusic()
    {
        if (backgroundMusic != null)
        {
            audioSource.clip = backgroundMusic;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void AnimateAllElements()
    {
        foreach (var element in animatedElements)
        {
            if (element.elementCanvasGroup != null)
            {
                element.elementCanvasGroup.alpha = 0;
                element.elementCanvasGroup.interactable = false;
            }
        }
        foreach (var element in animatedElements)
        {
            StartCoroutine(AnimateElement(element));
        }
    }
    
    // --- ALTERADO ---
    private IEnumerator FadeInFromWhite()
    {
        if (fadeFromWhitePanel == null) yield break;

        // Garante que o painel branco possa ser atravessado por cliques após o fade.
        fadeFromWhitePanel.blocksRaycasts = true;

        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            fadeFromWhitePanel.alpha = 1f - (elapsedTime / fadeInDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        fadeFromWhitePanel.alpha = 0f;
        fadeFromWhitePanel.blocksRaycasts = false; // Permite cliques nos botões
    }
    // --- FIM DA ALTERAÇÃO ---

    private IEnumerator AnimateElement(AnimatedUIElement element)
    {
        // ... (código inalterado)
        if (element.elementCanvasGroup == null) yield break;
        yield return new WaitForSeconds(element.startDelay);
        RectTransform rect = element.elementCanvasGroup.GetComponent<RectTransform>();
        Vector2 finalPosition = rect.anchoredPosition;
        Vector2 startPosition = finalPosition + element.startPositionOffset;
        float elapsedTime = 0f;
        while (elapsedTime < element.duration)
        {
            float progress = elapsedTime / element.duration;
            float easedProgress = 1 - Mathf.Pow(1 - progress, 3);
            element.elementCanvasGroup.alpha = easedProgress;
            rect.anchoredPosition = Vector2.Lerp(startPosition, finalPosition, easedProgress);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        element.elementCanvasGroup.alpha = 1;
        element.elementCanvasGroup.interactable = true;
        rect.anchoredPosition = finalPosition;
    }
    
    public void NewGame()
    {
        if (buttonClickSound != null) audioSource.PlayOneShot(buttonClickSound);
        StartCoroutine(FadeOutAndLoadScene(characterSelectSceneName));
    }

    public void OpenDeckManager()
    {
        if (buttonClickSound != null) audioSource.PlayOneShot(buttonClickSound);
        Debug.Log("Abrindo o Deck Manager...");
    }

public void OnPlayOnlineClicked()
{
    if (buttonClickSound != null) audioSource.PlayOneShot(buttonClickSound);
    if (multiplayerPanel != null) multiplayerPanel.SetActive(true);
}

public async void OnCreateMatchClicked()
{
    if (buttonClickSound != null) audioSource.PlayOneShot(buttonClickSound);
    if (feedbackText != null) feedbackText.text = "Creating match...";

    string joinCode = await NetworkConnectManager.Instance.CreateRelay();

    if (!string.IsNullOrEmpty(joinCode))
    {
        if (feedbackText != null) feedbackText.text = "Match created! Share the code:";
        if (createdJoinCodeText != null)
        {
            createdJoinCodeText.text = joinCode;
            createdJoinCodeText.gameObject.SetActive(true);
        }
    }
    else
    {
        if (feedbackText != null) feedbackText.text = "Failed to create match.";
    }
}

public async void OnJoinMatchClicked()
{
    if (buttonClickSound != null) audioSource.PlayOneShot(buttonClickSound);

    string joinCode = joinCodeInput.text;
    if (string.IsNullOrWhiteSpace(joinCode))
    {
        if (feedbackText != null) feedbackText.text = "Please enter a valid code.";
        return;
    }

    if (feedbackText != null) feedbackText.text = $"Joining match {joinCode}...";

    await NetworkConnectManager.Instance.JoinRelay(joinCode);
}
    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        // Usa o painel preto para o fade-out
        if (fadeToBlackPanel == null)
        {
            SceneManager.LoadScene(sceneName);
            yield break;
        }
        
        fadeToBlackPanel.blocksRaycasts = true;
        float elapsedTime = 0f;
        float fadeDuration = 2.0f;
        while (elapsedTime < fadeDuration)
        {
            fadeToBlackPanel.alpha = elapsedTime / fadeDuration;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        fadeToBlackPanel.alpha = 1;

        SceneManager.LoadScene(sceneName);
    }
    // --- FIM DA ALTERAÇÃO ---

    public void QuitGame()
    {
        if (buttonClickSound != null) audioSource.PlayOneShot(buttonClickSound);
        Debug.Log("Saindo do jogo...");
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}