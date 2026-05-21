using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class EnemySlimeFlower : EnemyBasic
{
    [Header("SlimeFlower - Disguise Settings")]
    [Tooltip("玩家进入这个距离内触发变身")]
    [SerializeField] private float disguiseBreakRange = 3f;

    [Tooltip("变身动画持续时间（要和你的 Bloom 动画片段时长一致）")]
    [SerializeField] private float bloomAnimDuration = 0.8f;

    private NetworkVariable<bool> isSlimeMode = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool hasTransformed = false;
    private Animator _anim;

    protected override void Awake()
    {
        base.Awake();
        _anim = GetComponent<Animator>();
        if (_anim == null)
            _anim = GetComponentInChildren<Animator>();
    }

    // ★ 关键修复：用 Start 而不是 OnNetworkSpawn 设初始动画状态
    //   OnNetworkSpawn 太早，Animator 还没完全初始化好
    private void Start()
    {
        if (_anim != null)
            _anim.SetBool("IsBloom", true);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isSlimeMode.OnValueChanged += OnSlimeModeChanged;
    }

    public override void OnNetworkDespawn()
    {
        isSlimeMode.OnValueChanged -= OnSlimeModeChanged;
        base.OnNetworkDespawn();
    }

    protected override void ServerUpdate()
    {
        if (!IsServer) return;

        if (!hasTransformed)
        {
            // 花朵阶段：检测玩家距离，不移动
            GameObject nearest = NearestLivingTarget();
            if (nearest != null && DistanceToSelf(nearest) <= disguiseBreakRange)
            {
                StartCoroutine(TransformIntoSlime());
            }
            StopEnemyMovement();
            return;
        }

        // 变身完成后：正常 Slime 行为
        base.ServerUpdate();
    }

    private IEnumerator TransformIntoSlime()
    {
        if (hasTransformed) yield break;
        hasTransformed = true;

        SetCanAct(false);

        // 通知所有客户端：退出 IsBloom，触发 Bloom 动画
        TriggerBloomAnimClientRpc();

        yield return new WaitForSeconds(bloomAnimDuration);

        isSlimeMode.Value = true;
        SetCanAct(true);
    }

    [ClientRpc]
    private void TriggerBloomAnimClientRpc()
    {
        if (_anim == null) return;
        _anim.SetBool("IsBloom", false);
        _anim.SetTrigger("Bloom");
    }

    private void OnSlimeModeChanged(bool oldValue, bool newValue)
    {
        // Bloom 动画播完自动进入 Idle_Blend，这里不需要额外操作
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.4f);
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, disguiseBreakRange);
    }
#endif
}