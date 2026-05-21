using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemySlimeFlower : EnemyBasic
{
    [Header("SlimeFlower - Disguise Settings")]
    [Tooltip("玩家进入这个距离内触发变身")]
    [SerializeField] private float disguiseBreakRange = 3f;

    [Tooltip("变身动画持续时间，要和 Bloom 动画片段长度接近")]
    [SerializeField] private float bloomAnimDuration = 0.8f;

    [Tooltip("花朵伪装阶段是否隐藏血条")]
    [SerializeField] private bool hideHealthBarWhileDisguised = true;

    [Tooltip("花朵伪装阶段是否关闭身体碰撞")]
    [SerializeField] private bool disableBodyCollisionWhileDisguised = true;

    private readonly List<Collider2D> bodyColliders = new List<Collider2D>();

    private NetworkVariable<bool> isSlimeMode = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool hasTransformed = false;
    private bool isTransforming = false;
    private Animator _anim;

    protected override void Awake()
    {
        base.Awake();

        _anim = GetComponent<Animator>();
        if (_anim == null)
            _anim = GetComponentInChildren<Animator>();

        CacheBodyColliders();
    }

    private void Start()
    {
        // 初始状态是花
        SetFlowerVisualState();

        // 先本地隐藏一次，避免刚生成时血条/碰撞闪一下
        ApplyDisguiseStateLocal(true);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        isSlimeMode.OnValueChanged += OnSlimeModeChanged;

        // 根据当前网络状态刷新一次
        ApplyDisguiseStateLocal(!isSlimeMode.Value);

        if (IsServer)
        {
            hasTransformed = false;
            isTransforming = false;

            // Server 自己也要关碰撞/血条
            ApplyDisguiseStateLocal(true);

            // 所有 Client 也关碰撞/血条
            SetDisguiseStateClientRpc(true);
        }
    }

    public override void OnNetworkDespawn()
    {
        isSlimeMode.OnValueChanged -= OnSlimeModeChanged;
        base.OnNetworkDespawn();
    }

    protected override void ServerUpdate()
    {
        if (!IsServer) return;

        // 花朵阶段 / 变身阶段：不移动，不攻击
        if (!hasTransformed)
        {
            StopEnemyMovement();
            SetMovingAnimationState(false);

            if (isTransforming) return;

            GameObject nearest = NearestLivingTarget();

            if (nearest != null && DistanceToSelf(nearest) <= disguiseBreakRange)
            {
                StartCoroutine(TransformIntoSlime());
            }

            return;
        }

        // 变身完成后，正常走 EnemyBasic 行为
        base.ServerUpdate();
    }

    public override void TakeDamage(float damage)
    {
        // 只要还没完全变成怪，就不能被打
        if (!hasTransformed)
            return;

        if (!isSlimeMode.Value)
            return;

        base.TakeDamage(damage);
    }

    private IEnumerator TransformIntoSlime()
    {
        if (!IsServer) yield break;
        if (isTransforming) yield break;
        if (hasTransformed) yield break;

        isTransforming = true;

        StopEnemyMovement();
        SetCanAct(false);

        // Bloom 动画期间仍然不能被打、没有血条、没有身体碰撞
        ApplyDisguiseStateLocal(true);
        SetDisguiseStateClientRpc(true);

        TriggerBloomAnimClientRpc();

        yield return new WaitForSeconds(bloomAnimDuration);

        // 变身正式完成
        isSlimeMode.Value = true;
        hasTransformed = true;

        // 恢复血条和身体碰撞
        ApplyDisguiseStateLocal(false);
        SetDisguiseStateClientRpc(false);

        SetCanAct(true);
        isTransforming = false;
    }

    [ClientRpc]
    private void TriggerBloomAnimClientRpc()
    {
        if (_anim == null) return;

        _anim.SetBool("IsBloom", false);
        _anim.SetTrigger("Bloom");
    }

    [ClientRpc]
    private void SetDisguiseStateClientRpc(bool disguised)
    {
        ApplyDisguiseStateLocal(disguised);
    }

    private void OnSlimeModeChanged(bool oldValue, bool newValue)
    {
        // newValue = true 代表已经变成怪
        ApplyDisguiseStateLocal(!newValue);

        if (newValue)
        {
            SetSlimeVisualState();
        }
        else
        {
            SetFlowerVisualState();
        }
    }

    private void ApplyDisguiseStateLocal(bool disguised)
    {
        // 血条：花朵阶段隐藏，变怪之后显示
        if (hideHealthBarWhileDisguised && healthBar != null)
        {
            if (disguised)
                healthBar.Hide();
            else
                healthBar.UnHide();
        }

        // 身体碰撞：花朵阶段关闭，变怪之后打开
        // 注意：这里只关闭 non-trigger collider。
        // TargetingRange 通常是 trigger，不能关，不然检测不到玩家靠近。
        if (disableBodyCollisionWhileDisguised)
        {
            foreach (Collider2D col in bodyColliders)
            {
                if (col == null) continue;
                col.enabled = !disguised;
            }
        }
    }

    private void CacheBodyColliders()
    {
        bodyColliders.Clear();

        Collider2D[] allColliders = GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D col in allColliders)
        {
            if (col == null) continue;

            // 保留 trigger，比如 TargetingRange。
            // 花朵靠近检测需要它。
            if (col.isTrigger) continue;

            bodyColliders.Add(col);
        }
    }

    private void SetFlowerVisualState()
    {
        if (_anim == null) return;

        // 你的 Animator 里原本就是用 IsBloom = true 表示花朵状态
        _anim.SetBool("IsBloom", true);
        _anim.SetBool("IsMoving", false);
        _anim.SetBool("IsDead", false);
    }

    private void SetSlimeVisualState()
    {
        if (_anim == null) return;

        _anim.SetBool("IsBloom", false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.4f);
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, disguiseBreakRange);
    }
#endif
}