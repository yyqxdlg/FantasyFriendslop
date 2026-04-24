using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ObjectListCollider : NetworkBehaviour
{
    //keeps track of all gameobjects inside the collider

    public List<GameObject> targets = new List<GameObject>();

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;

        if (!collision.isTrigger)
        {
            targets.Add(collision.gameObject);
        }
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsOwner) return;

        if (!collision.isTrigger)
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
