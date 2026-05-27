using Unity.Netcode;
using UnityEngine;

public class LockedDoor : NetworkBehaviour
{
    public NetworkVariable<bool> doorOpen = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool initialized = false;

    public SpriteRenderer lockRenderer;
    public SpriteRenderer closedVis;
    public SpriteRenderer openVis;

    public Collider2D blocker;

    public FogOfWar fow;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        doorOpen.OnValueChanged += OnDoorChange;

        if (GameplayManager.Instance != null)
            GameplayManager.Instance.levelStarted.OnValueChanged += OnLevelStartChange;

        // Apply visual state only during spawn.
        // Do not play network sound during NetworkObject spawn.
        ApplyDoorVisual(doorOpen.Value);

        initialized = true;
    }

    public override void OnNetworkDespawn()
    {
        doorOpen.OnValueChanged -= OnDoorChange;

        if (GameplayManager.Instance != null)
            GameplayManager.Instance.levelStarted.OnValueChanged -= OnLevelStartChange;

        base.OnNetworkDespawn();
    }

    private void OnLevelStartChange(bool prev, bool next)
    {
        if (!IsServer) return;

        if (!next && doorOpen.Value != false)
            doorOpen.Value = false;
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        if (collision.isTrigger) return;

        if (doorOpen.Value) return;

        CharacterBasic player = collision.gameObject.GetComponent<CharacterBasic>();

        if (player != null && player.CheckIfInInventory("DoorKey"))
        {
            player.RemoveFromInventory("DoorKey");
            OpenDoor();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void OpenDoorServerRpc()
    {
        OpenDoor();
    }

    private void OpenDoor()
    {
        if (!IsServer) return;

        if (doorOpen.Value != true)
            doorOpen.Value = true;
    }

    private void OnDoorChange(bool prev, bool next)
    {
        ApplyDoorVisual(next);

        // Do not play sound during initial network spawn.
        if (!initialized) return;

        // Do not play sound if the value did not actually change.
        if (prev == next) return;

        if (IsServer && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound("door", transform.position);
        }
    }

    private void ApplyDoorVisual(bool isOpen)
    {
        if (lockRenderer != null)
            lockRenderer.enabled = !isOpen;

        if (closedVis != null)
            closedVis.enabled = !isOpen;

        if (openVis != null)
            openVis.enabled = isOpen;

        if (blocker != null)
            blocker.enabled = !isOpen;

        if (fow != null && IsServer)
            fow.revealed.Value = isOpen;
    }
}