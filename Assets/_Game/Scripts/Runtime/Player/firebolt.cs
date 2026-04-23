using Unity.Netcode;
using UnityEngine;

public class firebolt : BasicProjectile
{
    public string spawnableName;
    public override void OnHitAnyEffect(Collider2D collision)
    {
        if (!collision.isTrigger)
        {
            SpawnerUtil.Instance.NetworkSpawnGameObject(spawnableName, gameObject.transform.position, OwnerClientId, creatorNetworkId.Value);
        }
    }
}
