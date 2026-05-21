using Mono.Cecil.Cil;
using Unity.Netcode;
using UnityEngine;

public class LevelGate : ClickReceiver
{

    [SerializeField] private SelectPlateController plateControl;

    [SerializeField] private Collider2D blocker;

    [SerializeField] private SpriteRenderer closedVis;
    [SerializeField] private SpriteRenderer openVis;

    [SerializeField] private SpawnPointController spawnPointController;



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
        plateControl.DisablePlates();

        blocker.enabled = false;
        closedVis.enabled = false;
        openVis.enabled = true;

        spawnPointController.SpawnAll();
    }


}
