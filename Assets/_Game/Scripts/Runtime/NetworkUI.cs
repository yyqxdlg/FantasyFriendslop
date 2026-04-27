using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode.Transports.UTP;

public class NetworkUI : MonoBehaviour
{
    private CanvasGroup networkCanvasGroup;

    public GameObject inGameCanvasObject;

    private CanvasGroup inGameCanvasGroup;

    [SerializeField] private Button serverBtn;

    [SerializeField] private Button hostBtn;

    [SerializeField] private Button clientBtn;

    [SerializeField] private TMP_InputField ipField;

    [SerializeField] private CharacterSelectUI characterSelectUI;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        networkCanvasGroup = GetComponent<CanvasGroup>();

        inGameCanvasGroup = inGameCanvasObject.GetComponent<CanvasGroup>();

        serverBtn.onClick.AddListener(() =>
        {
            InGameUIMode();

            //NetworkManager.Singleton.NetworkConfig.PlayerPrefab = getChosenClassPrefab();

            NetworkManager.Singleton.StartServer();
        });

        hostBtn.onClick.AddListener(() =>
        {
            InGameUIMode();

            //NetworkManager.Singleton.NetworkConfig.PlayerPrefab = getChosenClassPrefab();

           GameObject playerPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;

            Debug.Log($"PlayerPrefab is null = {playerPrefab == null}");

            if (playerPrefab != null)
            {
                Debug.Log($"PlayerPrefab name = {playerPrefab.name}");
            }

            bool started = NetworkManager.Singleton.StartHost();

            Debug.Log($"StartHost result = {started}");
                });

        clientBtn.onClick.AddListener(() =>
        {
            InGameUIMode();
            Debug.Log(ipField.text);

            //NetworkManager.Singleton.NetworkConfig.PlayerPrefab = getChosenClassPrefab();

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipField.text, 7777);
            NetworkManager.Singleton.StartClient();
        });
    }

    public void InGameUIMode()
    {
        networkCanvasGroup.alpha = 0;
        networkCanvasGroup.blocksRaycasts = true;

        inGameCanvasGroup.alpha = 1;
        inGameCanvasGroup.blocksRaycasts = false;
    }
}
