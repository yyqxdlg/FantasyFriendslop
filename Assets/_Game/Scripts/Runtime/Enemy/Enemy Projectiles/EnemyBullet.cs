using System;
using Unity.Netcode;
using UnityEngine;

public class EnemyBullet : NetworkBehaviour
{
    public float speed = 8f;
    public float lifeTime = 2f;
    public float damage = 1f;
    private Rigidbody2D rb;
    public GameObject creator;
    private Vector2 direction = Vector2.right;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Set velocity after direction is set
        rb.linearVelocity = direction * speed;

        Invoke("networkDestroy", lifeTime);
    }

    public void SetCreator(GameObject creator)
    {
        this.creator = creator;
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        // If already spawned, update velocity immediately
        if (rb != null && IsServer)
        {
            rb.linearVelocity = direction * speed;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject != creator && !collision.isTrigger)
        {
            if (IsServer)
            {
                // Damage players instead of enemies
                CharacterBasic characterHit = collision.gameObject.GetComponent<CharacterBasic>();
                if (characterHit != null)
                {
                    characterHit.TakeDamage(damage);
                }
                gameObject.GetComponent<NetworkObject>().Despawn(true);
            }
        }
    }

    private void networkDestroy()
    {
        gameObject.GetComponent<NetworkObject>().Despawn(true);
    }
}