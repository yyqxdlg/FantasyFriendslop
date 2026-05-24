using Unity.Netcode;
using UnityEngine;
using System;

public class SpawnPoint : NetworkBehaviour
{

    public string spawnableName;

    public SpawnPointController controller;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        controller = GetComponentInParent<SpawnPointController>();

        if (controller == null)
        {
            throw new Exception("Spawn point without a controller!");
        }

        controller.AddPointToList(this);
    }

    public virtual void Spawn()
    {
        SpawnerUtil.Instance.NetworkSpawnGameObject(spawnableName, transform.position);
    }
}
