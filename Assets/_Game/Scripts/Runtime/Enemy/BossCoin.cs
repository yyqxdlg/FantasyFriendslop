using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BossCoin : Spawnable
{
    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Timing")]
    [SerializeField] private float fallTime = 0.8f;
    [SerializeField] private float explodeDestroyDelay = 0.35f;

    [Header("Damage")]
    [SerializeField] private float explosionRadius = 1.2f;
    [SerializeField] private float damage = 2f;

    private bool exploded = false;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            StartCoroutine(ServerFallThenExplode());
        }
    }

    private IEnumerator ServerFallThenExplode()
    {
        yield return new WaitForSeconds(fallTime);
        Explode();
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

        foreach (Collider2D hit in hits)
        {
            CharacterBasic player = hit.GetComponentInParent<CharacterBasic>();

            if (player != null && player.alive.Value)
            {
                player.TakeDamage(damage);
            }
        }

        PlayExplodeClientRpc();

        StartCoroutine(ServerDestroyAfterExplode());
    }

    [ClientRpc]
    private void PlayExplodeClientRpc()
    {
        if (animator != null)
            animator.SetTrigger("Explode");
    }

    private IEnumerator ServerDestroyAfterExplode()
    {
        yield return new WaitForSeconds(explodeDestroyDelay);
        NetworkDestroy();
    }
}