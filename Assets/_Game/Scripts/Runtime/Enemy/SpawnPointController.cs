using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnPointController : NetworkBehaviour
{
    private List<SpawnPoint> points = new List<SpawnPoint>();

    public void AddPointToList(SpawnPoint newPoint)
    {
        points.Add(newPoint);
    }

    public void SpawnAll()
    {
        for (int i = 0; i < points.Count; i++)
        {
            points[i].Spawn();
        }
    }
}
