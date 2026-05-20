using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class StatueEnemy : EnemyBasic
{
    [Header("Statue Attack")]
    [SerializeField] private float statueHitDelay = 0.25f;
    [SerializeField] private int comboCount = 1;
    [SerializeField] private float timeBetweenHits = 0.35f;
    [SerializeField] private float extraHitRange = 0.25f;

    private bool isAttacking = false;

    protected override void ServerUpdate()
    {
        if (!IsServer) return;

        attackCooldownCurr -= Time.deltaTime;

        target = NearestLivingTarget();

        StopEnemyMovement();

        if (target == null) return;

        // Statue 不移动，但是攻击前要转向玩家
        UpdateFacingOnly(DirToTarget());

        if (!isAttacking && attackCooldownCurr <= 0f && DistanceToSelf(target) <= attackRange)
        {
            StartCoroutine(ServerAttackRoutine());
        }
    }

    private IEnumerator ServerAttackRoutine()
    {
        isAttacking = true;
        attackCooldownCurr = attackCooldown;

        for (int i = 0; i < comboCount; i++)
        {
            if (target == null) break;

            // 每次攻击前重新判断方向
            UpdateFacingOnly(DirToTarget());

            // 播 Attack trigger
            PlayAttackVisual();

            // 等动画打到攻击帧
            yield return new WaitForSeconds(statueHitDelay);

            if (target != null && DistanceToSelf(target) <= attackRange + extraHitRange)
            {
                Attack();
            }

            yield return new WaitForSeconds(timeBetweenHits);
        }

        isAttacking = false;
    }
}