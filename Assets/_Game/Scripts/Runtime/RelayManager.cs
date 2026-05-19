using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class RelayManager : MonoBehaviour
{
    [SerializeField] Button hostBtn;
    [SerializeField] Button joinBtn;
    [SerializeField] TMP_InputField joinInput;

    [SerializeField] TMP_Text joinCodeDisplay;

    [SerializeField] CanvasGroup relayCanvasGroup;

    [SerializeField] CanvasGroup inGameCanvasGroup;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void Start()
    {
        await UnityServices.InitializeAsync();

        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        hostBtn.onClick.AddListener(CreateRelay);

        joinBtn.onClick.AddListener(() => JoinRelay(joinInput.text));
    }

    async void CreateRelay()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        Debug.Log(joinCode);

        var relayServerData = allocation.ToRelayServerData("wss");

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartHost();

        InGameUIMode();

        joinCodeDisplay.text = joinCode;
    }

    async void JoinRelay(string joinCode)
    {
        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        var relayServerData = joinAllocation.ToRelayServerData("wss");

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        Debug.Log("Start client with code: " + joinCode);

        NetworkManager.Singleton.StartClient();

        InGameUIMode();

        joinCodeDisplay.text = joinCode;
    }

    public void InGameUIMode()
    {
        relayCanvasGroup.alpha = 0;
        relayCanvasGroup.blocksRaycasts = false;

        inGameCanvasGroup.alpha = 1;
        inGameCanvasGroup.blocksRaycasts = true;
    }
}
