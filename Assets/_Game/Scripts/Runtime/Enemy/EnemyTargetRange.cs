using UnityEngine;
using Unity.Netcode;

using System.Collections.Generic;

public class EnemyTargetRange : NetworkBehaviour
{

    public List<GameObject> targets = new List<GameObject>();

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.tag == "Player")
        {
            targets.Add(collision.gameObject);
        }
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.tag == "Player")
        {
            targets.Remove(collision.gameObject);
        }
    }

    public GameObject GetTarget(int index)
    {
        if (index < targets.Count)
        {
            return targets[index];
        }
        else
        {
            return null;
        }
    }

    public int GetNumberOfTargets()
    {
        return targets.Count;
    }
}
