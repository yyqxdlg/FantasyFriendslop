using Unity.Netcode;
using UnityEngine;

public class BlackHoleCollider : NetworkBehaviour
{

    public ExplosionInstant explosionScript;

    public override void OnNetworkSpawn()
    {
        transform.localScale = new Vector3(0, 0, 0);
    }

    public void Explode(float radius)
    {
        transform.localScale = new Vector3(radius, radius, radius);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.isTrigger)
        {
            explosionScript.Damage(collision.gameObject);
        }
        
    }
}
