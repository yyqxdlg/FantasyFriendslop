using Unity.Netcode;
// using UnityEditor.PackageManager;
using UnityEngine;
public class CharacterSelectPlate : NetworkBehaviour
{
    public string characterSpawnableName;

    public string acceptedTag;

    public string songToPlay;

    public float songVolume = 0.5f;

    private Color enabledColor;

    public NetworkVariable<bool> plateEnabled = new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        plateEnabled.OnValueChanged += OnEnablingChanged;

        enabledColor = gameObject.GetComponent<SpriteRenderer>().color;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (plateEnabled.Value)
        {
            if (collision.gameObject.tag == acceptedTag)
            {
                AudioManager.Instance.PlayBackgroundSong(songToPlay, songVolume);

                if (!IsServer) { return; }

                collision.gameObject.GetComponent<NetworkObject>().Despawn(true);

                ulong playerId = collision.gameObject.GetComponent<NetworkObject>().OwnerClientId;

                SpawnerUtil.Instance.NetworkSpawnGameObject(characterSpawnableName, gameObject.transform.position, playerId, ulong.MaxValue);
            }
        }
        
    }

    public void PlateEnabled(bool enabled)
    {
        PlateEnabledServerRpc(enabled);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlateEnabledServerRpc(bool enabled)
    {
        plateEnabled.Value = enabled;
    }

    public void OnEnablingChanged(bool prev, bool curr)
    {
        if (curr)
        {
            gameObject.GetComponent<SpriteRenderer>().color = enabledColor;
        } else
        {
            gameObject.GetComponent<SpriteRenderer>().color = Color.gray;
        }
    }

}
