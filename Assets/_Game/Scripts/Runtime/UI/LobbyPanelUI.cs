using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LobbyPanelUI : MonoBehaviour
{
    [Header("Room code")]
    [SerializeField] private TMP_Text roomCodeText;

    [Header("Local player")]
    [SerializeField] private TMP_Text localPlayerNameText;

    [Header("Player list")]
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerRowPrefab;

    [Header("Hero icons — order match HeroId 0/1/2/3")]
    [SerializeField] private Sprite[] heroSprites;
    [SerializeField] private Sprite notSelectedSprite;

    [Header("Lobby info")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button startGameButton;

    private string currentRoomCode = "";
    private bool refreshQueued = false;
    private bool subscribed = false;

    private void OnEnable()
    {
        Subscribe();

        if (localPlayerNameText != null)
            localPlayerNameText.text = LocalGameSettings.PlayerName;

        if (LobbyNetworkState.Instance != null && LobbyNetworkState.Instance.IsSpawned)
            QueueRefresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (startGameButton == null || NetworkManager.Singleton == null)
            return;

        bool isHost = NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;

        startGameButton.gameObject.SetActive(isHost);

        startGameButton.interactable =
            isHost &&
            LobbyNetworkState.Instance != null &&
            LobbyNetworkState.Instance.AllRegisteredPlayersSelectedLocal();
    }

    public void SetRoomCode(string code)
    {
        currentRoomCode = code;

        if (roomCodeText != null)
            roomCodeText.text = code;
    }

    public void CopyRoomCode()
    {
        if (string.IsNullOrWhiteSpace(currentRoomCode) && roomCodeText != null)
            currentRoomCode = roomCodeText.text.Trim();

        if (string.IsNullOrWhiteSpace(currentRoomCode))
        {
            SetMessage("No room code yet.");
            return;
        }

        WebClipboard.Copy(currentRoomCode);
        SetMessage("Room code copied: " + currentRoomCode);
    }

    public void StartGame()
    {
        if (LobbyNetworkState.Instance == null)
        {
            SetMessage("Lobby state is missing.");
            return;
        }

        LobbyNetworkState.Instance.RequestStartGame();
    }

    public void Refresh()
    {
        Debug.Log("[LobbyPanelUI] Refresh START");

        if (playerListContainer == null)
        {
            Debug.LogError("[LobbyPanelUI] playerListContainer is null");
            return;
        }

        if (playerRowPrefab == null)
        {
            Debug.LogError("[LobbyPanelUI] playerRowPrefab is null");
            return;
        }

        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }

        if (LobbyNetworkState.Instance == null)
        {
            Debug.LogError("[LobbyPanelUI] LobbyNetworkState.Instance is null");
            return;
        }

        Debug.Log("[LobbyPanelUI] Player count: " + LobbyNetworkState.Instance.Players.Count);

        for (int i = 0; i < LobbyNetworkState.Instance.Players.Count; i++)
        {
            Debug.Log("[LobbyPanelUI] Creating row " + i);

            PlayerLobbyData data = LobbyNetworkState.Instance.Players[i];

            GameObject row = Instantiate(playerRowPrefab, playerListContainer);

            Image heroIcon = row.transform.Find("HeroIcon")?.GetComponent<Image>();
            TMP_Text nameText = row.transform.Find("NameText")?.GetComponent<TMP_Text>();

            if (nameText != null)
            {
                string heroName = LobbyNetworkState.Instance.GetHeroDisplayName(data.HeroId);
                nameText.text = $"{data.PlayerName}  {heroName}";
            }
            else
            {
                Debug.LogWarning("[LobbyPanelUI] NameText missing in PlayerRow prefab.");
            }

            if (heroIcon != null)
            {
                Sprite iconSprite = notSelectedSprite;

                if (data.HeroId >= 0 &&
                    heroSprites != null &&
                    data.HeroId < heroSprites.Length &&
                    heroSprites[data.HeroId] != null)
                {
                    iconSprite = heroSprites[data.HeroId];
                }

                heroIcon.sprite = iconSprite;
                heroIcon.enabled = iconSprite != null;
                heroIcon.preserveAspect = true;

                Color c = heroIcon.color;
                c.a = 1f;
                heroIcon.color = c;

                Debug.Log($"[LobbyPanelUI] Row {i}: player={data.PlayerName}, heroId={data.HeroId}, icon={(iconSprite != null ? iconSprite.name : "null")}");
            }
            else
            {
                Debug.LogWarning("[LobbyPanelUI] HeroIcon missing in PlayerRow prefab.");
            }
        }

        Debug.Log("[LobbyPanelUI] Refresh DONE");
    }

    private void Subscribe()
    {
        if (subscribed)
            return;

        if (LobbyNetworkState.Instance == null)
            return;

        LobbyNetworkState.Instance.Players.OnListChanged += OnPlayersChanged;
        LobbyNetworkState.Instance.OnLobbyMessage += SetMessage;
        LobbyNetworkState.Instance.OnPlayersReady += QueueRefresh;

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        if (LobbyNetworkState.Instance == null)
        {
            subscribed = false;
            return;
        }

        LobbyNetworkState.Instance.Players.OnListChanged -= OnPlayersChanged;
        LobbyNetworkState.Instance.OnLobbyMessage -= SetMessage;
        LobbyNetworkState.Instance.OnPlayersReady -= QueueRefresh;

        subscribed = false;
    }

    private void OnPlayersChanged(NetworkListEvent<PlayerLobbyData> changeEvent)
    {
        QueueRefresh();
    }

    private void QueueRefresh()
    {
        if (!isActiveAndEnabled)
            return;

        if (refreshQueued)
            return;

        StartCoroutine(RefreshNextFrame());
    }

    private IEnumerator RefreshNextFrame()
    {
        refreshQueued = true;
        yield return null;
        refreshQueued = false;

        Refresh();
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }
}