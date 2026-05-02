
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
// using UnityEditor.TextCore.Text;
using TMPro;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class BasicProjectile : Spawnable
{
    public float speedFromProjectile = 1f;
    public float lifeTime = 2f;
    public float damage = 1f;

    [NonSerialized] public float speed = 1f;

    [NonSerialized] public Vector2 movementDir = Vector2.zero;

    [NonSerialized] public Rigidbody2D rb;

    public Boolean despawnOnHit = true;

    // when this is false, the bullet will check objects it hits so that it can't damage the same one twice
    // irrelevant if despawnOnHit is set to true
    public Boolean preventRepeatedHits = false;

    private List<EntityId> enemiesHit = new List<EntityId>();

    public bool NonRotational = true;

    public void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        Fire();
    }  

    // start movement of projectile
    public virtual void Fire()
    {
        Vector2 fireDir = (GetCreator().GetComponent<CharacterBasic>().mousePos - new Vector2(GetCreator().transform.position.x, GetCreator().transform.position.y)).normalized;

        speed = speedFromProjectile;

        movementDir = fireDir;

        if (!NonRotational)
        {
            float angle = FFUtilities.CounterClockwiseAngle(fireDir, new Vector2(1, 0));
            gameObject.transform.rotation = Quaternion.Euler(0,0,angle);
        }

        FireBehaviour();
    }

    public virtual void FireBehaviour()
    {
        rb.linearVelocity = movementDir.normalized * speed;

    
            

        if (!IsOwner) { return; }
        Invoke("NetworkDestroy", lifeTime);
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject != GetCreator() && !collision.isTrigger)
        {

            if (IsOwner)
            {

                OnHitAnyEffect(collision);

                if (despawnOnHit)
                {
                    NetworkDestroy();
                }

            }
        }
    }

    public virtual void OnHitAnyEffect(Collider2D collision)
    {
        GameObject objectHit = collision.gameObject;

        EnemyBasic enemyHitScript = objectHit.GetComponent<EnemyBasic>();
        if (enemyHitScript != null)
        {
            if (preventRepeatedHits)
            {
                if (enemiesHit.Contains(objectHit.GetEntityId()))
                {
                    return;
                }
                else
                {
                    enemiesHit.Add(objectHit.GetEntityId());
                }
            }

            OnEnemyHitEffect(enemyHitScript);
        }
    }

    public virtual void OnEnemyHitEffect(EnemyBasic enemyHitScript)
    {
        enemyHitScript.TakeDamage(damage);
    }

}
