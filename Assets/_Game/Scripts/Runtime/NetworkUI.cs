using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode.Transports.UTP;

public class NetworkUI : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    [SerializeField] private Button serverBtn;

    [SerializeField] private Button hostBtn;

    [SerializeField] private Button clientBtn;

    [SerializeField] private TMP_InputField ipField;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        serverBtn.onClick.AddListener(() =>
        {
            HideUI();
            NetworkManager.Singleton.StartServer();
        });

        hostBtn.onClick.AddListener(() =>
        {
            HideUI();
            NetworkManager.Singleton.StartHost();
        });

        clientBtn.onClick.AddListener(() =>
        {
            HideUI();
            Debug.Log(ipField.text);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipField.text, 7777);
            NetworkManager.Singleton.StartClient();
        });
    }

    public void HideUI()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = true;
    }
}
