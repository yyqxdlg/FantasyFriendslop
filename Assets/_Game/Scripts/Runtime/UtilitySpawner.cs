using Unity.Netcode;
using UnityEngine;

public class UtilitySpawner : NetworkBehaviour
{

    public Transform[] utilities;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        for (int i = 0; i < utilities.Length; i++)
        {
            SpawnObject(utilities[i]);
        }
    }

    private void SpawnObject(Transform obj)
    {
        Transform spawnedObjectTransform = Instantiate(obj, new Vector3(0,0,0), Quaternion.identity);

        spawnedObjectTransform.GetComponent<NetworkObject>().SpawnWithOwnership(0);
    }
}
