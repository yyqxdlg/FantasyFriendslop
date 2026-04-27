using System;
using Unity.Netcode;
using UnityEngine;

public class RoomKey : Spawnable
{
    public static event Action<int, ulong> OnKeyPickedUpInRoom;

    [SerializeField] private string playerTag = "Player";

    private int roomId = -1;
    private bool hasBeenPickedUp = false;

    public int RoomId => roomId;

    public void SetRoomId(int newRoomId)
    {
        roomId = newRoomId;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        CharacterBasic player = GetPlayerFromCollider(col);

        if (player == null)
        {
            return;
        }

        // Host / Server: directly pick up.
        if (IsServer)
        {
            Server_PickUpKey(player.OwnerClientId);
            return;
        }

        // Client: only the local owner should request pickup.
        // This prevents one client from sending pickup requests for another player's character.
        if (player.IsOwner)
        {
            RequestPickUpKeyServerRpc(player.OwnerClientId);
        }
    }

    private CharacterBasic GetPlayerFromCollider(Collider2D col)
    {
        CharacterBasic player = col.GetComponent<CharacterBasic>();

        if (player == null)
        {
            player = col.GetComponentInParent<CharacterBasic>();
        }

        if (player == null)
        {
            return null;
        }

        if (!player.CompareTag(playerTag) && !col.CompareTag(playerTag))
        {
            return null;
        }

        return player;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestPickUpKeyServerRpc(ulong pickerClientId)
    {
        Server_PickUpKey(pickerClientId);
    }

    private void Server_PickUpKey(ulong pickerClientId)
    {
        if (!IsServer) return;
        if (hasBeenPickedUp) return;

        hasBeenPickedUp = true;

        OnKeyPickedUpInRoom?.Invoke(roomId, pickerClientId);

        NetworkObject netObj = GetComponent<NetworkObject>();

        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}