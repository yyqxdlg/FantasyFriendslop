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

	public void NetworkSpawnGameObject(string spawnableName, Vector2 spawnPos, ulong spawnerClientId, bool spawnerIsOwnerBool, ulong creatorObjectNetworkId)
	{
		SpawnObjectServerRpc(spawnableName, spawnPos, spawnerClientId, spawnerIsOwnerBool, creatorObjectNetworkId);
	}

	public Transform GetGobByName(string name)
	{
		int index = Array.IndexOf(spawnablesNames, name);

		Debug.Log(index);

        return spawnables[index];
	}

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	void SpawnObjectServerRpc(string spawnableName, Vector2 spawnPos, ulong spawnerClientId, bool spawnerIsOwnerBool, ulong creatorObjectNetworkId)
	{
		Debug.Log("HELLO WORLD");

		Transform spawnedObjectTransform = Instantiate(GetGobByName(spawnableName), spawnPos, Quaternion.identity);

		ulong ownerId = 0;

		if (spawnerIsOwnerBool)
		{
			ownerId = spawnerClientId;
		}
		
		spawnedObjectTransform.GetComponent<NetworkObject>().SpawnWithOwnership(ownerId);

		NetworkObject netObj;

        if (creatorObjectNetworkId == ulong.MaxValue)
        {
			netObj = null;
        } else
		{
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(creatorObjectNetworkId, out netObj);
        }


		spawnedObjectTransform.gameObject.GetComponent<Spawnable>().SetCreator(netObj.gameObject);
	}
}
