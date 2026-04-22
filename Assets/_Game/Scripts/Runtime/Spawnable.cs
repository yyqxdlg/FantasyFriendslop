using UnityEngine;
using Unity.Netcode;

//anything spawnable by the spawner util must extend this, so that it can keep track of its creator
public class Spawnable : NetworkBehaviour
{
	public NetworkVariable<ulong> creatorNetworkId = new NetworkVariable<ulong>();

	//public GameObject creator;

	/*
    public override void OnNetworkSpawn()
    {
		creatorNetworkId.OnValueChanged += (prevId, newId) =>
		{
			CreatorChangeFromNetworkId();
		};
    }
	public void CreatorChangeFromNetworkId()
	{
		if(creatorNetworkId.Value == ulong.MaxValue) { return; }

		Debug.Log("SET CREATOR " + creatorNetworkId.Value);

		NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(creatorNetworkId.Value, out NetworkObject netObj);
		creator = netObj.gameObject;

        Debug.Log("Creator null? " + creator == null);
    }
	*/

	public GameObject GetCreator()
	{
		if (creatorNetworkId.Value == ulong.MaxValue)
		{
			return null;
		}
		else
		{
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(creatorNetworkId.Value, out NetworkObject netObj);

            return netObj.gameObject;
        }

	}

	public void SetCreator(ulong newCreatorNetworkId)
	{
		creatorNetworkId.Value = newCreatorNetworkId;
    }
}
