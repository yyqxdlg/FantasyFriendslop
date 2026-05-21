using Unity.Netcode;
using UnityEngine;

public class SelectPlateController : NetworkBehaviour
{

    public CharacterSelectPlate[] plates;

    public void DisablePlates()
    {
        if (!IsServer)
        {
            Debug.Log("THis should be called from the server");
            return;
        }

        for(int i = 0; i < plates.Length; i++)
        {
            plates[i].DisablePlate();
        }
    }
}
