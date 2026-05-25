using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class TrapDoor : NetworkBehaviour
{
    private NetworkVariable<bool> doorOpen = new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
    );

    private bool locked = false;

    public SpriteRenderer closedVis;
    public SpriteRenderer openVis;

    public Collider2D blocker;

    public float closeDelay = 0.5f;

    //opens after X amount of time, unless set to -1, in which case it remains closed
    public float openAutomatically = -1f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        doorOpen.OnValueChanged += OnDoorChange;

        GameplayManager.Instance.levelStarted.OnValueChanged += OnStartChange;

        OnDoorChange(false, doorOpen.Value);
    }

    private void OnStartChange(bool prev, bool next)
    {
        if (!next)
        {
            ResetState();
        }
    }

    private void ResetState()
    {
        locked = false;
        doorOpen.Value = true;
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) { return; }

        if (locked) { return; }

        if (!collision.isTrigger)
        {
            CharacterBasic player = collision.gameObject.GetComponent<CharacterBasic>();

            if (player != null)
            {
                Invoke("CloseDoor", closeDelay);
                locked = true;
            }
        }
    }

    private void CloseDoor()
    {
        DoorChangeOpenServerRpc(false);

        if(openAutomatically != -1f)
        {
            Invoke("OpenDoor", openAutomatically);
        }
    }

    private void OpenDoor()
    {
        DoorChangeOpenServerRpc(true);
    }


    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void DoorChangeOpenServerRpc(bool open)
    {
        doorOpen.Value = open;
    }

    public void OnDoorChange(bool prev, bool next)
    {
        closedVis.enabled = !next;

        openVis.enabled = next;

        if (blocker != null)
        {
            blocker.enabled = !next;
        }
    }
}
