using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

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

    private void OnEnable()
    {
        Subscribe();
        if (localPlayerNameText != null)
            localPlayerNameText.text = LocalGameSettings.PlayerName;
        if (LobbyNetworkState.Instance != null && LobbyNetworkState.Instance.IsSpawned)
            Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }
    private void OnDestroy() // ← 新加
    {
        Unsubscribe();
    }
    private void Update()
    {
        if (startGameButton == null || NetworkManager.Singleton == null) return;

        bool isHost = NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;
        startGameButton.gameObject.SetActive(isHost);
        startGameButton.interactable = isHost
            && LobbyNetworkState.Instance != null
            && LobbyNetworkState.Instance.AllRegisteredPlayersSelectedLocal();
    }

    public void SetRoomCode(string code)
    {
        currentRoomCode = code;
        if (roomCodeText != null) roomCodeText.text = code;
    }

    public void CopyRoomCode()
    {
        if (string.IsNullOrWhiteSpace(currentRoomCode))
        {
            SetMessage("No room code yet.");
            return;
        }
        GUIUtility.systemCopyBuffer = currentRoomCode;
        SetMessage("Room code copied!");
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
        if (playerListContainer == null || playerRowPrefab == null) return;

        // 清掉旧的行
        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);

        if (LobbyNetworkState.Instance == null) return;
        if (localPlayerNameText != null)
            localPlayerNameText.text = LocalGameSettings.PlayerName;
        for (int i = 0; i < LobbyNetworkState.Instance.Players.Count; i++)
        {
            PlayerLobbyData data = LobbyNetworkState.Instance.Players[i];
            GameObject row = Instantiate(playerRowPrefab, playerListContainer);

            // 设置英雄图标
            Image heroIcon = row.transform.Find("HeroIcon")?.GetComponent<Image>();
            if (heroIcon != null)
            {
                int heroId = data.HeroId;
              
                if (heroId >= 0 && heroId < heroSprites.Length && heroSprites[heroId] != null)
                    heroIcon.sprite = heroSprites[heroId];
                else
                    heroIcon.sprite = notSelectedSprite;
                
                if (heroIcon.sprite != null)
                {
                    AspectRatioFitter fitter = heroIcon.GetComponent<AspectRatioFitter>();
                    if (fitter != null)
                        fitter.aspectRatio = (float)heroIcon.sprite.rect.width / heroIcon.sprite.rect.height;
                }
            }

            // 设置名字和英雄状态
            TMP_Text nameText = row.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null)
            {
                string heroName = LobbyNetworkState.Instance.GetHeroDisplayName(data.HeroId);
                nameText.text = $"{data.PlayerName}  {heroName}";
            }
        }
    }

    private void Subscribe()
    {
        if (LobbyNetworkState.Instance == null) return;
        LobbyNetworkState.Instance.Players.OnListChanged += OnPlayersChanged;
        LobbyNetworkState.Instance.OnLobbyMessage += SetMessage;
        LobbyNetworkState.Instance.OnPlayersReady += Refresh;
    }

    private void Unsubscribe()
    {
        if (LobbyNetworkState.Instance == null) return;
        LobbyNetworkState.Instance.Players.OnListChanged -= OnPlayersChanged;
        LobbyNetworkState.Instance.OnLobbyMessage -= SetMessage;
        LobbyNetworkState.Instance.OnPlayersReady -= Refresh;
    }

    private void OnPlayersChanged(NetworkListEvent<PlayerLobbyData> changeEvent)
    {
        Refresh();
    }

    private void SetMessage(string message)
    {
        if (messageText != null) messageText.text = message;
    }
}