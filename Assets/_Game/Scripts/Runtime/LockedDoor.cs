using Unity.Netcode;
using UnityEngine;

public class LockedDoor : NetworkBehaviour
{

    public NetworkVariable<bool> doorOpen = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
    );

    public SpriteRenderer lockRenderer;

    public SpriteRenderer closedVis;
    public SpriteRenderer openVis;

    public Collider2D blocker;

    public FogOfWar fow;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        doorOpen.OnValueChanged += OnDoorChange;

        OnDoorChange(false, doorOpen.Value);
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if(!IsServer) { return; }

        if (!collision.isTrigger)
        {
            CharacterBasic player = collision.gameObject.GetComponent<CharacterBasic>();

            if (player != null)
            {
                if (player.CheckIfInInventory("DoorKey"))
                {
                    player.RemoveFromInventory("DoorKey");
                    OpenDoorServerRpc();
                }
                
            }
        }
    }


    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void OpenDoorServerRpc()
    {
        doorOpen.Value = true;
    }

    public void OnDoorChange(bool prev, bool next)
    {
        lockRenderer.enabled = !next;
        closedVis.enabled = !next;

        openVis.enabled = next;

        if(blocker != null)
        {
            blocker.enabled = !next;
        }

        if(fow != null)
        {
            fow.Reveal();
        }
    }
}
