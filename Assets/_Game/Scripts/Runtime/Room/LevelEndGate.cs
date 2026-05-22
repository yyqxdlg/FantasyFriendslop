using Unity.Netcode;
using UnityEngine;

public class LevelEndGate : NetworkBehaviour
{
    public SpriteRenderer openVis;
    public SpriteRenderer closeVis;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        GameplayManager.Instance.levelStarted.OnValueChanged += OnLevelStateChanged;

        OnLevelStateChanged(false, GameplayManager.Instance.levelStarted.Value);
    }

    public void OnLevelStateChanged(bool prev, bool next)
    {
        if (next)
        {
            Open();
        } else
        {
            Close();
        }
    }

    public void Close()
    {
        openVis.enabled = false;
        closeVis.enabled = true;
    }

    public void Open()
    {
        openVis.enabled = true;
        closeVis.enabled = false;
    }
}
