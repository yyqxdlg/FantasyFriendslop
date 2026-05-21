using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PigBoss : EnemyBasic
{
    // [Header("Boss UI")]
    // [SerializeField] private string bossDisplayName = "Piggy Boss";
    // [SerializeField] private Sprite bossIcon;

    [Header("Boss - Stomp")]
    [SerializeField] private float stompHitDelay = 0.25f;
    [SerializeField] private float stompLockTime = 0.6f;
    [SerializeField] private float stompRadius = 2.5f;
    [SerializeField] private float stompDamage = 2f;
    [Tooltip("践踏圆形范围的位置偏移。X = 朝向前后，Y = 朝向左右。")]
    [SerializeField] private Vector2 stompCenterOffset = Vector2.zero;
    [SerializeField] private float stompKnockdownDuration = 0.35f;

    [Header("Boss - Phase Coin Ult")]
    [SerializeField] private float firstUltHealthPercent = 0.25f;
    [SerializeField] private float secondUltHealthPercent = 0.05f;
    [SerializeField] private float ultHealPercent = 0.10f;
    [SerializeField] private float ultCastDelay = 0.4f;
    [SerializeField] private float ultCoinInterval = 0.08f;

    [Header("Boss - SmallJump")]
    [SerializeField] private float smallJumpForce = 6f;
    [SerializeField] private float smallJumpDuration = 0.4f;
    [SerializeField] private float smallJumpStompRadius = 1.5f;
    [SerializeField] private float smallJumpDamage = 1f;
    [SerializeField] private float smallJumpLandingWarningTime = 0.15f;
    [Tooltip("小跳落地圆形范围的位置偏移。X = 朝向前后，Y = 朝向左右。")]
    [SerializeField] private Vector2 smallJumpLandingOffset = Vector2.zero;

    [Header("Boss - SmallJump Path Hit")]
    [SerializeField] private float smallJumpPathWidth = 1.2f;
    [SerializeField] private float smallJumpPathDamage = 1f;
    [SerializeField] private float smallJumpPathKnockbackForce = 3f;
    [SerializeField] private float smallJumpPathKnockdownDuration = 0.2f;
    [Tooltip("小跳路径矩形的位置偏移。X = 路径前后，Y = 路径左右。")]
    [SerializeField] private Vector2 smallJumpPathOffset = Vector2.zero;

    [Header("Boss - BigJump")]
    [SerializeField] private float bigJumpForce = 10f;
    [SerializeField] private float bigJumpDuration = 0.8f;
    [SerializeField] private float bigJumpStompRadius = 3f;
    [SerializeField] private float bigJumpStompDamage = 3f;
    [SerializeField] private float bigJumpKnockbackForce = 8f;
    [SerializeField] private float bigJumpLandingWarningTime = 0.25f;
    [SerializeField] private float landingKnockdownDuration = 0.45f;
    [Tooltip("大跳落地圆形范围的位置偏移。X = 朝向前后，Y = 朝向左右。")]
    [SerializeField] private Vector2 bigJumpLandingOffset = Vector2.zero;

    [Header("Boss - BigJump Path Hit")]
    [SerializeField] private float bigJumpPathWidth = 1.6f;
    [SerializeField] private float bigJumpPathDamage = 2f;
    [SerializeField] private float bigJumpPathKnockbackForce = 7f;
    [SerializeField] private float bigJumpPathKnockdownDuration = 0.45f;
    [Tooltip("大跳路径矩形的位置偏移。X = 路径前后，Y = 路径左右。")]
    [SerializeField] private Vector2 bigJumpPathOffset = Vector2.zero;

    [Header("Boss - Coin Ult")]
    [SerializeField] private int ultCoinCount = 12;
    [SerializeField] private float ultCoinRadius = 5f;
    [SerializeField] private string bossCoinSpawnableName = "BossCoin";
    [Tooltip("金币雨中心偏移。这个是世界坐标偏移，不跟随朝向旋转。")]
    [SerializeField] private Vector2 coinUltCenterOffset = Vector2.zero;
    [Tooltip("施法前是否显示整片金币雨区域。每枚金币自己的爆炸范围在 BossCoin prefab 里调。")]
    [SerializeField] private bool showCoinRainAreaWarning = true;

    [Header("Boss - Beat")]
    [SerializeField] private float phaseDelay = 0.6f;

    private bool firstUltTriggered = false;
    private bool secondUltTriggered = false;
    private bool isBossActing = false;

    private Animator _anim;
    private AttackTelegraph telegraph;

    protected override void Awake()
    {
        base.Awake();

        _anim = GetComponent<Animator>();
        if (_anim == null)
            _anim = GetComponentInChildren<Animator>();

        telegraph = GetComponent<AttackTelegraph>();
    }

    protected override void ServerUpdate()
    {
        if (!IsServer) return;

        if (isBossActing) return;

        target = NearestLivingTarget();

        if (target == null)
        {
            StopEnemyMovement();
            return;
        }

        float dist = DistanceToSelf(target);
        Vector2 dir = DirToTarget();

        if (dist > attackRange)
        {
            ApplyMoveVector(dir * speed);
            return;
        }

        StopEnemyMovement();
        UpdateFacingOnly(dir);

        if (TryStartPhaseCoinUlt())
            return;

        StartCoroutine(BossAttackCycle());
    }

    private bool TryStartPhaseCoinUlt()
    {
        float hpPercent = health.Value / maxHealth;

        if (!firstUltTriggered && hpPercent <= firstUltHealthPercent)
        {
            firstUltTriggered = true;
            StartCoroutine(DoPhaseCoinUlt());
            return true;
        }

        if (!secondUltTriggered && hpPercent <= secondUltHealthPercent)
        {
            secondUltTriggered = true;
            StartCoroutine(DoPhaseCoinUlt());
            return true;
        }

        return false;
    }

    private IEnumerator DoPhaseCoinUlt()
    {
        isBossActing = true;
        StopEnemyMovement();
        SetMovingAnimationState(false);

        health.Value = Mathf.Min(maxHealth, health.Value + maxHealth * ultHealPercent);

        if (target != null)
        {
            UpdateFacingOnly(DirToTarget());
        }

        TriggerAnimClientRpc("Stomp");

        if (showCoinRainAreaWarning && telegraph != null)
        {
            Vector2 center = (Vector2)transform.position + coinUltCenterOffset;
            telegraph.ShowCircleClientRpc(center, ultCoinRadius, ultCastDelay);
        }

        yield return new WaitForSeconds(ultCastDelay);

        yield return StartCoroutine(DoCoinUlt());

        yield return new WaitForSeconds(phaseDelay);

        StopEnemyMovement();
        isBossActing = false;
    }

    private IEnumerator BossAttackCycle()
    {
        if (isBossActing) yield break;

        isBossActing = true;
        StopEnemyMovement();
        SetMovingAnimationState(false);

        float roll = Random.value;

        if (roll < 1f / 3f)
        {
            yield return StartCoroutine(DoSmallJump());
        }
        else if (roll < 2f / 3f)
        {
            yield return StartCoroutine(DoBigJump());
        }
        else
        {
            yield return StartCoroutine(DoStomp());
        }

        yield return new WaitForSeconds(phaseDelay);

        StopEnemyMovement();
        isBossActing = false;
    }

    private IEnumerator DoStomp()
    {
        Vector2 dir = GetCurrentAttackDirection();
        UpdateFacingOnly(dir);

        StopEnemyMovement();
        SetMovingAnimationState(false);

        Vector2 center = ApplyLocalOffset(transform.position, dir, stompCenterOffset);

        TriggerAnimClientRpc("Stomp");

        if (telegraph != null)
        {
            telegraph.ShowCircleClientRpc(center, stompRadius, stompHitDelay);
        }

        yield return new WaitForSeconds(stompHitDelay);

        DoCircleDamage(
            center,
            stompRadius,
            stompDamage,
            bigJumpKnockbackForce,
            stompKnockdownDuration
        );

        yield return new WaitForSeconds(Mathf.Max(0f, stompLockTime - stompHitDelay));
    }

    private IEnumerator DoSmallJump()
    {
        if (target == null) yield break;

        Vector2 dir = DirToTarget();
        UpdateFacingOnly(dir);
        SetMovingAnimationState(false);

        Vector2 startPos = transform.position;
        Vector2 predictedEndPos = startPos + dir * smallJumpForce * smallJumpDuration;

        ShowPathWarning(
            startPos,
            predictedEndPos,
            smallJumpPathWidth,
            smallJumpDuration,
            smallJumpPathOffset
        );

        TriggerAnimClientRpc("SmallJump");

        HashSet<CharacterBasic> hitPlayers = new HashSet<CharacterBasic>();

        rb.linearVelocity = dir * smallJumpForce;

        float timer = 0f;
        while (timer < smallJumpDuration)
        {
            timer += Time.deltaTime;

            DamagePlayersOnPath(
                startPos,
                transform.position,
                smallJumpPathWidth,
                smallJumpPathOffset,
                smallJumpPathDamage,
                smallJumpPathKnockbackForce,
                smallJumpPathKnockdownDuration,
                hitPlayers
            );

            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        SetMovingAnimationState(false);

        Vector2 landingCenter = ApplyLocalOffset(transform.position, dir, smallJumpLandingOffset);

        if (telegraph != null)
        {
            telegraph.ShowCircleClientRpc(
                landingCenter,
                smallJumpStompRadius,
                smallJumpLandingWarningTime
            );
        }

        yield return new WaitForSeconds(smallJumpLandingWarningTime);

        DoCircleDamage(
            landingCenter,
            smallJumpStompRadius,
            smallJumpDamage,
            smallJumpPathKnockbackForce,
            smallJumpPathKnockdownDuration
        );
    }

    private IEnumerator DoBigJump()
    {
        if (target == null) yield break;

        Vector2 dir = DirToTarget();
        UpdateFacingOnly(dir);
        SetMovingAnimationState(false);

        Vector2 startPos = transform.position;
        Vector2 predictedEndPos = startPos + dir * bigJumpForce * bigJumpDuration;

        ShowPathWarning(
            startPos,
            predictedEndPos,
            bigJumpPathWidth,
            bigJumpDuration,
            bigJumpPathOffset
        );

        TriggerAnimClientRpc("BigJump");

        HashSet<CharacterBasic> hitPlayers = new HashSet<CharacterBasic>();

        rb.linearVelocity = dir * bigJumpForce;

        float timer = 0f;
        while (timer < bigJumpDuration)
        {
            timer += Time.deltaTime;

            DamagePlayersOnPath(
                startPos,
                transform.position,
                bigJumpPathWidth,
                bigJumpPathOffset,
                bigJumpPathDamage,
                bigJumpPathKnockbackForce,
                bigJumpPathKnockdownDuration,
                hitPlayers
            );

            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        SetMovingAnimationState(false);

        Vector2 landingCenter = ApplyLocalOffset(transform.position, dir, bigJumpLandingOffset);

        if (telegraph != null)
        {
            telegraph.ShowCircleClientRpc(
                landingCenter,
                bigJumpStompRadius,
                bigJumpLandingWarningTime
            );
        }

        yield return new WaitForSeconds(bigJumpLandingWarningTime);

        DoCircleDamage(
            landingCenter,
            bigJumpStompRadius,
            bigJumpStompDamage,
            bigJumpKnockbackForce,
            landingKnockdownDuration
        );
    }

    private IEnumerator DoCoinUlt()
    {
        Vector2 rainCenter = (Vector2)transform.position + coinUltCenterOffset;

        for (int i = 0; i < ultCoinCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * ultCoinRadius;
            Vector2 spawnPos = rainCenter + offset;

            SpawnerUtil.Instance.NetworkSpawnGameObject(
                bossCoinSpawnableName,
                spawnPos
            );

            yield return new WaitForSeconds(ultCoinInterval);
        }

        yield return new WaitForSeconds(1.5f);
    }

    private void ShowPathWarning(Vector2 start, Vector2 end, float width, float duration, Vector2 localOffset)
    {
        if (telegraph == null) return;

        GetPathBox(start, end, width, localOffset, out Vector2 center, out Vector2 size, out float angle);

        if (size.x <= 0.01f) return;

        telegraph.ShowBoxClientRpc(center, size, angle, duration);
    }

    private void DamagePlayersOnPath(
        Vector2 start,
        Vector2 end,
        float width,
        Vector2 localOffset,
        float damage,
        float knockbackForce,
        float knockdownDuration,
        HashSet<CharacterBasic> hitPlayers
    )
    {
        if (!IsServer) return;

        GetPathBox(start, end, width, localOffset, out Vector2 center, out Vector2 size, out float angle);
        if (size.x <= 0.01f) return;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle);

        foreach (Collider2D hit in hits)
        {
            CharacterBasic player = hit.GetComponentInParent<CharacterBasic>();

            if (player == null) continue;
            if (!player.alive.Value) continue;
            if (hitPlayers.Contains(player)) continue;

            hitPlayers.Add(player);

            player.TakeDamage(damage);

            Vector2 knockDir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
            if (knockDir.sqrMagnitude <= 0.01f)
                knockDir = (end - start).normalized;

            player.ApplyKnockdown(knockDir * knockbackForce, knockdownDuration);
        }
    }

    private void DoCircleDamage(
        Vector2 center,
        float radius,
        float damage,
        float knockbackForce,
        float knockdownDuration
    )
    {
        if (!IsServer) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        HashSet<CharacterBasic> damagedPlayers = new HashSet<CharacterBasic>();

        foreach (Collider2D hit in hits)
        {
            CharacterBasic player = hit.GetComponentInParent<CharacterBasic>();

            if (player == null) continue;
            if (!player.alive.Value) continue;
            if (damagedPlayers.Contains(player)) continue;

            damagedPlayers.Add(player);

            player.TakeDamage(damage);

            if (knockbackForce > 0f || knockdownDuration > 0f)
            {
                Vector2 knockDir = ((Vector2)player.transform.position - center).normalized;
                if (knockDir.sqrMagnitude <= 0.01f)
                    knockDir = Vector2.down;

                player.ApplyKnockdown(knockDir * knockbackForce, knockdownDuration);
            }
        }
    }

    private void GetPathBox(
        Vector2 start,
        Vector2 end,
        float width,
        Vector2 localOffset,
        out Vector2 center,
        out Vector2 size,
        out float angle
    )
    {
        Vector2 diff = end - start;
        float length = diff.magnitude;

        if (length <= 0.01f)
        {
            center = start;
            size = Vector2.zero;
            angle = 0f;
            return;
        }

        Vector2 dir = diff.normalized;
        Vector2 right = new Vector2(-dir.y, dir.x);

        center = start + diff * 0.5f + dir * localOffset.x + right * localOffset.y;
        size = new Vector2(length, width);
        angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
    }

    private Vector2 ApplyLocalOffset(Vector2 basePos, Vector2 dir, Vector2 localOffset)
    {
        if (dir.sqrMagnitude <= 0.01f)
            dir = Vector2.down;

        dir.Normalize();
        Vector2 right = new Vector2(-dir.y, dir.x);

        return basePos + dir * localOffset.x + right * localOffset.y;
    }

    private Vector2 GetCurrentAttackDirection()
    {
        if (target != null)
        {
            Vector2 dir = DirToTarget();
            if (dir.sqrMagnitude > 0.01f)
                return dir.normalized;
        }

        return Vector2.down;
    }

    [ClientRpc]
    private void TriggerAnimClientRpc(string triggerName)
    {
        if (_anim != null)
            _anim.SetTrigger(triggerName);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stompRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere((Vector2)transform.position + coinUltCenterOffset, ultCoinRadius);
    }
}
