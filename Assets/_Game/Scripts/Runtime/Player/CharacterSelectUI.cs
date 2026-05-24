using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour
{
    [Header("Panel flow")]
    [SerializeField] private MenuFlowUI menuFlowUI;

    [Header("Preview image")]
    [SerializeField] private Image heroPreviewImage;
    [SerializeField] private Sprite yellowSprite;
    [SerializeField] private Sprite greenSprite;
    [SerializeField] private Sprite blueSprite;
    [SerializeField] private Sprite redSprite;
    [SerializeField] private Sprite noneSprite;

    [Header("Hero buttons")]
    [SerializeField] private Button yellowButton;
    [SerializeField] private Button greenButton;
    [SerializeField] private Button blueButton;
    [SerializeField] private Button redButton;

    [Header("Taken-by labels")]
    [SerializeField] private TMP_Text yellowTakenText;
    [SerializeField] private TMP_Text greenTakenText;
    [SerializeField] private TMP_Text blueTakenText;
    [SerializeField] private TMP_Text redTakenText;

    [Header("UI")]
    [SerializeField] private TMP_Text selectedHeroText;
    [SerializeField] private TMP_Text messageText;

    private int pendingHeroId = -1;

    private void OnEnable()
    {
        pendingHeroId = LobbyNetworkState.Instance != null
            ? LobbyNetworkState.Instance.GetLocalPlayerHeroId()
            : -1;
        Subscribe();
        RefreshAll();
        RefreshPreview();
    }

    private void OnDisable()
    {
        Unsubscribe();
        ClearMessage();
    }

    public void SelectYellow() => SelectHero(0);
    public void SelectGreen() => SelectHero(1);
    public void SelectBlue() => SelectHero(2);
    public void SelectRed() => SelectHero(3);

    public void SelectHero(int heroId)
    {
        pendingHeroId = heroId;
        RefreshPreview();
    }

    public void CloseWithoutSaving()
    {
        pendingHeroId = LobbyNetworkState.Instance != null
            ? LobbyNetworkState.Instance.GetLocalPlayerHeroId()
            : -1;
        if (menuFlowUI != null) menuFlowUI.CloseCharacterSelect();
    }

    public void ConfirmSelection()
    {
        if (pendingHeroId < 0)
        {
            SetMessage("Choose a hero first.");
            return;
        }

        if (LobbyNetworkState.Instance == null)
        {
            SetMessage("Lobby state is missing.");
            return;
        }

        LobbyNetworkState.Instance.RequestSelectHero(pendingHeroId);
    }

    private void RefreshAll()
    {
        RefreshTakenLabels();
        RefreshButtonStates();
    }

private void RefreshPreview()
{
    if (heroPreviewImage == null) return;

    Sprite sprite = null;
    switch (pendingHeroId)
    {
        case 0: sprite = yellowSprite; break;
        case 1: sprite = greenSprite; break;
        case 2: sprite = blueSprite; break;
        case 3: sprite = redSprite; break;
        default: sprite = noneSprite; break;
    }

    // 没有 sprite 就隐藏整个 Image
    heroPreviewImage.gameObject.SetActive(sprite != null);
    heroPreviewImage.sprite = sprite;

    if (sprite != null)
    {
        AspectRatioFitter fitter = heroPreviewImage.GetComponent<AspectRatioFitter>();
        if (fitter != null)
            fitter.aspectRatio = (float)sprite.rect.width / sprite.rect.height;
    }

    if (selectedHeroText != null)
    {
        selectedHeroText.text = LobbyNetworkState.Instance != null
            ? LobbyNetworkState.Instance.GetHeroDisplayName(pendingHeroId)
            : "";
    }
}

    private void RefreshTakenLabels()
    {
        if (LobbyNetworkState.Instance == null) return;
        SetTakenText(yellowTakenText, LobbyNetworkState.Instance.GetHeroTakenText(0));
        SetTakenText(greenTakenText, LobbyNetworkState.Instance.GetHeroTakenText(1));
        SetTakenText(blueTakenText, LobbyNetworkState.Instance.GetHeroTakenText(2));
        SetTakenText(redTakenText, LobbyNetworkState.Instance.GetHeroTakenText(3));
    }

    private void RefreshButtonStates()
    {
        if (LobbyNetworkState.Instance == null || NetworkManager.Singleton == null) return;

        bool allowSame = LobbyNetworkState.Instance.allowSameHero.Value;
        ulong localId = NetworkManager.Singleton.LocalClientId;

        SetButtonInteractable(yellowButton, allowSame || !LobbyNetworkState.Instance.IsHeroTakenByOtherClient(0, localId));
        SetButtonInteractable(greenButton, allowSame || !LobbyNetworkState.Instance.IsHeroTakenByOtherClient(1, localId));
        SetButtonInteractable(blueButton, allowSame || !LobbyNetworkState.Instance.IsHeroTakenByOtherClient(2, localId));
        SetButtonInteractable(redButton, allowSame || !LobbyNetworkState.Instance.IsHeroTakenByOtherClient(3, localId));
    }

    private void SetTakenText(TMP_Text label, string names)
    {
        if (label == null) return;
        label.text = string.IsNullOrWhiteSpace(names) ? "" : names;
    }

    private void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null) button.interactable = interactable;
    }

    private void Subscribe()
    {
        if (LobbyNetworkState.Instance == null) return;
        LobbyNetworkState.Instance.Players.OnListChanged += OnPlayersChanged;
        LobbyNetworkState.Instance.allowSameHero.OnValueChanged += OnAllowSameHeroChanged;
        LobbyNetworkState.Instance.OnSelectionResponse += OnSelectionResponse;
    }

    private void Unsubscribe()
    {
        if (LobbyNetworkState.Instance == null) return;
        LobbyNetworkState.Instance.Players.OnListChanged -= OnPlayersChanged;
        LobbyNetworkState.Instance.allowSameHero.OnValueChanged -= OnAllowSameHeroChanged;
        LobbyNetworkState.Instance.OnSelectionResponse -= OnSelectionResponse;
    }

    private void OnPlayersChanged(NetworkListEvent<PlayerLobbyData> changeEvent)
    {
        RefreshAll();
    }

    private void OnAllowSameHeroChanged(bool oldValue, bool newValue)
    {
        RefreshButtonStates();
    }

    private void OnSelectionResponse(bool success, string message)
    {
        if (this == null || !gameObject.activeInHierarchy) return; 
        SetMessage(message);
        if (success && menuFlowUI != null)
            menuFlowUI.CloseCharacterSelect();
    }
    private void OnDestroy()
    {
        Unsubscribe();
    }
    private void SetMessage(string message)
    {
        if (messageText != null) messageText.text = message;
    }

    private void ClearMessage()
    {
        SetMessage("");
    }
}