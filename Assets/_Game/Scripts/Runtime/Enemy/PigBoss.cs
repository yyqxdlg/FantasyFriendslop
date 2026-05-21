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

    [Header("Boss - Phase Coin Ult")]
    [SerializeField] private float firstUltHealthPercent = 0.25f;
    [SerializeField] private float secondUltHealthPercent = 0.05f;
    [SerializeField] private float ultHealPercent = 0.10f;
    [SerializeField] private float ultCastDelay = 0.4f;
    [SerializeField] private float ultCoinInterval = 0.08f;

    private bool firstUltTriggered = false;
    private bool secondUltTriggered = false;
    // ── Inspector 配置 ────────────────────────────────
    [Header("Boss - SmallJump")]
    [SerializeField] private float smallJumpForce = 6f;
    [SerializeField] private float smallJumpDuration = 0.4f;   // 在空中的时间
    [Tooltip("小跳攻击半径（落地时判定）")]
    [SerializeField] private float smallJumpStompRadius = 1.5f;
    [SerializeField] private float smallJumpDamage = 1f;

    [Header("Boss - Bigjump")]
    [SerializeField] private float bigJumpForce = 10f;
    [SerializeField] private float bigJumpDuration = 0.8f;
    [Tooltip("大跳落地踩踏半径")]
    [SerializeField] private float bigJumpStompRadius = 3f;
    [SerializeField] private float bigJumpStompDamage = 3f;
    [SerializeField] private float bigJumpKnockbackForce = 8f;

    [Header("Boss - Coin Ult")]
    [Tooltip("每次 Ult 掉落的硬币数量")]
    [SerializeField] private int ultCoinCount = 12;
    [Tooltip("硬币掉落范围半径")]
    [SerializeField] private float ultCoinRadius = 5f;
    [SerializeField] private string bossCoinSpawnableName = "BossCoin";
    [Tooltip("血量低于此百分比时才会触发 Ult")]
    [SerializeField] private float ultHealthThreshold = 0.5f;
    [Tooltip("每次大跳后触发 Ult 的概率（0~1）")]
    [SerializeField] private float ultChance = 0.4f;

    [Header("Boss - beat")]
    [SerializeField] private float phaseDelay = 0.6f;         // 每个动作之间的停顿
    [SerializeField] private int smallJumpsBeforeBig = 2;     // 多少次小跳后大跳
    [Header("Boss - Attack Warning")]
[SerializeField] private float smallJumpLandingWarningTime = 0.15f;
[SerializeField] private float bigJumpLandingWarningTime = 0.25f;

[Header("Boss - Path Hit")]
[SerializeField] private float smallJumpPathWidth = 1.2f;
[SerializeField] private float smallJumpPathDamage = 1f;
[SerializeField] private float smallJumpPathKnockbackForce = 3f;
[SerializeField] private float smallJumpPathKnockdownDuration = 0.2f;

[SerializeField] private float bigJumpPathWidth = 1.6f;
[SerializeField] private float bigJumpPathDamage = 2f;
[SerializeField] private float bigJumpPathKnockbackForce = 7f;
[SerializeField] private float bigJumpPathKnockdownDuration = 0.45f;

[SerializeField] private float stompKnockdownDuration = 0.35f;
[SerializeField] private float landingKnockdownDuration = 0.45f;


    // ── 运行时状态 ────────────────────────────────────
    private bool isBossActing = false;
    private int smallJumpCount = 0;

    // ── Animator ──────────────────────────────────────
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

    // ── 覆盖父类 ServerUpdate ─────────────────────────
    protected override void ServerUpdate()
{
    if (!IsServer) return;

    if (isBossActing)
    {
        return;
    }

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
    {
        return;
    }

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

    // 触发阶段技能时回血 10%
    health.Value = Mathf.Min(maxHealth, health.Value + maxHealth * ultHealPercent);

    if (target != null)
    {
        UpdateFacingOnly(DirToTarget());
    }

    // 用 Stomp 或 BigJump 当施法动作都可以
    TriggerAnimClientRpc("Stomp");

    yield return new WaitForSeconds(ultCastDelay);

    yield return StartCoroutine(DoCoinUlt());

    yield return new WaitForSeconds(phaseDelay);

    StopEnemyMovement();
    isBossActing = false;
}
    // ── 主攻击循环 ────────────────────────────────────
private IEnumerator BossAttackCycle()
{
    if (isBossActing) yield break;

    isBossActing = true;
    StopEnemyMovement();

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
    if (target != null)
    {
        UpdateFacingOnly(DirToTarget());
    }

    StopEnemyMovement();
    SetMovingAnimationState(false);

    TriggerAnimClientRpc("Stomp");

    if (telegraph != null)
    {
        telegraph.ShowCircleClientRpc(
            transform.position,
            stompRadius,
            stompHitDelay
        );
    }

    yield return new WaitForSeconds(stompHitDelay);

    DoCircleDamage(
        transform.position,
        stompRadius,
        stompDamage,
        bigJumpKnockbackForce,
        stompKnockdownDuration
    );

    yield return new WaitForSeconds(Mathf.Max(0f, stompLockTime - stompHitDelay));
}
    // ── 小跳实现 ──────────────────────────────────────
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
        smallJumpDuration
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
            smallJumpPathDamage,
            smallJumpPathKnockbackForce,
            smallJumpPathKnockdownDuration,
            hitPlayers
        );

        yield return null;
    }

    rb.linearVelocity = Vector2.zero;
    SetMovingAnimationState(false);

    if (telegraph != null)
    {
        telegraph.ShowCircleClientRpc(
            transform.position,
            smallJumpStompRadius,
            smallJumpLandingWarningTime
        );
    }

    yield return new WaitForSeconds(smallJumpLandingWarningTime);

    DoCircleDamage(
        transform.position,
        smallJumpStompRadius,
        smallJumpDamage,
        smallJumpPathKnockbackForce,
        smallJumpPathKnockdownDuration
    );
}

    // ── 大跳实现 ──────────────────────────────────────
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
        bigJumpDuration
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
            bigJumpPathDamage,
            bigJumpPathKnockbackForce,
            bigJumpPathKnockdownDuration,
            hitPlayers
        );

        yield return null;
    }

    rb.linearVelocity = Vector2.zero;
    SetMovingAnimationState(false);

    if (telegraph != null)
    {
        telegraph.ShowCircleClientRpc(
            transform.position,
            bigJumpStompRadius,
            bigJumpLandingWarningTime
        );
    }

    yield return new WaitForSeconds(bigJumpLandingWarningTime);

    DoCircleDamage(
        transform.position,
        bigJumpStompRadius,
        bigJumpStompDamage,
        bigJumpKnockbackForce,
        landingKnockdownDuration
    );
}
private void ShowPathWarning(Vector2 start, Vector2 end, float width, float duration)
{
    if (telegraph == null) return;

    Vector2 diff = end - start;
    float length = diff.magnitude;

    if (length <= 0.01f) return;

    Vector2 center = start + diff * 0.5f;
    float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

    telegraph.ShowBoxClientRpc(
        center,
        new Vector2(length, width),
        angle,
        duration
    );
}

private void DamagePlayersOnPath(
    Vector2 start,
    Vector2 end,
    float width,
    float damage,
    float knockbackForce,
    float knockdownDuration,
    HashSet<CharacterBasic> hitPlayers
)
{
    if (!IsServer) return;

    Vector2 diff = end - start;
    float length = diff.magnitude;

    if (length <= 0.01f) return;

    Vector2 center = start + diff * 0.5f;
    float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

    Collider2D[] hits = Physics2D.OverlapBoxAll(
        center,
        new Vector2(length, width),
        angle
    );

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
            knockDir = diff.normalized;

        player.ApplyKnockdown(
            knockDir * knockbackForce,
            knockdownDuration
        );
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

            player.ApplyKnockdown(
                knockDir * knockbackForce,
                knockdownDuration
            );
        }
    }
}
    // ── Coin Ult 实现 ─────────────────────────────────
    private IEnumerator DoCoinUlt()
    {
        // 从天上随机位置掉落 BossCoin
        for (int i = 0; i < ultCoinCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * ultCoinRadius;
            Vector2 spawnPos = (Vector2)transform.position + offset;

            // BossCoin 是一个有 Rigidbody2D 和爆炸脚本的特殊预制体
            SpawnerUtil.Instance.NetworkSpawnGameObject(
                bossCoinSpawnableName,
                spawnPos
            );

            // 每枚硬币之间有短暂间隔，营造"下雨"感觉
            yield return new WaitForSeconds(ultCoinInterval);
        }

        // 等待硬币全部落地爆炸
        yield return new WaitForSeconds(1.5f);
    }

    // ── 踩踏伤害工具函数 ─────────────────────────────
    private void DoStompDamage(Vector2 center, float radius, float damage, float knockback)
    {
        CharacterBasic[] players = FindObjectsByType<CharacterBasic>(FindObjectsSortMode.None);

        foreach (CharacterBasic player in players)
        {
            if (player == null) continue;
            if (!player.alive.Value) continue;

            float dist = Vector2.Distance(center, player.transform.position);
            if (dist <= radius)
            {
                player.TakeDamage(damage);

                if (knockback > 0f)
                {
                    Vector2 knockDir = ((Vector2)player.transform.position - center).normalized;
                    player.GetComponent<EnemyBasic>(); // 玩家用 CharacterBasic，knockback 看你的实现
                    // 如果 CharacterBasic 有 KnockBack 方法，在这里调用：
                    // player.KnockBack(knockDir * knockback);
                }
            }
        }
    }

    // ── 通知所有客户端触发动画 Trigger ───────────────
    [ClientRpc]
    private void TriggerAnimClientRpc(string triggerName)
    {
        if (_anim != null)
            _anim.SetTrigger(triggerName);
    }
//     public override void OnNetworkSpawn()
// {
//     base.OnNetworkSpawn();

//     if (BossHealthbarUI.Instance != null)
//     {
//         BossHealthbarUI.Instance.Show(this, bossDisplayName, bossIcon);
//     }
// }

// public override void OnNetworkDespawn()
// {
//     if (BossHealthbarUI.Instance != null)
//     {
//         BossHealthbarUI.Instance.HideIfShowing(this);
//     }

//     base.OnNetworkDespawn();
// }
}


// NOTE: 在 Awake 后添加 OnNetworkSpawn：
// public override void OnNetworkSpawn()
// {
//     base.OnNetworkSpawn();
//     if (BossHealthBar.Instance != null)
//         BossHealthBar.Instance.Show(this);
// }
