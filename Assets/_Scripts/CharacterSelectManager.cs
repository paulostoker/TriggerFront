// _Scripts/CharacterSelectManager.cs - VERSÃO FINAL, COMPLETA E CORRIGIDA

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Collections;

[RequireComponent(typeof(AudioSource))]
public class CharacterSelectManager : MonoBehaviour
{
    public static CharacterSelectManager Instance { get; private set; }

    [Header("UI References")]
    public List<RectTransform> attackerSlotPlaceholders;
    public List<RectTransform> defenderSlotPlaceholders;
    public GameObject previewCardContainer;
    public GameObject selectionGridContainer;
    public Button lockInButton;
    public Button backButton;
    public Button shuffleButton;
    public Button startBattleButton; // Botão de "Confirm Team" ou "Start Battle"
    public TextMeshProUGUI statusText;

    public Button confirmTeamButton; 

    [Header("Scene Transition")]
    public string battleSceneName = "Battle";

    [Header("Data & Prefabs")]
    public FreelancerDatabase freelancerDatabase;
    public GameObject freelancerIconPrefab;
    public GameObject card3DPrefab;

    [Header("Card Materials")]
    public Material freelancerMaterial;
    public Material borderMaterial;
    public Material backMaterial;

    [Header("Animation Settings")]
    public float previewAnimationDuration = 0.3f;

    [Header("Audio")]
    public AudioClip backgroundMusic;
    public AudioClip buttonClickSound;
    public AudioClip buttonConfirmSound;
    public AudioClip buttonRemoveSound;
    
    [Header("Camera Animation")]
    public Camera targetCamera;
    public float cameraRotationSpeed = 1f;

    [Header("Networking")]
    public GameObject playerNetworkDataPrefab;

    // --- Variáveis Internas ---
    private GameSessionManager gameSession;
    private List<FreelancerData> allFreelancers;
    private Dictionary<FreelancerData, FreelancerSelectionIcon> iconLookup = new Dictionary<FreelancerData, FreelancerSelectionIcon>();
    private FreelancerData currentPreviewFreelancer;
    private FreelancerSelectionIcon currentHighlightedIcon = null;
    private GameObject currentPreviewCardInstance;
    private Coroutine previewAnimationCoroutine;
    private List<GameObject> attackerCardInstances = new List<GameObject>();
    private List<GameObject> defenderCardInstances = new List<GameObject>();
    private bool isAttackersTurn = true; // Para modo local
    private int nextSlotIndex = 0; // Para modo local
    private AudioSource audioSource;
    
    // --- Lógica de Rede ---
    private List<FreelancerData> localPlayerSelection = new List<FreelancerData>();
    private HashSet<ulong> readyClients = new HashSet<ulong>(); // Apenas o Host usa esta lista

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
{
    if (GameSessionManager.Instance == null) { Debug.LogError("GameSessionManager not found!"); enabled = false; return; }
    gameSession = GameSessionManager.Instance;
    gameSession.ResetSelections();
    
    LoadAllFreelancers();
    PopulateSelectionGrid();

    if (backgroundMusic != null) { audioSource.clip = backgroundMusic; audioSource.loop = true; audioSource.Play(); }
    if (targetCamera == null) targetCamera = Camera.main;

    if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost))
    {
        // Lógica de rede que estava faltando
        if (playerNetworkDataPrefab != null)
        {
            Instantiate(playerNetworkDataPrefab);
        }
        else
        {
            Debug.LogError("PlayerNetworkData Prefab não está associado no CharacterSelectManager!");
        }
        SetupUIForOnlineMode();
    }
    else
    {
        SetupUIForLocalMode();
    }
}

    void Update()
    {
        if (targetCamera != null) { targetCamera.transform.Rotate(0, 0, cameraRotationSpeed * Time.deltaTime); }
    }

    public void StorePlayerSelection(ulong clientId, string selectionPayload)
{
    if (!NetworkManager.Singleton.IsServer) return;
    Debug.Log($"Host: Received selection payload from ClientId: {clientId}. Payload: {selectionPayload}");

    // Separa a string de volta em uma lista de nomes
    List<string> selectedNames = selectionPayload.Split(',').ToList();
    
    // Converte os nomes de volta para os ScriptableObjects FreelancerData
    List<FreelancerData> selectedFreelancers = new List<FreelancerData>();
    foreach (string freelancerName in selectedNames)
    {
        FreelancerData data = allFreelancers.FirstOrDefault(f => f.name == freelancerName);
        if (data != null)
        {
            selectedFreelancers.Add(data);
        }
    }

    // Armazena a seleção no GameSessionManager, que persistirá para a próxima cena
    if (clientId == NetworkManager.ServerClientId) // Se for o Host (Atacante)
    {
        gameSession.SelectedAttackers = selectedFreelancers;
        Debug.Log($"Host stored {selectedFreelancers.Count} attackers.");
    }
    else // Se for um Cliente (Defensor)
    {
        gameSession.SelectedDefenders = selectedFreelancers;
        Debug.Log($"Host stored {selectedFreelancers.Count} defenders.");
    }
    
    readyClients.Add(clientId);
    CheckIfAllPlayersAreReady();
}

    public void CheckIfAllPlayersAreReady()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        int connectedPlayers = NetworkManager.Singleton.ConnectedClientsList.Count;
        
        if (connectedPlayers >= 2 && readyClients.Count == connectedPlayers)
        {
            Debug.Log("All players are ready! Host is starting the game...");
            NetworkManager.Singleton.SceneManager.LoadScene(battleSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.Log($"Waiting... {readyClients.Count}/{connectedPlayers} players are ready.");
        }
    }
    
    public void OnConfirmTeamClicked()
{
    Debug.Log("<color=cyan>[DEBUG]</color> OnConfirmTeamClicked: O método foi chamado com sucesso!");

    if (localPlayerSelection.Count < 5)
    {
        Debug.LogWarning($"<color=orange>[DEBUG]</color> OnConfirmTeamClicked: Tentativa de confirmar com {localPlayerSelection.Count}/5 freelancers. Ação interrompida.");
        return;
    }

    PlayerNetworkData localPlayerNetworkData = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject()?.GetComponent<PlayerNetworkData>();

    if (localPlayerNetworkData != null)
    {
        Debug.Log("<color=green>[DEBUG]</color> OnConfirmTeamClicked: PlayerNetworkData encontrado! Enviando RPC para o servidor...");
        
        // --- INÍCIO DA MUDANÇA CRÍTICA ---
        // Cria uma lista de nomes a partir da nossa seleção local
        List<string> selectedNames = localPlayerSelection.Select(f => f.name).ToList();
        // Converte a lista para uma string única separada por vírgulas (um formato simples)
        string selectionPayloadString = string.Join(",", selectedNames);
        // --- FIM DA MUDANÇA CRÍTICA ---

        localPlayerNetworkData.isReady.Value = true;
        FixedString512Bytes selectionPayload = new FixedString512Bytes(selectionPayloadString);
        localPlayerNetworkData.SubmitSelectedFreelancersServerRpc(selectionPayload);

        if (confirmTeamButton != null) confirmTeamButton.interactable = false;
        if (lockInButton != null) lockInButton.interactable = false;
        if (shuffleButton != null) shuffleButton.interactable = false;
        
        if (statusText != null)
        {
            statusText.text = "Waiting for other player...";
            statusText.gameObject.SetActive(true);
        }
    }
    else
    {
        Debug.LogError("<color=red>[DEBUG]</color> OnConfirmTeamClicked: ERRO CRÍTICO! Não foi possível encontrar o PlayerNetworkData no objeto do jogador local.");
    }
}
    
public void OnLockInClicked()
{
    if (currentPreviewFreelancer == null) return;
    
    // Verifica se estamos em uma sessão de rede
    if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost))
    {
        // --- MODO ONLINE ---
        if (localPlayerSelection.Count < 5)
        {
            if (buttonConfirmSound != null) audioSource.PlayOneShot(buttonConfirmSound);
            localPlayerSelection.Add(currentPreviewFreelancer);
            bool amIAttacker = NetworkManager.Singleton.IsHost;
            var placeholders = amIAttacker ? attackerSlotPlaceholders : defenderSlotPlaceholders;
            var cardInstances = amIAttacker ? attackerCardInstances : defenderCardInstances;
            PlaceCardInSlot(currentPreviewFreelancer, placeholders[localPlayerSelection.Count - 1], cardInstances, amIAttacker);

            if (iconLookup.TryGetValue(currentPreviewFreelancer, out var icon)) { icon.SetSelectable(false); }
            currentPreviewFreelancer = null;
            UpdatePreviewCard();
            UpdateOnlineUIState();
        }
    }
    else
    {
        // --- MODO LOCAL (LÓGICA RESTAURADA) ---
        if (buttonConfirmSound != null) audioSource.PlayOneShot(buttonConfirmSound);

        if (isAttackersTurn)
        {
            if (gameSession.SelectedAttackers.Count < 5)
            {
                gameSession.AddAttacker(currentPreviewFreelancer);
                PlaceCardInSlot(currentPreviewFreelancer, attackerSlotPlaceholders[nextSlotIndex], attackerCardInstances, true);
            }
        }
        else
        {
            if (gameSession.SelectedDefenders.Count < 5)
            {
                gameSession.AddDefender(currentPreviewFreelancer);
                PlaceCardInSlot(currentPreviewFreelancer, defenderSlotPlaceholders[nextSlotIndex], defenderCardInstances, false);
            }
        }

        if (iconLookup.TryGetValue(currentPreviewFreelancer, out var icon)) { icon.SetSelectable(false); }
        currentPreviewFreelancer = null;
        UpdatePreviewCard();
        UpdateUIState(); // Chama a atualização de UI específica do modo LOCAL
    }
}

public void OnShuffleClicked()
{
    if (buttonConfirmSound != null)
    {
        audioSource.PlayOneShot(buttonConfirmSound);
    }

    // Determina a lista de freelancers que já foram selecionados por qualquer jogador
    List<FreelancerData> alreadyPickedGlobally = gameSession.SelectedAttackers.Concat(gameSession.SelectedDefenders).ToList();
    
    // Em modo online, também considera os que o jogador local já escolheu
    if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost))
    {
        alreadyPickedGlobally.AddRange(localPlayerSelection);
    }

    // Filtra a lista para pegar apenas os freelancers disponíveis
    List<FreelancerData> availableFreelancers = allFreelancers.Where(f => !alreadyPickedGlobally.Contains(f)).ToList();
    
    // Se houver freelancers disponíveis...
    if (availableFreelancers.Count > 0)
    {
        // Sorteia um freelancer aleatório da lista de disponíveis
        int randomIndex = Random.Range(0, availableFreelancers.Count);
        FreelancerData chosenFreelancer = availableFreelancers[randomIndex];

        // --- INÍCIO DA CORREÇÃO ---
        // Simula o clique no ícone do freelancer sorteado. 
        // Esta chamada irá atualizar o preview card e o highlight do ícone.
        OnFreelancerIconClicked(chosenFreelancer);
        // --- FIM DA CORREÇÃO ---

        // Se o time ainda não estiver completo, confirma a seleção do freelancer sorteado
        // (chama o método que adiciona o freelancer ao time e o remove da lista de selecionáveis)
        if (localPlayerSelection.Count < 5)
        {
            OnLockInClicked();
        }
    }
}
    
    public void OnFreelancerIconClicked(FreelancerData data)
    {
        List<FreelancerData> alreadyPicked = gameSession.SelectedAttackers.Concat(gameSession.SelectedDefenders).ToList();
        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost))
        {
            alreadyPicked.AddRange(localPlayerSelection);
        }
        if (alreadyPicked.Contains(data)) { return; }

        if (currentHighlightedIcon != null) { currentHighlightedIcon.SetHighlight(false); }
        if (iconLookup.TryGetValue(data, out FreelancerSelectionIcon newIcon)) { newIcon.SetHighlight(true); currentHighlightedIcon = newIcon; }
        
        currentPreviewFreelancer = data;
        UpdatePreviewCard();
        if (buttonClickSound != null) audioSource.PlayOneShot(buttonClickSound);
    }

private void SetupUIForOnlineMode()
{

    if (lockInButton != null)    {
        lockInButton.gameObject.SetActive(true);

        TextMeshProUGUI lockInText = lockInButton.GetComponentInChildren<TextMeshProUGUI>();
        if (lockInText != null)
        {
            lockInText.text = "Lock In";
        }
    }
    if (shuffleButton != null)
    {
        shuffleButton.gameObject.SetActive(true);
    }
    if (backButton != null)
    {
        backButton.gameObject.SetActive(true);
    }

    if (statusText != null)
    {
        statusText.gameObject.SetActive(false);
    }

    if (startBattleButton != null)
    {
        startBattleButton.gameObject.SetActive(false);
    }


    if (confirmTeamButton != null)
    {
        confirmTeamButton.gameObject.SetActive(true);
    }

    UpdateOnlineUIState();
}

    private void UpdateOnlineUIState()
{
    bool selectionComplete = localPlayerSelection.Count >= 5;
    if (lockInButton != null) lockInButton.interactable = !selectionComplete;
    if (shuffleButton != null) shuffleButton.interactable = !selectionComplete;
    
    // Altere 'startBattleButton' para 'confirmTeamButton' aqui
    if (confirmTeamButton != null)
    {
        // O botão só aparece quando a seleção estiver completa
        confirmTeamButton.gameObject.SetActive(selectionComplete);
        
        // Quando o jogador clica para confirmar, ele fica esperando, então o botão pode ser desativado
        // A linha abaixo é do seu código OnConfirmTeamClicked, então o comportamento já está correto
        // confirmTeamButton.interactable = false; 
    }
}

    private void SetupUIForLocalMode()
{
    // Garante que o botão de modo local esteja visível (inicialmente)
    if (startBattleButton != null) startBattleButton.gameObject.SetActive(true);
    // Garante que o botão de modo online esteja escondido
    if (confirmTeamButton != null) confirmTeamButton.gameObject.SetActive(false);

    if (statusText != null) statusText.gameObject.SetActive(false);
    UpdateUIState();
}
    
    private void UpdateUIState()
    {
        if (isAttackersTurn && gameSession.SelectedAttackers.Count >= 5)
        {
            isAttackersTurn = false;
        }
        nextSlotIndex = isAttackersTurn ? gameSession.SelectedAttackers.Count : gameSession.SelectedDefenders.Count;
        bool defendersDone = gameSession.SelectedDefenders.Count >= 5;

        // CORREÇÃO DO BUG .SetActive AQUI
        if (startBattleButton != null) startBattleButton.gameObject.SetActive(defendersDone);
        if (lockInButton != null) lockInButton.gameObject.SetActive(!defendersDone);
        if (shuffleButton != null) shuffleButton.gameObject.SetActive(!defendersDone);
        if (backButton != null) backButton.gameObject.SetActive(!defendersDone);
    }

    public void StartBattle()
    {
        Debug.Log("Starting local battle! Loading scene: " + battleSceneName);
        SceneManager.LoadScene(battleSceneName);
    }
    
    private void LoadAllFreelancers()
    {
        if (freelancerDatabase == null) { Debug.LogError("FreelancerDatabase not assigned!"); allFreelancers = new List<FreelancerData>(); return; }
        allFreelancers = freelancerDatabase.allFreelancers.ToList();
    }

    private void PopulateSelectionGrid()
    {
        foreach (Transform child in selectionGridContainer.transform) { Destroy(child.gameObject); }
        iconLookup.Clear();
        foreach (FreelancerData freelancer in allFreelancers)
        {
            GameObject iconGO = Instantiate(freelancerIconPrefab, selectionGridContainer.transform);
            FreelancerSelectionIcon iconScript = iconGO.GetComponent<FreelancerSelectionIcon>();
            if (iconScript != null) { iconScript.Setup(freelancer, this); iconLookup[freelancer] = iconScript; }
        }
    }

    private void PlaceCardInSlot(FreelancerData data, RectTransform slot, List<GameObject> cardInstances, bool isAttacker)
    {
        GameObject cardInstance = Instantiate(card3DPrefab, slot);
        cardInstance.transform.localRotation = Quaternion.identity;
        cardInstance.transform.localScale = new Vector3(100, 100, 100);
        if (isAttacker) { cardInstance.transform.localPosition = new Vector3(25, -35, 0); }
        else { cardInstance.transform.localPosition = new Vector3(-25, 35, 0); }
        Card3D card3D = cardInstance.GetComponent<Card3D>();
        if (card3D != null)
        {
            CardData cardData = CreateCardDataFromFreelancer(data);
            card3D.SetMaterials(null, null, null, null, null, freelancerMaterial, borderMaterial, backMaterial);
            card3D.Setup(cardData, null);
            if (card3D.portraitImage != null && data.portrait != null)
            {
                Animator animator = card3D.portraitImage.GetComponent<Animator>();
                if (animator == null) animator = card3D.portraitImage.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = data.portrait;
            }
        }
        cardInstances.Add(cardInstance);
    }
    
    private void UpdatePreviewCard()
    {
        if (previewAnimationCoroutine != null) { StopCoroutine(previewAnimationCoroutine); }
        if (currentPreviewCardInstance != null) { Destroy(currentPreviewCardInstance); }
        if (currentPreviewFreelancer == null) return;
        currentPreviewCardInstance = Instantiate(card3DPrefab, previewCardContainer.transform);
        previewAnimationCoroutine = StartCoroutine(AnimatePreviewCardCoroutine(currentPreviewCardInstance));
        Card3D card3D = currentPreviewCardInstance.GetComponent<Card3D>();
        if (card3D != null)
        {
            CardData cardData = CreateCardDataFromFreelancer(currentPreviewFreelancer);
            card3D.SetMaterials(null, null, null, null, null, freelancerMaterial, borderMaterial, backMaterial);
            card3D.Setup(cardData, null);
            if (card3D.portraitImage != null && currentPreviewFreelancer.portrait != null)
            {
                Animator animator = card3D.portraitImage.GetComponent<Animator>();
                if (animator == null) animator = card3D.portraitImage.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = currentPreviewFreelancer.portrait;
            }
        }
    }

    private IEnumerator AnimatePreviewCardCoroutine(GameObject cardInstance)
    {
        float elapsedTime = 0f;
        Vector3 startScale = Vector3.one * (1000f * 0.1f); Vector3 finalScale = Vector3.one * 1000f;
        Quaternion startRotation = Quaternion.Euler(0, 90, 0); Quaternion finalRotation = Quaternion.identity;
        cardInstance.transform.localPosition = new Vector3(0, 170, 0);
        while (elapsedTime < previewAnimationDuration)
        {
            float easedProgress = 1 - Mathf.Pow(1 - (elapsedTime / previewAnimationDuration), 3);
            cardInstance.transform.localScale = Vector3.Lerp(startScale, finalScale, easedProgress);
            cardInstance.transform.localRotation = Quaternion.Slerp(startRotation, finalRotation, easedProgress);
            elapsedTime += Time.deltaTime; yield return null;
        }
        cardInstance.transform.localScale = finalScale; cardInstance.transform.localRotation = finalRotation;
        previewAnimationCoroutine = null;
    }
    
    private CardData CreateCardDataFromFreelancer(FreelancerData data)
    {
        CardData cardData = ScriptableObject.CreateInstance<CardData>();
        cardData.cardName = data.name;
        cardData.cardType = CardType.Freelancer;
        cardData.freelancerHP = data.HP;
        cardData.weaponName = data.weaponName;
        cardData.weaponCost = FormatActionCost(data.weaponCost);
        cardData.weaponInfo = data.weaponInfo;
        
        if (data.techniques != null && data.techniques.Count > 0 && data.techniques[0] != null)
        {
            TechniqueData technique = data.techniques[0];
            cardData.techniqueName = technique.techniqueName;
            cardData.techniqueCost = FormatActionCost(technique.cost);
            cardData.techniqueInfo = technique.description;
        }

        if (data.ultimate != null)
        {
            cardData.ultimateName = data.ultimate.techniqueName;
            cardData.ultimateCost = FormatActionCost(data.ultimate.cost);
            cardData.ultimateInfo = data.ultimate.description;
        }
        cardData.footer = data.footerInfo;
        cardData.portrait = null;
        return cardData;
    }

    private string FormatActionCost(ActionCost cost)
    {
        const string a = "⚡", u = "🧰", r = "🌀";
        StringBuilder sb = new StringBuilder();
        if (cost.action > 0) { for (int i = 0; i < cost.action; i++) sb.Append(a); }
        if (cost.utility > 0) { if (sb.Length > 0) sb.Append(" "); for (int i = 0; i < cost.utility; i++) sb.Append(u); }
        if (cost.aura > 0) { if (sb.Length > 0) sb.Append(" "); for (int i = 0; i < cost.aura; i++) sb.Append(r); }
        if (sb.Length == 0) return "0";
        return sb.ToString();
    }
}