using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class EnemyReviving : EnemyBasic
{
    [Header("Revive")]
    public float respawnTime = 3f;
    public float bodyHp = 5f;

    private bool isDead = false;
    private float bodyHpCurr;
    private Renderer[] renderers;
    private Collider2D[] colliders;

    protected new void Awake()
    {
        base.Awake();
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider2D>();
    }

    public override void TakeDamage(float damage)
    {
        if (isDead)
        {
            bodyHpCurr -= damage;
            if (bodyHpCurr <= 0)
                PermanentDie();
            return;
        }

        base.TakeDamage(damage);
    }

    public override void Die()
    {
        if (isDead) return;
        if (!IsServer) return;

        isDead = true;
        bodyHpCurr = bodyHp;
        StartCoroutine(ReviveAfterDelay());
    }

    private void PermanentDie()
    {
        StopCoroutine(ReviveAfterDelay());
        gameObject.GetComponent<NetworkObject>().Despawn(true);
    }

    private IEnumerator ReviveAfterDelay()
    {
        SetDeadState(true);

        yield return new WaitForSeconds(respawnTime);

        health.Value = maxHealth;
        isDead = false;
        SetDeadState(false);
    }

    private void SetDeadState(bool dead)
    {
        rb.linearVelocity = Vector2.zero;
        this.enabled = !dead;

        foreach (Renderer r in renderers)
            r.enabled = !dead;

        // colliders stay ON so the body can be hit
    }
}