using Unity.Netcode;
// using UnityEditor.PackageManager;
using UnityEngine;
public class CharacterSelectPlate : NetworkBehaviour
{
    public string characterSpawnableName;

    public string acceptedTag;

    public string songToPlay;

    public float songVolume = 0.5f;
    private void OnTriggerEnter2D(Collider2D collision)
    {
      
        if (collision.gameObject.tag == acceptedTag)
        {
            AudioManager.Instance.PlayBackgroundSong(songToPlay, 1);

            if (!IsServer) { return; }

            collision.gameObject.GetComponent<NetworkObject>().Despawn(true);

            ulong playerId = collision.gameObject.GetComponent<NetworkObject>().OwnerClientId;

            SpawnerUtil.Instance.NetworkSpawnGameObject(characterSpawnableName, gameObject.transform.position, playerId, ulong.MaxValue);
        }
    }
}
