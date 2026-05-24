using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class GameOverScreen : NetworkBehaviour
{

    private CanvasGroup group;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        group = GetComponent<CanvasGroup>();

        GameplayManager.Instance.gameOver.OnValueChanged += OnGameOverChange;

        OnGameOverChange(false, GameplayManager.Instance.gameOver.Value);
    }

    private void OnGameOverChange(bool prev, bool next)
    {
        if (next)
        {
            Show();
        } else
        {
            Hide();
        }
    }

    private void Show()
    {
        group.alpha = 1.0f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    private void Hide()
    {
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }
}
