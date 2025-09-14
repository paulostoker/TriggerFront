// _Scripts/Networking/NetworkConnectManager.cs (VERSÃO CORRIGIDA)
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkConnectManager : MonoBehaviour
{
    public static NetworkConnectManager Instance { get; private set; }

    [Header("Scene Configuration")]
    [SerializeField] private string characterSelectSceneName = "CharacterSelect";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        await UnityServices.InitializeAsync();
        
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Player signed in with ID: {AuthenticationService.Instance.PlayerId}");
        }
    }
    
    public async Task<string> CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // **MUDANÇA CRÍTICA 1:** Adicionamos um listener para o evento de conexão.
            // A cena só vai mudar QUANDO este evento for disparado.
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;

            // **MUDANÇA CRÍTICA 2:** Iniciamos o Host, mas NÃO carregamos a cena aqui.
            NetworkManager.Singleton.StartHost();
            
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay create failed: {e.Message}");
            return null;
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        // Verifica se quem conectou não é o próprio host (clientId 0) e se somos o host
        if (clientId != NetworkManager.Singleton.LocalClientId && NetworkManager.Singleton.IsHost)
        {
            Debug.Log($"Client has connected with ID: {clientId}. Host is now loading the scene.");
            
            // Remove o listener para não ser chamado novamente caso outro cliente tente conectar
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;

            // Agora, com o cliente conectado, o Host carrega a cena para todos.
            NetworkManager.Singleton.SceneManager.LoadScene(characterSelectSceneName, LoadSceneMode.Single);
        }
    }
    
    public async Task JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log($"Joining Relay with code: {joinCode}");
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );
            
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay join failed: {e.Message}");
        }
    }
}