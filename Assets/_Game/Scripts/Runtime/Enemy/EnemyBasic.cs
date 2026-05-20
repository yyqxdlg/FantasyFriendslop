using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class EnemyBasic : Spawnable
{
    [Header("Optional Animation Settings")]
    [SerializeField] private bool hasSpawnAnimation = false;
    [SerializeField] private bool hasAttackAnimation = false;
    [SerializeField] private bool hasDeathAnimation = true;

    [SerializeField] private float spawnLockTime = 0.6f;
    [SerializeField] private float attackHitDelay = 0.25f;
    [SerializeField] private float deathDespawnDelay = 0.8f;

    [Header("Optional Loot Settings")]
    [SerializeField] private bool dropLootOnDeath = true;
    [SerializeField] private string dropSpawnableName = "Coin";
    
    // add roomid, every own a roomid
    public static event Action<int, Vector3> OnEnemyDiedInRoom;

    [SerializeField] private int roomId = -1;

    private bool hasDied = false;
    private bool canAct = true;

    public int RoomId => roomId;

    public void SetRoomId(int newRoomId)
    {
        roomId = newRoomId;
    }
    // old
	public float maxHealth = 10.0f;

    public float speed = 2f;

    public NetworkVariable<float> health = new NetworkVariable<float>();

    [SerializeField] protected Healthbar healthBar;

    [SerializeField] private GameObject targetingRangeObject;

	protected EnemyTargetRange targetingRange;

    protected GameObject target;

	protected Rigidbody2D rb;

    // ADD ANIMATOR
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;


    private NetworkVariable<bool> isDeadForAnimation = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    // 如果你的 side 原图是朝LEFT，就保持 true。
    // 如果你的 side 原图是朝右，后面 flip 逻辑要反过来。
    [SerializeField] private bool useAnimIndexBlendTree = false;
    [SerializeField] private bool hasWalkAnimations = true;
    [SerializeField] private bool useFlipForSideDirections = true;
    [SerializeField] private bool sideSpriteFacesLeft = true;

    private NetworkVariable<int> facing = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> isMoving = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
	
	[Header("Movement")]
	public float standingRange = 0f;

	//when enemy is knocked back, this value is set to something, and decays back to zero over time
	private Vector2 knockbackVector = Vector2.zero;

    [NonSerialized] public float knockbackMultiplier = 1f;

    [NonSerialized] public float knockbackDecayMultiplier = 0.5f;



    [Header("Strafing")]
	public float strafeSpeed = 1.5f;

	private int strafeDirection = 1; // 1 or -1

	public float strafeChangeInterval = 2f;

	private float strafeTimer;

	[Header("Attack")]
	public float attackCooldown = 1f;

	protected float attackCooldownCurr = 1f;

    public float attackDamage = 1f;

    public float attackRange = 1f;

    [Header("Loot")]
    public int coinsToDrop = 1;


    protected void Awake()
    {
        healthBar = GetComponentInChildren<Healthbar>();

        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
        animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

		targetingRange = targetingRangeObject.GetComponent<EnemyTargetRange>();
	}

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;

        health.Value = maxHealth;
        hasDied = false;
        isDeadForAnimation.Value = false;

        if (hasSpawnAnimation)
        {
            canAct = false;
            isMoving.Value = false;

            PlaySpawnAnimationClientRpc();
            StartCoroutine(ServerUnlockAfterSpawnAnimation());
        }
        else
        {
            canAct = true;
        }
    }

	public virtual void TakeDamage(float Damage)
	{
		TakeDamageServerRpc(Damage);
	}

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TakeDamageServerRpc(float Damage)
    {
        if (hasDied) return;

        health.Value -= Damage;

        if (health.Value <= 0)
        {
            Die();
        }
    }
    [ClientRpc]
    private void PlaySpawnAnimationClientRpc()
    {
        if (animator != null)
        {
            animator.SetTrigger("Spawn");
        }
    }

    private IEnumerator ServerUnlockAfterSpawnAnimation()
    {
        yield return new WaitForSeconds(spawnLockTime);
        canAct = true;
    }
    public virtual void HealAmount(float heal)
    {
        HealAmountServerRpc(heal);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void HealAmountServerRpc(float heal)
    {

        float newHealth = health.Value + heal;
        if (newHealth <= maxHealth)
        {
            health.Value = newHealth;
        }
        else
        {
            health.Value = maxHealth;
        }
    }

    public void KnockBack(Vector2 knockVector)
	{
		KnockBackServerRpc(knockVector * knockbackMultiplier);
	}

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void KnockBackServerRpc(Vector2 knockVector)
	{
		knockbackVector = knockVector;
	}
    // add more when ebemy die

public virtual void Die()
{
    if (!IsServer) return;
    if (hasDied) return;

    hasDied = true;
    canAct = false;

    if (rb != null)
    {
        rb.linearVelocity = Vector2.zero;
    }

    isMoving.Value = false;

    OnEnemyDiedInRoom?.Invoke(roomId, transform.position);

    if (hasDeathAnimation)
    {
        isDeadForAnimation.Value = true;
        StartCoroutine(Server_DespawnAfterDeathAnimation());
    }
    else
    {
        DropLootIfNeeded();
        DespawnSelf();
    }
}
    private void DropLootIfNeeded()
    {
        if (dropLootOnDeath)
        {
            DropCoins(coinsToDrop);
        }
    }
private IEnumerator Server_DespawnAfterDeathAnimation()
{
    yield return new WaitForSeconds(deathDespawnDelay);

    DropLootIfNeeded();

    DespawnSelf();
}
    private void DespawnSelf()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();

        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void DropCoins(int coins)
    {
        if (coins <= 0) return;
        if (string.IsNullOrEmpty(dropSpawnableName)) return;

        for (int i = 0; i < coins; i++)
        {
            Vector2 pos = transform.position;

            pos.x += UnityEngine.Random.Range(-1f, 1f);
            pos.y += UnityEngine.Random.Range(-1f, 1f);

            SpawnerUtil.Instance.NetworkSpawnGameObject(dropSpawnableName, pos, 0, ulong.MaxValue);
        }
    }

    protected float DistanceToSelf(GameObject obj)
    {
        return Vector2.Distance(gameObject.transform.position, obj.transform.position);
    }

    public GameObject NearestLivingTarget()
    {
        GameObject targetOut = null;

        float targetOutDistance = float.MaxValue;

        for (int i = 0; i < targetingRange.GetNumberOfTargets(); i++)
        {
            GameObject currentTarget = targetingRange.GetTarget(i);

			if (currentTarget.GetComponent<CharacterBasic>().alive.Value)
			{
				float currentDistance = DistanceToSelf(currentTarget);

				if (DistanceToSelf(currentTarget) < targetOutDistance)
				{
					targetOut = currentTarget;
					targetOutDistance = currentDistance;
				}
			}
		}

        return targetOut;
    }

    public Vector2 DirToTarget()
    {

        return (target.transform.position - gameObject.transform.position).normalized;

	}

    public void AttemptAttack()
    {
        if (attackCooldownCurr > 0) return;
        if (hasDied) return;
        if (!canAct) return;

        attackCooldownCurr = attackCooldown;

        if (hasAttackAnimation)
        {
            PlayAttackAnimationClientRpc();
            StartCoroutine(ServerAttackAfterAnimationDelay());
        }
        else
        {
            Attack();
        }
    }
    private IEnumerator ServerAttackAfterAnimationDelay()
    {
        canAct = false;

        yield return new WaitForSeconds(attackHitDelay);

        if (!hasDied && target != null)
        {
            Attack();
        }

        canAct = true;
    }
    [ClientRpc]
    private void PlayAttackAnimationClientRpc()
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }
public virtual void Attack()
{
    target.GetComponent<CharacterBasic>().TakeDamage(attackDamage);
}

    void Update()
    {
        UpdateAnimatorVisuals();

        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(health.Value, maxHealth);
        }

        if (!IsServer) return;

        if (hasDied) return;

        if (!canAct)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            isMoving.Value = false;
            return;
        }

        DecayKnockbackVector();
        ServerUpdate();
    }

private void UpdateAnimatorVisuals()
{
    if (animator != null)
    {
        animator.SetBool("IsMoving", isMoving.Value);
        animator.SetInteger("Facing", facing.Value);
        animator.SetBool("IsDead", isDeadForAnimation.Value);

        if (useAnimIndexBlendTree)
        {
            animator.SetFloat("AnimIndex", GetAnimIndex());
        }
    }

    if (spriteRenderer == null) return;

    if (!useFlipForSideDirections)
    {
        spriteRenderer.flipX = false;
        return;
    }

    if (sideSpriteFacesLeft)
    {
        if (facing.Value == 1)
            spriteRenderer.flipX = false;
        else if (facing.Value == 2)
            spriteRenderer.flipX = true;
    }
    else
    {
        if (facing.Value == 1)
            spriteRenderer.flipX = true;
        else if (facing.Value == 2)
            spriteRenderer.flipX = false;
    }
}
private float GetAnimIndex()
{
    int dir = facing.Value;
    bool moving = isMoving.Value;

    // Facing:
    // 0 = Down
    // 1 = Left
    // 2 = Right
    // 3 = Up

    // For enemies like Slime:
    // They have directional idle animations, but no walk animations.
    if (!hasWalkAnimations)
    {
        if (dir == 0) return 0f; // Idle_Down
        if (dir == 1) return 1f; // Idle_Left
        if (dir == 2) return 2f; // Idle_Right
        if (dir == 3) return 3f; // Idle_Up

        return 0f;
    }

    // For enemies like Skeleton:
    // They have both idle and walk animations.
    if (!moving)
    {
        if (dir == 0) return 0f;             // Idle_Down
        if (dir == 1 || dir == 2) return 1f; // Idle_Side
        if (dir == 3) return 2f;             // Idle_Up
    }
    else
    {
        if (dir == 0) return 3f;             // Walk_Down
        if (dir == 1 || dir == 2) return 4f; // Walk_Side
        if (dir == 3) return 5f;             // Walk_Up
    }

    return 0f;
}
[SerializeField] private float moveThreshold = 0.05f;
[SerializeField] private float facingDeadZone = 0.15f;

private void UpdateFacingFromMove(Vector2 movementVector)
{
    if (!IsServer) return;

    isMoving.Value = movementVector.sqrMagnitude > moveThreshold * moveThreshold;

    if (!isMoving.Value) return;

    float x = movementVector.x;
    float y = movementVector.y;

    if (Mathf.Abs(x) > Mathf.Abs(y) + facingDeadZone)
    {
        facing.Value = x < 0 ? 1 : 2; // Left / Right
    }
    else if (Mathf.Abs(y) > Mathf.Abs(x) + facingDeadZone)
    {
        facing.Value = y > 0 ? 3 : 0; // Up / Down
    }
}

protected virtual void ServerUpdate()
{
    if (!IsServer) return;

    attackCooldownCurr -= Time.deltaTime;
    target = NearestLivingTarget();

    if (target != null)
    {
        float distance = DistanceToSelf(target);
        Vector2 dir = DirToTarget();
        Vector2 strafeDir = new Vector2(-dir.y, dir.x) * strafeDirection;

        if (distance > standingRange)
            ApplyMoveVector(dir * speed + strafeDir * strafeSpeed);
        else if (distance < standingRange * 0.8f)
            ApplyMoveVector(-dir * speed + strafeDir * strafeSpeed);
        else
            ApplyMoveVector(strafeDir * strafeSpeed);

        if (distance <= attackRange)
            AttemptAttack();
    }
    else
    {
        ApplyMoveVector(Vector2.zero);
    }
}

	//applies the desired movement vector to motion, but takes into account knockback
    private void ApplyMoveVector(Vector2 movementVector)
    {
		rb.linearVelocity = movementVector + knockbackVector;
        UpdateFacingFromMove(movementVector);
    }

	private void DecayKnockbackVector()
	{
        if (knockbackVector.magnitude > 0.01)
		{
			knockbackVector -= knockbackVector.normalized * knockbackDecayMultiplier * Time.deltaTime;
		}
        else
        {
			knockbackVector = Vector2.zero;
        }
    }
}


