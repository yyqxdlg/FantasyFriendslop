using Unity.Netcode;
using UnityEngine;

public class StartLevelButton : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        OnLevelStateChanged(false, GameplayManager.Instance.levelStarted.Value);

        GameplayManager.Instance.levelStarted.OnValueChanged += OnLevelStateChanged;
    }

    public void BtnClick()
    {
        GameplayManager.Instance.ChangeLevelStarted(true);
    }

    private void OnLevelStateChanged(bool prev, bool next)
    {

        if (next)
        {
            Hide();
        } else
        {
            Show();
        }
    }

    private void Hide()
    {
        CanvasGroup group = GetComponentInParent<CanvasGroup>();

        group.alpha = 0f;

        group.blocksRaycasts = false;
    }

    private void Show()
    {
        CanvasGroup group = GetComponentInParent<CanvasGroup>();

        group.alpha = 1f;

        group.blocksRaycasts = true;
    }
}
