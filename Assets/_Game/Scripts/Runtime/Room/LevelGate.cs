using Unity.Netcode;
using UnityEngine;

public class LevelGate : NetworkBehaviour
{

	[SerializeField] private SelectPlateController plateControl;

	[SerializeField] private Collider2D blocker;

	[SerializeField] private SpriteRenderer closedVis;
	[SerializeField] private SpriteRenderer openVis;

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		GameplayManager.Instance.levelStarted.OnValueChanged += OnLevelStateChanged;

		OnLevelStateChanged(false, GameplayManager.Instance.levelStarted.Value);
	}

	public void OnLevelStateChanged(bool prev, bool next)
	{
		if (next)
		{
			OpenDoor();
		}
		else {
			CloseDoor();
		}
	}

	public void OpenDoor()
	{
		blocker.enabled = false;
		closedVis.enabled = false;
		openVis.enabled = true;
	}

	public void CloseDoor()
	{
		blocker.enabled = true;
		closedVis.enabled = true;
		openVis.enabled = false;
	}
}
