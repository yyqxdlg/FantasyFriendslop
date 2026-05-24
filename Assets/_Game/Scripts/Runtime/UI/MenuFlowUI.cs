using TMPro;
using UnityEngine;

public class MenuFlowUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingPanel;
    public GameObject multiplayerPanel;
    public GameObject joinCodePanel;
    public GameObject lobbyPanel;
    public GameObject characterSelectPanel;

    [Header("Optional message text")]
    [SerializeField] private TMP_Text messageText;

    private void Start()
    {
        ShowMainMenu();
    }

    private void HideAll()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (settingPanel) settingPanel.SetActive(false);
        if (multiplayerPanel) multiplayerPanel.SetActive(false);
        if (joinCodePanel) joinCodePanel.SetActive(false);
        if (lobbyPanel) lobbyPanel.SetActive(false);
        if (characterSelectPanel) characterSelectPanel.SetActive(false);
    }

    public void ShowMainMenu()
    {
        HideAll();
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
        SetMessage("");
    }

    public void ShowSettings()
    {
        HideAll();
        if (settingPanel) settingPanel.SetActive(true);
        SetMessage("");
    }

    public void ShowMultiplayer()
    {
        HideAll();
        if (multiplayerPanel) multiplayerPanel.SetActive(true);
        SetMessage("");
    }

    public void ShowJoinCode()
    {
        HideAll();
        // if (multiplayerPanel) multiplayerPanel.SetActive(true);
        if (joinCodePanel) joinCodePanel.SetActive(true);
        SetMessage("");
    }

    public void CloseJoinCode()
    {
        HideAll();
        if (multiplayerPanel) multiplayerPanel.SetActive(true);
        SetMessage("");
    }

    public void ShowLobby()
    {
        HideAll();
        if (lobbyPanel) lobbyPanel.SetActive(true);
        SetMessage("");
    }

    public void ShowCharacterSelect()
    {
        if (characterSelectPanel) characterSelectPanel.SetActive(true);
        SetMessage("");
    }

    public void CloseCharacterSelect()
    {
        if (characterSelectPanel) characterSelectPanel.SetActive(false);
        SetMessage("");
    }

    public void SetMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }
    public void ExitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

}
