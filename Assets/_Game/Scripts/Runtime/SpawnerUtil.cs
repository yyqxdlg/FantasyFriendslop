using System;
using Unity.Netcode;
using UnityEngine;

public class SpawnerUtil : NetworkBehaviour
{
	public Transform[] spawnables;
	public string[] spawnablesNames;
	public static SpawnerUtil Instance { get; private set; }

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
	}

	public void NetworkSpawnGameObject(string spawnableName, Vector2 spawnPos, ulong spawnerClientId, ulong creatorObjectNetworkId)
	{
		SpawnObjectServerRpc(spawnableName, spawnPos, spawnerClientId, creatorObjectNetworkId);
	}

	public Transform GetGobByName(string name)
	{
		int index = Array.IndexOf(spawnablesNames, name);

        return spawnables[index];
	}

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	void SpawnObjectServerRpc(string spawnableName, Vector2 spawnPos, ulong spawnerClientId, ulong creatorObjectNetworkId)
	{
		Transform spawnedObjectTransform = Instantiate(GetGobByName(spawnableName), spawnPos, Quaternion.identity);

		ulong ownerId = spawnerClientId;

        spawnedObjectTransform.gameObject.GetComponent<Spawnable>().SetCreator(creatorObjectNetworkId);

        spawnedObjectTransform.GetComponent<NetworkObject>().SpawnWithOwnership(ownerId);
    }
}
