using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyBasic : Spawnable
{
    // add roomid, every own a roomid
    public static event Action<int, Vector3> OnEnemyDiedInRoom;

    [SerializeField] private int roomId = -1;

    private bool hasDied = false;

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

    // 如果你的 side 原图是朝LEFT，就保持 true。
    // 如果你的 side 原图是朝右，后面 flip 逻辑要反过来。
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
        if (!IsServer) return;

        health.Value = maxHealth;
    }

	public virtual void TakeDamage(float Damage)
	{
		TakeDamageServerRpc(Damage);
	}

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TakeDamageServerRpc(float Damage)
	{
        health.Value -= Damage;

        if (health.Value <= 0)
        {
            Die();
        }
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

        OnEnemyDiedInRoom?.Invoke(roomId, transform.position);

        NetworkObject netObj = GetComponent<NetworkObject>();

        DropCoins(coinsToDrop);

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
        for(int i = 0; i < coins; i++)
        {
            Vector2 pos = gameObject.transform.position;
            
            pos.x += UnityEngine.Random.Range(-1, 1);
            pos.y += UnityEngine.Random.Range(-1, 1);

            Debug.Log("Spawning coin");

            SpawnerUtil.Instance.NetworkSpawnGameObject("Coin", pos, 0, ulong.MaxValue);
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
    if (attackCooldownCurr <= 0)
    {
        attackCooldownCurr = attackCooldown;
        Attack();
    }
}

public virtual void Attack()
{
    target.GetComponent<CharacterBasic>().TakeDamage(attackDamage);
}

void Update()
{
    DecayKnockbackVector();
    healthBar.UpdateHealthBar(health.Value, maxHealth);
    UpdateAnimatorVisuals();
    ServerUpdate();
}

private void UpdateAnimatorVisuals()
{
    if (animator != null)
    {
        animator.SetBool("IsMoving", isMoving.Value);
        animator.SetInteger("Facing", facing.Value);
    }

    if (spriteRenderer == null) return;

    // 只有左右方向需要镜像
    if (sideSpriteFacesLeft)
    {
        // side 原图朝左
        if (facing.Value == 1) // Left
            spriteRenderer.flipX = false;
        else if (facing.Value == 2) // Right
            spriteRenderer.flipX = true;
    }
    else
    {
        // side 原图朝右
        if (facing.Value == 1) // Left
            spriteRenderer.flipX = true;
        else if (facing.Value == 2) // Right
            spriteRenderer.flipX = false;
    }
}

private void UpdateFacingFromMove(Vector2 movementVector)
{
    if (!IsServer) return;

    isMoving.Value = movementVector.sqrMagnitude > 0.01f;

    if (!isMoving.Value) return;

    if (Mathf.Abs(movementVector.x) > Mathf.Abs(movementVector.y))
    {
        if (movementVector.x < 0)
            facing.Value = 1; // Left
        else
            facing.Value = 2; // Right
    }
    else
    {
        if (movementVector.y > 0)
            facing.Value = 3; // Up
        else
            facing.Value = 0; // Down
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


