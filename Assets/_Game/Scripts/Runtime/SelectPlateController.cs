using Unity.Netcode;
using UnityEngine;

public class SelectPlateController : NetworkBehaviour
{

    public CharacterSelectPlate[] plates;

    public void DisablePlates()
    {
        for(int i = 0; i < plates.Length; i++)
        {
            plates[i].PlateEnabled(false);
        }
    }
}
