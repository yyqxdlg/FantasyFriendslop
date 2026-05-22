using TMPro;
using Unity.Netcode;
using UnityEngine;

public class EndLevelButton : NetworkBehaviour
{
    public ExitZone ExitZone;

    [SerializeField] private TMP_Text interestText;

    [SerializeField] private TMP_Text buttonText;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        OnLevelStateChanged(false, GameplayManager.Instance.levelStarted.Value);

        GameplayManager.Instance.levelStarted.OnValueChanged += OnLevelStateChanged;

        OnMinReachedChange(false, GameplayManager.Instance.minInterestReached.Value);

        GameplayManager.Instance.minInterestReached.OnValueChanged += OnMinReachedChange;
    }

    public void BtnClick()
    {
        ButtonClickServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ButtonClickServerRpc()
    {
        if (!GameplayManager.Instance.minInterestReached.Value)
        {
            GameplayManager.Instance.GuildSmiteSelective(ExitZone.GetPlayersInExitZone());
        } else
        {
            Debug.Log("Continuing to next level");

            GameplayManager.Instance.NextLevel();
        }
    }

    private void OnLevelStateChanged(bool prev, bool next)
    {
        Debug.Log("LEVEL STATE: " + next);

        if (next)
        {
            Show();
        }
        else
        {
            Hide();
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

    private void OnMinReachedChange(bool prev, bool next)
    {
        if (next)
        {
            interestText.text = "Minimum interest " + GameplayManager.Instance.GetCurrentMinInterest() + " reached";

            buttonText.text = "Next Quest?";
        }
        else
        {

            interestText.text = "Minimum interest " + GameplayManager.Instance.GetCurrentMinInterest() + " NOT reached";

            buttonText.text = "Face the Guild?";
        }
    }
}
