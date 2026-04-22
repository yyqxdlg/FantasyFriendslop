using UnityEngine;
using Unity.Netcode;

//anything spawnable by the spawner util must extend this, so that it can keep track of its creator
public class Spawnable : NetworkBehaviour
{
    private GameObject creator;

    public GameObject GetCreator()
    {
        return creator;
    }

    public void SetCreator(GameObject newCreator)
    {
        creator = newCreator;
    }
}
