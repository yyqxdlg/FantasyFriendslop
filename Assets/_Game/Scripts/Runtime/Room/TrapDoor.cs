using Unity.Netcode;
using UnityEngine;

public class TrapDoor : NetworkBehaviour
{
    private NetworkVariable<bool> doorOpen = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool locked = false;
    private bool initialized = false;

    public SpriteRenderer closedVis;
    public SpriteRenderer openVis;
    public Collider2D blocker;

    public float closeDelay = 0.5f;

    // Opens after X seconds. If set to -1, it remains closed.
    public float openAutomatically = -1f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        doorOpen.OnValueChanged += OnDoorChange;

        if (GameplayManager.Instance != null)
            GameplayManager.Instance.levelStarted.OnValueChanged += OnStartChange;

        // Important: only apply visual state here.
        // Do NOT play sound during NetworkObject spawn.
        ApplyDoorVisual(doorOpen.Value);

        initialized = true;
    }

    public override void OnNetworkDespawn()
    {
        doorOpen.OnValueChanged -= OnDoorChange;

        if (GameplayManager.Instance != null)
            GameplayManager.Instance.levelStarted.OnValueChanged -= OnStartChange;

        base.OnNetworkDespawn();
    }

    private void OnStartChange(bool prev, bool next)
    {
        if (!IsServer) return;

        if (!next)
            ResetState();
    }

    private void ResetState()
    {
        if (!IsServer) return;

        locked = false;

        if (doorOpen.Value != true)
            doorOpen.Value = true;
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        if (locked) return;
        if (collision.isTrigger) return;

        CharacterBasic player = collision.gameObject.GetComponent<CharacterBasic>();

        if (player != null)
        {
            Invoke(nameof(CloseDoor), closeDelay);
            locked = true;
        }
    }

    private void CloseDoor()
    {
        if (!IsServer) return;

        SetDoorOpen(false);

        if (openAutomatically != -1f)
            Invoke(nameof(OpenDoor), openAutomatically);
    }

    private void OpenDoor()
    {
        if (!IsServer) return;

        SetDoorOpen(true);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void DoorChangeOpenServerRpc(bool open)
    {
        SetDoorOpen(open);
    }

    private void SetDoorOpen(bool open)
    {
        if (!IsServer) return;
        if (doorOpen.Value == open) return;

        doorOpen.Value = open;
    }

    private void OnDoorChange(bool prev, bool next)
    {
        ApplyDoorVisual(next);

        // Do not play sound during initial network spawn.
        if (!initialized) return;

        // Do not play sound if the value did not really change.
        if (prev == next) return;

        if (IsServer && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound("door", transform.position);
        }
    }

    private void ApplyDoorVisual(bool isOpen)
    {
        if (closedVis != null)
            closedVis.enabled = !isOpen;

        if (openVis != null)
            openVis.enabled = isOpen;

        if (blocker != null)
            blocker.enabled = !isOpen;
    }
}