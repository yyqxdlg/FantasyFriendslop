using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
public class CharacterSelectPlate : NetworkBehaviour
{
    public Transform characterPrefab;

    public string acceptedTag;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) { return; }

        Debug.Log("Collision");

        if (collision.gameObject.tag == acceptedTag)
        {
            collision.gameObject.GetComponent<NetworkObject>().Despawn(true);

            ulong playerId = collision.gameObject.GetComponent<NetworkObject>().OwnerClientId;

            SpawnerUtil.Instance.NetworkSpawnGameObject(characterPrefab, gameObject.transform.position, playerId);
        }
    }
}
