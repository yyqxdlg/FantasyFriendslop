
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor.TextCore.Text;
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

    [NonSerialized] public Boolean despawnOnHit = true;

    // when this is false, the bullet will check objects it hits so that it can't damage the same one twice
    // irrelevant if despawnOnHit is set to true
    [NonSerialized] public Boolean preventRepeatedHits = false;

    private List<EntityId> enemiesHit = new List<EntityId>();

    public void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        Fire();
    }  

    // start movement of projectile
    public void Fire()
    {
        Vector2 fireDir = (GetCreator().GetComponent<CharacterBasic>().mousePos - new Vector2(GetCreator().transform.position.x, GetCreator().transform.position.y)).normalized;

        speed = speedFromProjectile;

        movementDir = fireDir;

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
