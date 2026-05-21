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


	//use if spawning client authoritative object or something that needs to be aware of its creator
    public void NetworkSpawnGameObject(string spawnableName, Vector2 spawnPos, ulong spawnerClientId, ulong creatorObjectNetworkId)
	{
        SpawnObjectServerRpc(spawnableName, spawnPos, spawnerClientId, creatorObjectNetworkId);
	}

	//use if spawning something server owned with no care for its creator
    public void NetworkSpawnGameObject(string spawnableName, Vector2 spawnPos)
    {
        SpawnObjectServerRpc(spawnableName, spawnPos, 0, ulong.MaxValue);
    }

    public Transform GetGobByName(string name)
	{
		int index = Array.IndexOf(spawnablesNames, name);

		if (index == -1)
		{
			throw new Exception("Spawnable with the name: \"" + name + "\" does not exist"); 
		}

        return spawnables[index];
	}

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	void SpawnObjectServerRpc(string spawnableName, Vector2 spawnPos, ulong spawnerClientId, ulong creatorObjectNetworkId)
	{
        Debug.Log("SPAWN " + spawnableName);

        Transform spawnedObjectTransform = Instantiate(GetGobByName(spawnableName), spawnPos, Quaternion.identity);

        Debug.Log("SPAWN RESULT " + spawnedObjectTransform.name);

        ulong ownerId = spawnerClientId;

        spawnedObjectTransform.gameObject.GetComponent<Spawnable>().SetCreator(creatorObjectNetworkId);

        spawnedObjectTransform.GetComponent<NetworkObject>().SpawnWithOwnership(ownerId);
    }
}
