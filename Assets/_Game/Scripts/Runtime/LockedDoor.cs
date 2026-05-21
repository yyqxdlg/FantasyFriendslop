using Unity.Netcode;
using UnityEngine;

public class LockedDoor : MonoBehaviour
{

    public SpriteRenderer lockRenderer;

    public SpriteRenderer closedVis;
    public SpriteRenderer openVis;

    public Collider2D blocker;

    public void OnTriggerEnter2D(Collider2D collision)
    {
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
        lockRenderer.enabled = false;
        closedVis.enabled = false;

        openVis.enabled = true;

        blocker.enabled = false;
    }
}
