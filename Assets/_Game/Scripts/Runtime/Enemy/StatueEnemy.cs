using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class StatueEnemy : EnemyBasic
{
    [Header("Statue Dormant / Wake Up")]
    [Tooltip("初始是否像普通雕像一样沉睡：隐藏血条、不能被打、不会攻击。")]
    [SerializeField] private bool startDormant = true;

    [Tooltip("玩家进入这个范围后，雕像会被唤醒。")]
    [SerializeField] private float activationRange = 6f;

    [Tooltip("false = 任意一个活着的玩家进入范围就唤醒；true = 所有活着的玩家都进入范围才唤醒。")]
    [SerializeField] private bool activationRequiresAllLivingPlayers = true;

    [Tooltip("沉睡时隐藏血条。")]
    [SerializeField] private bool hideHealthBarWhileDormant = true;

    [Tooltip("沉睡时关闭身体碰撞。一般雕像建议 false，这样它仍然像场景物体一样挡路。")]
    [SerializeField] private bool disableBodyCollisionWhileDormant = false;

    [Tooltip("沉睡时是否转向最近玩家。一般建议 false，看起来更像普通雕像。")]
    [SerializeField] private bool faceNearestPlayerWhileDormant = false;

    [Header("Statue Attack")]
    [SerializeField] private float statueHitDelay = 0.35f;
    [SerializeField] private int comboCount = 1;
    [SerializeField] private float timeBetweenHits = 0.35f;

    [Header("Attack Box Size")]
    [SerializeField] private float attackLength = 4f;
    [SerializeField] private float attackWidth = 1.2f;

    [Header("Attack Box Offset Per Direction")]
    [SerializeField] private Vector2 rightBoxOffset = new Vector2(1.6f, -0.6f);
    [SerializeField] private Vector2 leftBoxOffset = new Vector2(-1.6f, -0.6f);
    [SerializeField] private Vector2 upBoxOffset = Vector2.zero;
    [SerializeField] private Vector2 downBoxOffset = new Vector2(0f, -1.5f);

    private readonly List<Collider2D> bodyColliders = new List<Collider2D>();

    private AttackTelegraph telegraph;
    private bool isAttacking = false;

    private NetworkVariable<bool> isStatueActive = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Dash")]
    public float dashCooldown = 5f;

    protected override void Awake()
    {
        base.Awake();

        telegraph = GetComponent<AttackTelegraph>();
        CacheBodyColliders();
    }

    private void Start()
    {
        // 防止第一帧血条闪出来。
        if (startDormant)
        {
            ApplyDormantStateLocal(true);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        isStatueActive.OnValueChanged += OnStatueActiveChanged;

        if (IsServer)
        {
            isStatueActive.Value = !startDormant;
        }

        ApplyDormantStateLocal(startDormant && !isStatueActive.Value);
    }

    public override void OnNetworkDespawn()
    {
        isStatueActive.OnValueChanged -= OnStatueActiveChanged;
        base.OnNetworkDespawn();
    }

    protected override void ServerUpdate()
    {
        if (!IsServer) return;

        attackCooldownCurr -= Time.deltaTime;

        StopEnemyMovement();
        SetMovingAnimationState(false);

        // 沉睡阶段：不攻击、不造成伤害，只检测是否应该激活。
        if (!isStatueActive.Value)
        {
            if (faceNearestPlayerWhileDormant)
            {
                GameObject nearest = FindNearestLivingPlayer();

                if (nearest != null)
                {
                    target = nearest;
                    UpdateFacingOnly(DirToTarget());
                }
            }

            if (ShouldWakeUp())
            {
                WakeUpStatue();
            }

            return;
        }

        // 激活后：朝向最近玩家。
        GameObject nearestTarget = FindNearestLivingPlayer();

        if (nearestTarget != null)
        {
            target = nearestTarget;
            UpdateFacingOnly(DirToTarget());
        }

        if (isAttacking) return;
        if (attackCooldownCurr > 0f) return;

        // 注意：这里不再用圆形 Distance 判断。
        // 只有玩家在四方向矩形攻击范围里，雕像才攻击。
        GameObject validTarget = FindTargetInsideAttackBox();

        if (validTarget == null) return;

        target = validTarget;
        StartCoroutine(ServerAttackRoutine());
    }

    public override void TakeDamage(float damage)
    {
        // 沉睡阶段不能被打，不掉血，也不会显示血条。
        if (!isStatueActive.Value)
            return;

        base.TakeDamage(damage);
    }

    private void WakeUpStatue()
    {
        if (!IsServer) return;
        if (isStatueActive.Value) return;

        isStatueActive.Value = true;

        ApplyDormantStateLocal(false);
        SetDormantStateClientRpc(false);
        Invoke("DashToTarget", dashCooldown);
        SetPlayerDependentStats();
    }

    private bool ShouldWakeUp()
    {
        CharacterBasic[] players = FindObjectsByType<CharacterBasic>(FindObjectsSortMode.None);

        int livingPlayerCount = 0;
        int insideRangeCount = 0;

        foreach (CharacterBasic player in players)
        {
            if (player == null) continue;
            if (!player.alive.Value) continue;

            livingPlayerCount++;

            float distance = Vector2.Distance(transform.position, player.transform.position);

            if (distance <= activationRange)
            {
                insideRangeCount++;
            }
        }

        if (livingPlayerCount <= 0)
            return false;

        if (activationRequiresAllLivingPlayers)
        {
            return insideRangeCount >= livingPlayerCount;
        }

        return insideRangeCount > 0;
    }

    private IEnumerator ServerAttackRoutine()
    {
        isAttacking = true;
        attackCooldownCurr = attackCooldown;

        for (int i = 0; i < comboCount; i++)
        {
            if (!isStatueActive.Value)
                break;

            GameObject currentTarget = FindTargetInsideAttackBox();

            if (currentTarget == null)
                break;

            target = currentTarget;

            Vector2 attackDir = SnapToCardinal(
                (target.transform.position - transform.position).normalized
            );

            UpdateFacingOnly(attackDir);

            GetAttackBox(
                attackDir,
                out Vector2 boxCenter,
                out Vector2 boxSize
            );

            PlayAttackVisual();

            // 显示矩形攻击范围。
            if (telegraph != null)
            {
                telegraph.ShowBoxClientRpc(
                    boxCenter,
                    boxSize,
                    0f,
                    statueHitDelay
                );
            }

            yield return new WaitForSeconds(statueHitDelay);

            // 实际伤害也使用同一个矩形范围。
            DamagePlayersInBox(boxCenter, boxSize);

            yield return new WaitForSeconds(timeBetweenHits);
        }

        isAttacking = false;
    }

    private GameObject FindNearestLivingPlayer()
    {
        CharacterBasic[] players = FindObjectsByType<CharacterBasic>(FindObjectsSortMode.None);

        GameObject bestTarget = null;
        float bestDistance = float.MaxValue;

        foreach (CharacterBasic player in players)
        {
            if (player == null) continue;
            if (!player.alive.Value) continue;

            float distance = ((Vector2)player.transform.position - (Vector2)transform.position).sqrMagnitude;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = player.gameObject;
            }
        }

        return bestTarget;
    }

    private GameObject FindTargetInsideAttackBox()
    {
        CharacterBasic[] players = FindObjectsByType<CharacterBasic>(FindObjectsSortMode.None);

        GameObject bestTarget = null;
        float bestDistance = float.MaxValue;

        foreach (CharacterBasic player in players)
        {
            if (player == null) continue;
            if (!player.alive.Value) continue;

            Vector2 toPlayer = player.transform.position - transform.position;

            if (toPlayer.sqrMagnitude <= 0.001f)
                continue;

            Vector2 attackDir = SnapToCardinal(toPlayer.normalized);

            GetAttackBox(
                attackDir,
                out Vector2 boxCenter,
                out Vector2 boxSize
            );

            if (!IsPointInsideBox(player.transform.position, boxCenter, boxSize))
                continue;

            float distance = toPlayer.sqrMagnitude;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = player.gameObject;
            }
        }

        return bestTarget;
    }

    private void GetAttackBox(Vector2 attackDir, out Vector2 center, out Vector2 size)
    {
        center = (Vector2)transform.position + attackDir * (attackLength * 0.5f);

        if (attackDir == Vector2.right)
        {
            center += rightBoxOffset;
            size = new Vector2(attackLength, attackWidth);
        }
        else if (attackDir == Vector2.left)
        {
            center += leftBoxOffset;
            size = new Vector2(attackLength, attackWidth);
        }
        else if (attackDir == Vector2.up)
        {
            center += upBoxOffset;
            size = new Vector2(attackWidth, attackLength);
        }
        else
        {
            center += downBoxOffset;
            size = new Vector2(attackWidth, attackLength);
        }
    }

    private bool IsPointInsideBox(Vector2 point, Vector2 center, Vector2 size)
    {
        Vector2 diff = point - center;

        return Mathf.Abs(diff.x) <= size.x * 0.5f &&
               Mathf.Abs(diff.y) <= size.y * 0.5f;
    }

    private void DamagePlayersInBox(Vector2 center, Vector2 size)
    {
        if (!IsServer) return;
        if (!isStatueActive.Value) return;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);

        HashSet<CharacterBasic> damagedPlayers = new HashSet<CharacterBasic>();

        foreach (Collider2D hit in hits)
        {
            CharacterBasic player = hit.GetComponentInParent<CharacterBasic>();

            if (player == null) continue;
            if (!player.alive.Value) continue;
            if (damagedPlayers.Contains(player)) continue;

            damagedPlayers.Add(player);
            player.TakeDamage(attackDamage);
        }
    }

    private Vector2 SnapToCardinal(Vector2 dir)
    {
        if (dir.sqrMagnitude <= 0.001f)
            return Vector2.down;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            return dir.x >= 0f ? Vector2.right : Vector2.left;
        }

        return dir.y >= 0f ? Vector2.up : Vector2.down;
    }

    private void OnStatueActiveChanged(bool oldValue, bool newValue)
    {
        ApplyDormantStateLocal(!newValue);
    }

    [ClientRpc]
    private void SetDormantStateClientRpc(bool dormant)
    {
        ApplyDormantStateLocal(dormant);
    }

    private void ApplyDormantStateLocal(bool dormant)
    {
        if (hideHealthBarWhileDormant && healthBar != null)
        {
            if (dormant)
                healthBar.Hide();
            else
                healthBar.UnHide();
        }

        if (disableBodyCollisionWhileDormant)
        {
            foreach (Collider2D col in bodyColliders)
            {
                if (col == null) continue;
                col.enabled = !dormant;
            }
        }
    }

    private void DashToTarget()
    {
        if (!IsServer) return;

        GameObject target = NearestLivingTarget();

        if(target != null)
        {
            transform.position = target.transform.position;
        }

        Invoke("DashToTarget", dashCooldown);

    }

    private void SetPlayerDependentStats()
    {
        if (!IsServer) return;

        int playerCount = GameplayManager.Instance.characters.Count;

        float newMaxHealth = maxHealth * playerCount;

        ChangeMaxHealthRpc(newMaxHealth);

        health.Value = newMaxHealth;

        attackDamage = attackDamage * playerCount;
    }


    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void ChangeMaxHealthRpc(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
    }

    private void CacheBodyColliders()
    {
        bodyColliders.Clear();

        Collider2D[] allColliders = GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D col in allColliders)
        {
            if (col == null) continue;

            // 保留 trigger，比如 TargetingRange。
            // 如果以后你还想用 trigger 检测玩家，就不能关掉它。
            if (col.isTrigger) continue;

            bodyColliders.Add(col);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRange);

        Gizmos.color = Color.red;
        DrawDirectionGizmo(Vector2.right, rightBoxOffset);
        DrawDirectionGizmo(Vector2.left, leftBoxOffset);
        DrawDirectionGizmo(Vector2.up, upBoxOffset);
        DrawDirectionGizmo(Vector2.down, downBoxOffset);
    }

    private void DrawDirectionGizmo(Vector2 dir, Vector2 offset)
    {
        Vector2 center = (Vector2)transform.position + dir * (attackLength * 0.5f) + offset;

        Vector2 size = Mathf.Abs(dir.x) > 0.01f
            ? new Vector2(attackLength, attackWidth)
            : new Vector2(attackWidth, attackLength);

        Gizmos.DrawWireCube(center, size);
    }
}