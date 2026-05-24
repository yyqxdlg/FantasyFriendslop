using System;
using Unity.Netcode;
using UnityEngine;

public class EnemyArrow : Spawnable
{
    public float speed = 8f;
    public float lifeTime = 2f;
    public float damage = 1f;
    private Rigidbody2D rb;
    [Header("Audio")]
    [SerializeField] private string hitSoundName = null;
    [SerializeField][Range(0f, 1f)] private float hitVolume = 1f;

    public bool nonRotational = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;

        Vector3 fireDir = (transform.position - GetCreator().transform.position).normalized;

        if (!nonRotational)
        {
            float angle = FFUtilities.CounterClockwiseAngle(fireDir, new Vector2(1, 0));
            gameObject.transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        rb = GetComponent<Rigidbody2D>();

        rb.linearVelocity = fireDir * speed;

        Invoke("NetworkDestroy", lifeTime);
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
                if (hitSoundName != null)
                    AudioManager.Instance.PlaySound(hitSoundName, transform.position);

                gameObject.GetComponent<NetworkObject>().Despawn(true);
            }
        }
    }
}