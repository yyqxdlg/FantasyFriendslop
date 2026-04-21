
using System;
using Unity.Netcode;
using UnityEditor.TextCore.Text;
using UnityEngine;

public class BulletMoveMP : NetworkBehaviour
{
    public float speedFromProjectile = 1f;
	public float lifeTime = 2f;
    public float damage = 1f;

    [NonSerialized] public float speed = 0f;

    [NonSerialized] public Vector2 movementDir = Vector2.zero;

    [NonSerialized] public Boolean despawnOnHit = true;

    [NonSerialized] public Rigidbody2D rb;

    public GameObject creator;

	public void Awake()
	{
        rb = GetComponent<Rigidbody2D>();
    }

    // start movement of projectile
    public void Fire(GameObject creator, Vector2 fireDir, float initialSpeedFromShooter)
    {
        this.creator = creator;

        speed = initialSpeedFromShooter * speedFromProjectile;

        movementDir = fireDir;

        FireBehaviour();
    }

    public virtual void FireBehaviour()
    {
        rb.linearVelocity = movementDir.normalized * speed;

        Invoke("networkDestroy", lifeTime);
    }

    public void SetCreator(GameObject creator)
    {
        this.creator = creator;
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject != creator && !collision.isTrigger)
        {

            if (IsServer)
            {
                EnemyBasic scriptEnemyHit = collision.gameObject.GetComponent<EnemyBasic>();
                if(scriptEnemyHit != null)
                {
                    scriptEnemyHit.TakeDamage(damage);
                }

                if (despawnOnHit)
                {
                    gameObject.GetComponent<NetworkObject>().Despawn(true);
                }
                
            }
        }
    }

    public void networkDestroy()
    {
        gameObject.GetComponent<NetworkObject>().Despawn(true);
    }

}
