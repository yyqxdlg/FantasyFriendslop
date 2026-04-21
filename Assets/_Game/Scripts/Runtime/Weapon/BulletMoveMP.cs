
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor.TextCore.Text;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class BulletMoveMP : NetworkBehaviour
{
    public float speedFromProjectile = 1f;
	public float lifeTime = 2f;
    public float damage = 1f;

    [NonSerialized] public float speed = 0f;

    [NonSerialized] public Vector2 movementDir = Vector2.zero;

    [NonSerialized] public Rigidbody2D rb;

    public GameObject creator;


    [NonSerialized] public Boolean despawnOnHit = true;

    // when this is false, the bullet will check objects it hits so that it can't damage the same one twice
    // irrelevant if despawnOnHit is set to true
    [NonSerialized] public Boolean preventRepeatedHits = false;

    private List<EntityId> enemiesHit = new List<EntityId>();

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
                GameObject objectHit = collision.gameObject;

                EnemyBasic enemyHitScript = objectHit.GetComponent<EnemyBasic>();
                if(enemyHitScript != null)
                {
                    if (preventRepeatedHits)
                    {
                        if (enemiesHit.Contains(objectHit.GetEntityId()))
                        {
                            return;
                        } else
                        {
                            enemiesHit.Add(objectHit.GetEntityId());
                        }
                    }

                    OnEnemyHitEffect(enemyHitScript);


                }

                if (despawnOnHit)
                {
                    gameObject.GetComponent<NetworkObject>().Despawn(true);
                }
                
            }
        }
    }

    public virtual void OnEnemyHitEffect(EnemyBasic enemyHitScript)
    {
        enemyHitScript.TakeDamage(damage);
    }

    public void networkDestroy()
    {
        gameObject.GetComponent<NetworkObject>().Despawn(true);
    }

}
