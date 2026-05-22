using Unity.Netcode;
using UnityEngine;

public class LevelFinalGate : NetworkBehaviour
{

    public LockedDoor door;

    private ClickButton btnScript;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        btnScript = gameObject.GetComponent<ClickButton>();

        door.doorOpen.OnValueChanged += OnChangeOpen;

        btnScript.ChangeVisibility(false);
    }

    public void OnChangeOpen(bool prev, bool next)
    {
        btnScript.ChangeVisibility(next);
    }
}
