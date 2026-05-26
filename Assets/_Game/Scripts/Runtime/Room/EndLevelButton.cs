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

		GameplayManager.Instance.levelStarted.OnValueChanged += OnLevelStateChanged;

        GameplayManager.Instance.currentMinInterest.OnValueChanged += OnInterestDisplayChanged;

        GameplayManager.Instance.exitZoneGold.OnValueChanged += OnInterestDisplayChanged;

		Invoke("DelayedInitialUpdate", 0.1f);
	}

	public void DelayedInitialUpdate()
	{
        OnLevelStateChanged(false, GameplayManager.Instance.levelStarted.Value);

        OnInterestDisplayChanged(0, GameplayManager.Instance.GetCurrentMinInterest());
    }

	public void BtnClick()
	{
		if (GameplayManager.Instance.MinInterestReached())
		{
			if (GameplayManager.Instance.allLivingSafe.Value)
			{
				ConfirmClickServerRpc();
			} else
			{
				ConfirmScreenUnsafe();
			}
		} else
		{
			ConfirmScreenGuild();
		}
	}

	private void ConfirmScreenGuild()
	{
		ConfirmNextLevelCanvas.Instance.SetText("Are you sure? The guild will be DISPLEASED.");

		ConfirmNextLevelCanvas.Instance.SetOnclickEvent(this);

		ConfirmNextLevelCanvas.Instance.Show();
	}

	private void ConfirmScreenUnsafe()
	{
        ConfirmNextLevelCanvas.Instance.SetText("Are you sure? The treasure of allies still in the dungeon will be lost.");

        ConfirmNextLevelCanvas.Instance.SetOnclickEvent(this);

        ConfirmNextLevelCanvas.Instance.Show();
    }

	public void ConfirmClick()
	{
        ConfirmClickServerRpc();
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void ConfirmClickServerRpc()
	{
		if (!GameplayManager.Instance.MinInterestReached())
		{
			GameplayManager.Instance.GuildSmiteSelective(ExitZone.GetPlayersInExitZone());
		}
		else
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

	private void OnInterestDisplayChanged(int prev, int next)
	{
		if (GameplayManager.Instance.MinInterestReached())
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
