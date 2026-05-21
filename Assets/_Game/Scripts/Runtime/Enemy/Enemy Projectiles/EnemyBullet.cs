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
    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField][Range(0f, 1f)] private float hitVolume = 1f;

    public bool nonRotational = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

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

        if (rb != null && IsServer)
        {
            if (!nonRotational)
            {
                float angle = FFUtilities.CounterClockwiseAngle(direction, new Vector2(1, 0));
                gameObject.transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            rb.linearVelocity = direction * speed;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag != "Enemy" && !collision.isTrigger)
        {
            if (IsServer)
            {

                CharacterBasic characterHit = collision.gameObject.GetComponent<CharacterBasic>();

                if (characterHit != null)
                {
                    characterHit.TakeDamage(damage);
                }
                if (hitSound != null)
                    AudioSource.PlayClipAtPoint(hitSound, transform.position, hitVolume);

                gameObject.GetComponent<NetworkObject>().Despawn(true);
            }
        }
    }

    private void networkDestroy()
    {
        gameObject.GetComponent<NetworkObject>().Despawn(true);
    }
}