using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class EnemyReviving : EnemyBasic
{
    [Header("Revive")]
    public float respawnTime = 3f;
    public float bodyHp = 5f;

    [SerializeField] private string reviveParticleName;

    private bool isDowned = false;
    private bool permanentlyDead = false;

    private float bodyHpCurr;
    private Renderer[] renderers;
    private Collider2D[] colliders;
    private Coroutine reviveCoroutine;

    protected override void Awake()
    {
        base.Awake();

        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider2D>();
    }

    public override void TakeDamage(float damage)
    {
        TakeDamageRevivingServerRpc(damage);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void TakeDamageRevivingServerRpc(float damage)
    {
        if (permanentlyDead) return;

        if (isDowned)
        {
            bodyHpCurr -= damage;

            if (bodyHpCurr <= 0f)
            {
                PermanentDie();
            }

            return;
        }

        health.Value -= damage;

        if (health.Value <= 0f)
        {
            EnterDownedState();
        }
    }

    private void EnterDownedState()
    {
        if (!IsServer) return;
        if (isDowned) return;
        if (permanentlyDead) return;

        isDowned = true;
        bodyHpCurr = bodyHp;
        health.Value = 0f;

        StopEnemyMovement();
        SetCanAct(false);

        // 注意：这里不要 SetDeadAnimationState(true)
        // 第一次倒下不播放 death 动画，只显示粒子效果。
        SetDownedVisualClientRpc(true);

        if (!string.IsNullOrEmpty(reviveParticleName))
        {
            ParticleManager.Instance.PlayParticle(
                reviveParticleName,
                transform.position,
                respawnTime,
                gameObject
            );
        }

        reviveCoroutine = StartCoroutine(ReviveAfterDelay());
    }

    private IEnumerator ReviveAfterDelay()
    {
        yield return new WaitForSeconds(respawnTime);

        if (!IsServer) yield break;
        if (!isDowned) yield break;
        if (permanentlyDead) yield break;

        isDowned = false;
        bodyHpCurr = bodyHp;
        health.Value = maxHealth;

        SetCanAct(true);
        SetDownedVisualClientRpc(false);
    }

    private void PermanentDie()
    {
        if (!IsServer) return;
        if (permanentlyDead) return;

        permanentlyDead = true;
        isDowned = false;

        if (reviveCoroutine != null)
        {
            StopCoroutine(reviveCoroutine);
            reviveCoroutine = null;
        }

        // 永久死亡前显示回 sprite。
        // 不然 base.Die() 播 death 动画时你会看不到。
        SetDownedVisualClientRpc(false);

        StopEnemyMovement();
        SetCanAct(false);

        // 这里才是真正死亡：
        // base.Die() 会设置 IsDead = true，
        // 播 death 动画，
        // deathDespawnDelay 后掉落并 Despawn。
        base.Die();
    }

    public override void Die()
    {
        if (!IsServer) return;
        if (permanentlyDead) return;

        if (!isDowned)
        {
            EnterDownedState();
        }
    }

    protected override void ServerUpdate()
    {
        if (!IsServer) return;

        if (isDowned || permanentlyDead)
        {
            StopEnemyMovement();
            return;
        }

        base.ServerUpdate();
    }

    [ClientRpc]
    private void SetDownedVisualClientRpc(bool downed)
    {
        foreach (Renderer r in renderers)
        {
            if (r != null)
            {
                r.enabled = !downed;
            }
        }

        // colliders 不关，身体还要能被打。
        // 所以这里不要 disable collider。

        if (healthBar != null)
        {
            if (downed)
                healthBar.Hide();
            else
                healthBar.UnHide();
        }
    }
}