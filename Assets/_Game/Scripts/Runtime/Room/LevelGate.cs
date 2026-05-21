using Unity.Netcode;
using UnityEngine;

public class LevelGate : ClickReceiver
{

    [SerializeField] private SelectPlateController plateControl;

    [SerializeField] private Collider2D blocker;

    [SerializeField] private SpriteRenderer closedVis;
    [SerializeField] private SpriteRenderer openVis;

    [SerializeField] private SpawnPointController spawnPointController;

    public NetworkVariable<bool> doorOpen = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        doorOpen.OnValueChanged += OnDoorOpen;
    }

    public override void ReceiveClick(int code)
    {
        base.ReceiveClick(code);

        if(code == 0)
        {
            StartLevel();
        }
    }

    private void StartLevel()
    {
        StartLevelServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void StartLevelServerRpc()
    {
        Debug.Log("STARTING LEVEL");
        //plateControl.DisablePlates();

        doorOpen.Value = true;

        spawnPointController.SpawnAll();
    }

    public void OnDoorOpen(bool prevVal, bool newVal)
    {
        blocker.enabled = false;
        closedVis.enabled = false;
        openVis.enabled = true;
    }
}
