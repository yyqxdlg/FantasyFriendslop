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

    [Header("Connection")]
    [SerializeField] private ushort port = 7777;

    private void Awake()
    {
        networkCanvasGroup = GetComponent<CanvasGroup>();
        inGameCanvasGroup = inGameCanvasObject.GetComponent<CanvasGroup>();

        serverBtn.onClick.AddListener(StartServerClicked);
        hostBtn.onClick.AddListener(StartHostClicked);
        clientBtn.onClick.AddListener(StartClientClicked);
    }

    private void StartServerClicked()
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // Listen on all local network interfaces.
        // This matters for other computers on the same LAN.
        transport.SetConnectionData("0.0.0.0", port, "0.0.0.0");

        bool started = NetworkManager.Singleton.StartServer();

        if (started)
        {
            InGameUIMode();
            Debug.Log("Server started on port " + port);
        }
        else
        {
            Debug.LogError("Failed to start server.");
        }
    }

    private void StartHostClicked()
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // Host must listen on LAN, not only localhost.
        transport.SetConnectionData("0.0.0.0", port, "0.0.0.0");

        bool started = NetworkManager.Singleton.StartHost();

        if (started)
        {
            InGameUIMode();
            Debug.Log("Host started on port " + port);
        }
        else
        {
            Debug.LogError("Failed to start host.");
        }
    }

    private void StartClientClicked()
    {
        string ip = ipField.text.Trim();

        // Do not type "192.168.1.10:7777" into the field.
        // Type only "192.168.1.10". The port is handled separately.
        if (string.IsNullOrWhiteSpace(ip))
        {
            Debug.LogError("IP field is empty. Enter the Host computer's IPv4 address.");
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // Client connects to the host's LAN IPv4.
        transport.SetConnectionData(ip, port);

        bool started = NetworkManager.Singleton.StartClient();

        if (started)
        {
            InGameUIMode();
            Debug.Log("Client trying to connect to " + ip + ":" + port);
        }
        else
        {
            Debug.LogError("Failed to start client.");
        }
    }

    public void InGameUIMode()
    {
        networkCanvasGroup.alpha = 0;
        networkCanvasGroup.blocksRaycasts = false;

        inGameCanvasGroup.alpha = 1;
        inGameCanvasGroup.blocksRaycasts = false;
    }
}