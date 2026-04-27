using UnityEngine;
using Unity.Netcode;

// Anything spawnable by the spawner util must extend this,
// so that it can keep track of its creator.
public class Spawnable : NetworkBehaviour
{
    private ulong preSpawnCreatorId = ulong.MaxValue;

    public NetworkVariable<ulong> creatorNetworkId = new NetworkVariable<ulong>(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // NetworkVariables should only be written by the server by default.
        if (IsServer)
        {
            creatorNetworkId.Value = preSpawnCreatorId;
        }
    }

    public GameObject GetCreator()
    {
        if (creatorNetworkId.Value == ulong.MaxValue)
        {
            return null;
        }

        if (NetworkManager.Singleton == null)
        {
            return null;
        }

        if (NetworkManager.Singleton.SpawnManager == null)
        {
            return null;
        }

        bool found = NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
            creatorNetworkId.Value,
            out NetworkObject netObj
        );

        if (!found || netObj == null)
        {
            return null;
        }

        if (!netObj.IsSpawned)
        {
            return null;
        }

        return netObj.gameObject;
    }

    public void SetCreator(ulong newCreatorNetworkId)
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            if (!IsServer)
            {
                Debug.LogWarning($"{name}: SetCreator was called on a non-server instance.");
                return;
            }

            creatorNetworkId.Value = newCreatorNetworkId;
        }
        else
        {
            preSpawnCreatorId = newCreatorNetworkId;
        }
    }

    public void NetworkDestroy()
    {
        if (!IsSpawned) return;

        NetworkDestroyServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void NetworkDestroyServerRpc()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();

        if (netObj == null) return;
        if (!netObj.IsSpawned) return;

        netObj.Despawn(true);
    }
}