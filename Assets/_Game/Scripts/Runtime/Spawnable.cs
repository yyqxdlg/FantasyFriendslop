using UnityEngine;
using Unity.Netcode;

//anything spawnable by the spawner util must extend this, so that it can keep track of its creator
public class Spawnable : NetworkBehaviour
{
	private ulong preSpawnCreatorId;

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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

		creatorNetworkId.Value = preSpawnCreatorId;
    }

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
		if (NetworkObject.IsSpawned)
		{
            creatorNetworkId.Value = newCreatorNetworkId;
        }
        else
        {
			preSpawnCreatorId = newCreatorNetworkId;
        }
    }

    public void NetworkDestroy()
    {
        NetworkDestroyServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void NetworkDestroyServerRpc()
    {
        gameObject.GetComponent<NetworkObject>().Despawn(true);
    }
}
