using Unity.Netcode;
using UnityEngine;

public class RoomLockedGate : NetworkBehaviour
{
    [Header("Unlock Rule")]
    [SerializeField] private int requiredKeyRoomId = 1;

    [Header("Gate Visual States")]
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject openVisual;

    [Header("Gate Colliders")]
    [SerializeField] private Collider2D[] gateColliders;

    [Header("Initial State")]
    [SerializeField] private bool startsUnlocked = false;

    private NetworkVariable<bool> isUnlocked =
        new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        isUnlocked.OnValueChanged += OnUnlockedChanged;

        if (IsServer)
        {
            isUnlocked.Value = startsUnlocked;
            RoomKey.OnKeyPickedUpInRoom += Server_OnKeyPickedUpInRoom;
        }

        ApplyGateState(isUnlocked.Value);
    }

    public override void OnNetworkDespawn()
    {
        isUnlocked.OnValueChanged -= OnUnlockedChanged;

        if (IsServer)
        {
            RoomKey.OnKeyPickedUpInRoom -= Server_OnKeyPickedUpInRoom;
        }
    }

    private void Server_OnKeyPickedUpInRoom(int pickedKeyRoomId, ulong pickerClientId)
    {
        if (!IsServer) return;

        if (pickedKeyRoomId != requiredKeyRoomId)
        {
            return;
        }

        isUnlocked.Value = true;

        Debug.Log($"Gate unlocked by Room {pickedKeyRoomId} key. Picker client: {pickerClientId}");
    }

    private void OnUnlockedChanged(bool oldValue, bool newValue)
    {
        ApplyGateState(newValue);
    }

    private void ApplyGateState(bool unlocked)
    {
        if (lockedVisual != null)
        {
            lockedVisual.SetActive(!unlocked);
        }

        if (closedVisual != null)
        {
            closedVisual.SetActive(false);
        }

        if (openVisual != null)
        {
            openVisual.SetActive(unlocked);
        }

        if (gateColliders == null) return;

        foreach (Collider2D col in gateColliders)
        {
            if (col != null)
            {
                col.enabled = !unlocked;
            }
        }
    }
}