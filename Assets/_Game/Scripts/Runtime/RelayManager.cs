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

    private async void Start()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                // 用不同 Profile 区分同一台电脑的多个实例
                var options = new InitializationOptions();

                #if UNITY_EDITOR
                // Editor 里用 ParrelSync 或者随机后缀区分
                options.SetProfile("Editor_" + UnityEngine.Random.Range(0, 99999));
                #else
                // Build 版本用时间戳区分
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
            Debug.LogError(e.Message);
        }
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

            RelayServerData relayServerData = allocation.ToRelayServerData("wss");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            bool started = NetworkManager.Singleton.StartHost();
            if (!started)
            {
                SystemMessage.ShowError("Host failed to start.");
                return;
            }

            if (lobbyPanelUI != null) lobbyPanelUI.SetRoomCode(joinCode);
            if (menuFlowUI != null) menuFlowUI.ShowLobby();
            SystemMessage.ShowSuccess("Room created! Code: " + joinCode);
        }
        catch (System.Exception e)
        {
            SystemMessage.ShowError("Failed to create room.");
            Debug.LogError(e.Message);
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
            RelayServerData relayServerData = joinAllocation.ToRelayServerData("wss");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            bool started = NetworkManager.Singleton.StartClient();
            if (!started)
            {
                SystemMessage.ShowError("Failed to connect.");
                return;
            }

            SystemMessage.Show("Connecting...");
            float timeout = 10f;
            float elapsed = 0f;

            while (!NetworkManager.Singleton.IsConnectedClient && elapsed < timeout)
            {
                await System.Threading.Tasks.Task.Delay(200);
                elapsed += 0.2f;
            }

            if (!NetworkManager.Singleton.IsConnectedClient)
            {
                NetworkManager.Singleton.Shutdown();
                SystemMessage.ShowError("Connection timed out.");
                return;
            }

            if (lobbyPanelUI != null) lobbyPanelUI.SetRoomCode(joinCode);
            if (menuFlowUI != null) menuFlowUI.ShowLobby();
            SystemMessage.ShowSuccess("Joined successfully!");
        }
        catch (System.Exception e)
        {
            SystemMessage.ShowError("Invalid room code.");
            Debug.LogError(e.Message);
        }
    }

    public void PasteJoinCode()
    {
        if (joinInput != null)
            joinInput.text = GUIUtility.systemCopyBuffer.Trim().ToUpper();
    }

    public void Disconnect()
    {
        StartCoroutine(DisconnectCoroutine());
    }

    private IEnumerator DisconnectCoroutine()
    {
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

        if (menuFlowUI != null) menuFlowUI.ShowMultiplayer();
        SystemMessage.Show("Disconnected.");
    }
}