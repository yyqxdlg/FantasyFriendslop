using System.Collections;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField joinInput;
    [SerializeField] private MenuFlowUI menuFlowUI;
    [SerializeField] private LobbyPanelUI lobbyPanelUI;

    private string pendingJoinCode = "";
    private bool waitingForClientConnect = false;
    private bool isBusy = false;
    private bool callbacksRegistered = false;

    private async void Start()
    {
        RegisterNetworkCallbacks();
        await EnsureServicesReady();
    }

    private void OnEnable()
    {
        RegisterNetworkCallbacks();
    }

    private void OnDisable()
    {
        UnregisterNetworkCallbacks();
    }

    private void OnDestroy()
    {
        UnregisterNetworkCallbacks();
    }

    private void RegisterNetworkCallbacks()
    {
        if (callbacksRegistered) return;
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;

        callbacksRegistered = true;
    }

    private void UnregisterNetworkCallbacks()
    {
        if (!callbacksRegistered) return;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnTransportFailure -= OnTransportFailure;
        }

        callbacksRegistered = false;
    }

    private async Task<bool> EnsureServicesReady()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                var options = new InitializationOptions();

#if UNITY_EDITOR
                options.SetProfile("Editor_" + Random.Range(0, 99999));
#else
                options.SetProfile("Build_" + System.DateTime.Now.Ticks);
#endif

                await UnityServices.InitializeAsync(options);
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            return true;
        }
        catch (System.Exception e)
        {
            SystemMessage.ShowError("Services init failed.");
            Debug.LogError(e);
            return false;
        }
    }

    public async void CreateRelay()
    {
        if (isBusy)
        {
            SystemMessage.Show("Please wait...");
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            SystemMessage.ShowError("NetworkManager is missing.");
            return;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            SystemMessage.ShowError("Already connected. Disconnect first.");
            return;
        }

        isBusy = true;

        try
        {
            bool servicesReady = await EnsureServicesReady();
            if (!servicesReady) return;

            SystemMessage.Show("Creating room...");

            int maxConnections = Mathf.Max(1, LocalGameSettings.MaxPeople - 1);

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            string connectionType = GetRelayConnectionType();
            Debug.Log("[RelayManager] Relay connection type = " + connectionType);

            RelayServerData relayServerData = allocation.ToRelayServerData(connectionType);
            Debug.Log("[RelayManager] Relay endpoint = " + relayServerData.Endpoint);

            ApplyRelayServerData(relayServerData);

            bool started = NetworkManager.Singleton.StartHost();

            if (!started)
            {
                SystemMessage.ShowError("Host failed to start.");
                SafeShutdown();
                return;
            }

            pendingJoinCode = "";
            waitingForClientConnect = false;

            if (lobbyPanelUI != null)
                lobbyPanelUI.SetRoomCode(joinCode);

            if (menuFlowUI != null)
                menuFlowUI.ShowLobby();

            SystemMessage.ShowSuccess("Room created! Code: " + joinCode);
        }
        catch (System.Exception e)
        {
            SystemMessage.ShowError("Failed to create room.");
            Debug.LogError(e);
            SafeShutdown();
        }
        finally
        {
            isBusy = false;
        }
    }

    public async void JoinRelayFromInput()
    {
        if (isBusy)
        {
            SystemMessage.Show("Please wait...");
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            SystemMessage.ShowError("NetworkManager is missing.");
            return;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            SystemMessage.ShowError("Already connected. Disconnect first.");
            return;
        }

        string joinCode = joinInput == null ? "" : joinInput.text.Trim().ToUpper();

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            SystemMessage.ShowError("Code cannot be empty.");
            return;
        }

        isBusy = true;

        try
        {
            bool servicesReady = await EnsureServicesReady();
            if (!servicesReady) return;

            SystemMessage.Show("Joining room...");

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            string connectionType = GetRelayConnectionType();
            Debug.Log("[RelayManager] Relay connection type = " + connectionType);

            RelayServerData relayServerData = joinAllocation.ToRelayServerData(connectionType);
            Debug.Log("[RelayManager] Relay endpoint = " + relayServerData.Endpoint);

            ApplyRelayServerData(relayServerData);

            pendingJoinCode = joinCode;
            waitingForClientConnect = true;

            bool started = NetworkManager.Singleton.StartClient();

            if (!started)
            {
                waitingForClientConnect = false;
                pendingJoinCode = "";
                SystemMessage.ShowError("Failed to connect.");
                SafeShutdown();
                return;
            }

            SystemMessage.Show("Connecting...");
        }
        catch (System.Exception e)
        {
            waitingForClientConnect = false;
            pendingJoinCode = "";
            SystemMessage.ShowError("Invalid room code or connection failed.");
            Debug.LogError(e);
            SafeShutdown();
        }
        finally
        {
            isBusy = false;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!waitingForClientConnect) return;
        if (NetworkManager.Singleton == null) return;
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        waitingForClientConnect = false;

        if (lobbyPanelUI != null)
            lobbyPanelUI.SetRoomCode(pendingJoinCode);

        if (menuFlowUI != null)
            menuFlowUI.ShowLobby();

        SystemMessage.ShowSuccess("Joined successfully!");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        if (waitingForClientConnect)
        {
            waitingForClientConnect = false;
            pendingJoinCode = "";

            if (menuFlowUI != null)
                menuFlowUI.ShowMultiplayer();

            SystemMessage.ShowError("Connection failed or was disconnected.");
        }
    }

    private void OnTransportFailure()
    {
        Debug.LogError("[RelayManager] Transport failure. Relay allocation must be recreated.");

        waitingForClientConnect = false;
        pendingJoinCode = "";
        isBusy = false;

        SafeShutdown();

        if (menuFlowUI != null)
            menuFlowUI.ShowMultiplayer();

        SystemMessage.ShowError("Connection to Relay failed. Please create or join a new room.");
    }

    public void PasteJoinCode()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (joinInput != null)
        {
            joinInput.ActivateInputField();
            SystemMessage.Show("Please paste manually with Ctrl+V / Cmd+V.");
        }
#else
        if (joinInput != null)
            joinInput.text = GUIUtility.systemCopyBuffer.Trim().ToUpper();
#endif
    }

    public void Disconnect()
    {
        StartCoroutine(DisconnectCoroutine());
    }

    private IEnumerator DisconnectCoroutine()
    {
        waitingForClientConnect = false;
        pendingJoinCode = "";
        isBusy = false;

        SafeShutdown();

        yield return new WaitForSeconds(0.5f);

        if (menuFlowUI != null)
            menuFlowUI.ShowMultiplayer();

        SystemMessage.Show("Disconnected.");
    }

    private void SafeShutdown()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (NetworkManager.Singleton.IsListening ||
            NetworkManager.Singleton.IsHost ||
            NetworkManager.Singleton.IsClient ||
            NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    private string GetRelayConnectionType()
    {
#if UNITY_WEBGL
        return "wss";
#else
        return "dtls";
#endif
    }

    private void ApplyRelayServerData(RelayServerData relayServerData)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[RelayManager] NetworkManager is missing.");
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (transport == null)
        {
            Debug.LogError("[RelayManager] UnityTransport is missing on NetworkManager.");
            return;
        }

        transport.SetRelayServerData(relayServerData);
    }
}