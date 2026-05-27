using System.Collections;
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

    private async void Start()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                var options = new InitializationOptions();

#if UNITY_EDITOR
                options.SetProfile("Editor_" + UnityEngine.Random.Range(0, 99999));
#else
                options.SetProfile("Build_" + System.DateTime.Now.Ticks);
#endif

                await UnityServices.InitializeAsync(options);
            }

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (System.Exception e)
        {
            SystemMessage.ShowError("Services init failed.");
            Debug.LogError(e);
        }
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
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

    public async void CreateRelay()
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            SystemMessage.ShowError("Already connected. Disconnect first.");
            return;
        }

        try
        {
            SystemMessage.Show("Creating room...");

            int maxConnections = Mathf.Max(1, LocalGameSettings.MaxPeople - 1);

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            string connectionType = GetRelayConnectionType();
            Debug.Log("Relay connection type = " + connectionType);

            RelayServerData relayServerData = allocation.ToRelayServerData(connectionType);
            ApplyRelayServerData(relayServerData);

            bool started = NetworkManager.Singleton.StartHost();
            if (!started)
            {
                SystemMessage.ShowError("Host failed to start.");
                return;
            }

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
        }
    }

    public async void JoinRelayFromInput()
    {
        string joinCode = joinInput == null ? "" : joinInput.text.Trim().ToUpper();

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            SystemMessage.ShowError("Code cannot be empty.");
            return;
        }

        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            SystemMessage.ShowError("Already connected. Disconnect first.");
            return;
        }

        try
        {
            SystemMessage.Show("Joining room...");

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            string connectionType = GetRelayConnectionType();
            Debug.Log("Relay connection type = " + connectionType);

            RelayServerData relayServerData = joinAllocation.ToRelayServerData(connectionType);
            ApplyRelayServerData(relayServerData);

            bool started = NetworkManager.Singleton.StartClient();
            if (!started)
            {
                SystemMessage.ShowError("Failed to connect.");
                return;
            }

            pendingJoinCode = joinCode;
            waitingForClientConnect = true;

            SystemMessage.Show("Connecting...");
        }
        catch (System.Exception e)
        {
            waitingForClientConnect = false;
            SystemMessage.ShowError("Invalid room code.");
            Debug.LogError(e);
        }
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

        if (NetworkManager.Singleton != null &&
            (NetworkManager.Singleton.IsHost ||
             NetworkManager.Singleton.IsClient ||
             NetworkManager.Singleton.IsServer))
        {
            NetworkManager.Singleton.Shutdown();
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            yield return null;
        }

        if (menuFlowUI != null)
            menuFlowUI.ShowMultiplayer();

        SystemMessage.Show("Disconnected.");
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
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (transport == null)
        {
            Debug.LogError("UnityTransport is missing on NetworkManager.");
            return;
        }

        transport.SetRelayServerData(relayServerData);
    }
}