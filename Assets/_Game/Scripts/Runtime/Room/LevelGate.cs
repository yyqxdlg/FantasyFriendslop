using Unity.Netcode;
using UnityEngine;

public class LevelGate : NetworkBehaviour
{

	[SerializeField] private SelectPlateController plateControl;

	[SerializeField] private Collider2D blocker;

	[SerializeField] private SpriteRenderer closedVis;
	[SerializeField] private SpriteRenderer openVis;

	[SerializeField] private SpawnPointController spawnPointController;

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

			if (IsServer)
			{
                StartLevelServer();
            }
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

	private void StartLevelServer()
	{
        Debug.Log("STARTING LEVEL");

        spawnPointController.SpawnAll();
    }
}
