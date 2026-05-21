using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BossCoin : Spawnable
{
    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Timing")]
    [SerializeField] private float fallTime = 0.8f;
    [SerializeField] private float explodeDestroyDelay = 0.45f;

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

        PlayFallClientRpc();

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
        if (!IsServer) return;
        if (exploded) return;

        exploded = true;

        PlayExplodeClientRpc();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

        foreach (Collider2D hit in hits)
        {
            CharacterBasic player = hit.GetComponentInParent<CharacterBasic>();

            if (player != null && player.alive.Value)
            {
                player.TakeDamage(damage);
            }
        }

        StartCoroutine(ServerDestroyAfterExplode());
    }

    [ClientRpc]
    private void PlayFallClientRpc()
    {
        if (animator == null) return;

        animator.ResetTrigger("Explode");
        animator.Play("BossCoin_Fall", 0, 0f);
    }

    [ClientRpc]
    private void PlayExplodeClientRpc()
    {
        if (animator == null) return;

        animator.SetTrigger("Explode");
    }

    private IEnumerator ServerDestroyAfterExplode()
    {
        yield return new WaitForSeconds(explodeDestroyDelay);
        NetworkDestroy();
    }
}