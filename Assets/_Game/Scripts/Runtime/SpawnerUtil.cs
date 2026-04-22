using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnerUtil : NetworkBehaviour
{

    public static SpawnerUtil Instance { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public Transform NetworkSpawnGameObject(Transform gob, Vector2 spawnPos, ulong spawnerId)
    {
        Transform spawnedObjectTransform = Instantiate(gob, spawnPos, Quaternion.identity);

        spawnedObjectTransform.GetComponent<NetworkObject>().SpawnWithOwnership(spawnerId);

        return spawnedObjectTransform;
    }
}
