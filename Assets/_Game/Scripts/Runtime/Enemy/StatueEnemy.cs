using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class StatueEnemy : EnemyBasic
{
    [Header("Statue Attack")]
    [SerializeField] private float statueHitDelay = 0.35f;
    [SerializeField] private int comboCount = 1;
    [SerializeField] private float timeBetweenHits = 0.35f;

    [Header("Attack Box Size")]
    [SerializeField] private float attackLength = 4f;
    [SerializeField] private float attackWidth = 1.2f;

    [Header("Attack Box Offset Per Direction")]
    [SerializeField] private Vector2 rightBoxOffset = Vector2.zero;
    [SerializeField] private Vector2 leftBoxOffset = Vector2.zero;
    [SerializeField] private Vector2 upBoxOffset = Vector2.zero;
    [SerializeField] private Vector2 downBoxOffset = Vector2.zero;

    private AttackTelegraph telegraph;
    private bool isAttacking = false;

    protected override void Awake()
    {
        base.Awake();
        telegraph = GetComponent<AttackTelegraph>();
    }

    protected override void ServerUpdate()
    {
        if (!IsServer) return;

        attackCooldownCurr -= Time.deltaTime;

        StopEnemyMovement();

        GameObject nearestTarget = NearestLivingTarget();

        if (nearestTarget != null)
        {
            target = nearestTarget;
            UpdateFacingOnly(DirToTarget());
        }

        if (isAttacking) return;
        if (attackCooldownCurr > 0f) return;

        GameObject validTarget = FindTargetInsideAttackBox();

        if (validTarget == null) return;

        target = validTarget;
        StartCoroutine(ServerAttackRoutine());
    }

    private IEnumerator ServerAttackRoutine()
    {
        isAttacking = true;
        attackCooldownCurr = attackCooldown;

        for (int i = 0; i < comboCount; i++)
        {
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

            DamagePlayersInBox(boxCenter, boxSize);

            yield return new WaitForSeconds(timeBetweenHits);
        }

        isAttacking = false;
    }

    private GameObject FindTargetInsideAttackBox()
    {
        if (targetingRange == null) return null;

        GameObject bestTarget = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < targetingRange.GetNumberOfTargets(); i++)
        {
            GameObject candidate = targetingRange.GetTarget(i);
            if (candidate == null) continue;

            CharacterBasic player = candidate.GetComponent<CharacterBasic>();
            if (player == null) continue;
            if (!player.alive.Value) continue;

            Vector2 toPlayer = candidate.transform.position - transform.position;
            Vector2 attackDir = SnapToCardinal(toPlayer.normalized);

            GetAttackBox(
                attackDir,
                out Vector2 boxCenter,
                out Vector2 boxSize
            );

            if (!IsPointInsideBox(candidate.transform.position, boxCenter, boxSize))
                continue;

            float distance = toPlayer.sqrMagnitude;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = candidate;
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
            return dir.x >= 0f ? Vector2.right : Vector2.left;

        return dir.y >= 0f ? Vector2.up : Vector2.down;
    }

    private void OnDrawGizmosSelected()
    {
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