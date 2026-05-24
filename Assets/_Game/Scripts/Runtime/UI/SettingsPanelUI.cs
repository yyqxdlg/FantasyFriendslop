using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelUI : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Toggle allowSameHeroToggle;

    [Header("Messages")]
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private float errorHideDelay = 2f;

    [Header("Panel flow")]
    [SerializeField] private MenuFlowUI menuFlowUI;

    private Coroutine hideErrorCoroutine;

    private void OnEnable()
    {
        LoadCurrentSettingsToUI();
    }

    private void OnDisable()
    {
        ClearErrorImmediately();
    }

    private void LoadCurrentSettingsToUI()
    {
        if (nameInput != null)
        {
            // This is where the current saved name is displayed.
            nameInput.text = LocalGameSettings.PlayerName;
        }

        if (allowSameHeroToggle != null)
        {
            // On = allow multiple players to pick the same hero.
            // Off = each hero can only be picked by one player.
            allowSameHeroToggle.isOn = LocalGameSettings.AllowSameHero;
        }

        ClearErrorImmediately();
    }

    public void Cancel()
    {
        // Discard current edits and return to main menu.
        LoadCurrentSettingsToUI();

        if (menuFlowUI != null)
        {
            menuFlowUI.ShowMainMenu();
        }
    }

    public void Confirm()
    {
        string playerName = nameInput == null ? "" : nameInput.text.Trim();

        if (string.IsNullOrWhiteSpace(playerName))
        {
            ShowError("Name cannot be empty.");
            return;
        }

        LocalGameSettings.PlayerName = playerName;
        LocalGameSettings.AllowSameHero = allowSameHeroToggle != null && allowSameHeroToggle.isOn;
        PlayerPrefs.Save();

        // If the player edits settings while already connected, update the lobby name too.
        if (LobbyNetworkState.Instance != null && LobbyNetworkState.Instance.IsClient)
        {
            LobbyNetworkState.Instance.RegisterLocalPlayerName();
        }

        ClearErrorImmediately();

        if (menuFlowUI != null)
        {
            menuFlowUI.ShowMainMenu();
        }
    }

    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
        }

        if (hideErrorCoroutine != null)
        {
            StopCoroutine(hideErrorCoroutine);
        }

        hideErrorCoroutine = StartCoroutine(HideErrorAfterDelay());
    }

    private IEnumerator HideErrorAfterDelay()
    {
        yield return new WaitForSeconds(errorHideDelay);
        ClearErrorImmediately();
    }

    private void ClearErrorImmediately()
    {
        if (hideErrorCoroutine != null)
        {
            StopCoroutine(hideErrorCoroutine);
            hideErrorCoroutine = null;
        }

        if (errorText != null)
        {
            errorText.text = "";
            errorText.gameObject.SetActive(false);
        }
    }
}
