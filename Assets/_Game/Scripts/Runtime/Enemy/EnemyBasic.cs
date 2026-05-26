using JetBrains.Annotations;
using System;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
public enum EnemyAnimProfile
{
	SingleIdle,              // 只有一个 idle，比如 Jellyfish
	FourDirIdle,             // 4方向 idle，没有 walk，比如 Slime / Flower Slime
	ThreeDirMirrorIdleWalk,  // Down / Side / Up + walk，左右镜像，比如 Skeleton
	FrontBackIdleWalk,       // Front / Back idle + walk，比如 Nice Guy
	SideIdleWalk,            // 单侧 idle/walk，左右用 flip，比如 Rat / RedJellyfish
	FrontOnlyIdleWalk,        // 只有正面 idle/walk，比如 Pig Boss
	UpDownMirrorIdleWalk,   // 新增：Down/Up 两套动画，每套都可以左右镜像
}

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
	[SerializeField] private EnemyAnimProfile animProfile = EnemyAnimProfile.SingleIdle;

	[SerializeField] private bool useAnimIndexBlendTree = false;
	[SerializeField] private bool useAttackIndexBlendTree = false;

	[SerializeField] private bool useFlipForSideDirections = true;
	[SerializeField] private bool sideSpriteFacesLeft = true;


	private NetworkVariable<int> facing = new NetworkVariable<int>(
		0,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Server
	);
	private NetworkVariable<int> horizontalFacing = new NetworkVariable<int>(
		1,
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

	float targetCheckDelay = 0.5f;

	[Header("Loot")]
	public int coinsToDrop = 1;


	[Header("Inventory")]
	public bool hasInventory = true;

	public NetworkList<FixedString64Bytes> inventory = new NetworkList<FixedString64Bytes>(
	   null,
	   NetworkVariableReadPermission.Everyone,
	   NetworkVariableWritePermission.Owner
   );


	[Header("RandomMovement")]
	public bool randomAlertedMovement = false;
	public bool alerted = false;
	public Vector2 randMovementVector = Vector2.zero;
	public float newRandTimer = 5f;

	protected virtual void Awake()
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

		if (randomAlertedMovement && alerted)
		{
			AlertEnemy();
        }

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

		AddSelfToEnemyList();

		UpdateTargetPeriodic();
	}

	private void UpdateTargetPeriodic()
	{
        GameObject targetOut = null;

        float targetOutDistance = float.MaxValue;

        for (int i = 0; i < targetingRange.GetNumberOfTargets(); i++)
        {
            GameObject currentTarget = targetingRange.GetTarget(i);

            if (currentTarget.GetComponent<CharacterBasic>().alive.Value)
            {
                LayerMask obstacleMask = LayerMask.GetMask("Wall");
                if (CheckLineOfSight(currentTarget.transform, obstacleMask))
                {
                    float currentDistance = DistanceToSelf(currentTarget);

                    if (DistanceToSelf(currentTarget) < targetOutDistance)
                    {
                        targetOut = currentTarget;
                        targetOutDistance = currentDistance;
                    }
                }
            }
        }

		target = targetOut;

		Invoke("UpdateTargetPeriodic", targetCheckDelay);
    }

	private void AddSelfToEnemyList()
	{
		if (GameplayManager.Instance.GetComponent<NetworkObject>().IsSpawned)
		{
			GameplayManager.Instance.AddEnemy(GetComponent<NetworkObject>().NetworkObjectId);
		}
		else
		{
			Debug.Log("WAIT WITH ADD FOR GAMEPLAYMANAGER SPAWN");
			Invoke("AddSelfToCharacterList", 0.1f);
		}
	}

	public override void OnNetworkDespawn()
	{
		if (IsServer)
		{
			DropInventory();

			GameplayManager.Instance.RemoveEnemy(GetComponent<NetworkObject>().NetworkObjectId);
		}

		base.OnNetworkDespawn();
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

		if (randomAlertedMovement)
		{
			AlertEnemy();
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
		return target;
	}

	private bool CheckLineOfSight(Transform target, LayerMask layerMask)
	{
		RaycastHit2D hit = Physics2D.Linecast(transform.position, target.position, layerMask);
		return hit.collider == null || hit.transform == target;
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

protected void SetCanAct(bool value)
{
	canAct = value;
}

protected void SetDeadAnimationState(bool value)
{
	if (!IsServer) return;
	isDeadForAnimation.Value = value;
}

protected void SetMovingAnimationState(bool value)
{
	if (!IsServer) return;
	isMoving.Value = value;
}

protected void StopEnemyMovement()
{
	if (rb != null)
	{
		rb.linearVelocity = Vector2.zero;
	}

	if (IsServer)
	{
		isMoving.Value = false;
	}
}

protected void PlayAttackVisual()
{
	if (!IsServer) return;
	PlayAttackAnimationClientRpc();
}

protected void UpdateFacingOnly(Vector2 direction)
{
	if (!IsServer) return;
	if (direction.sqrMagnitude <= 0.001f) return;

	float x = direction.x;
	float y = direction.y;

	if (animProfile == EnemyAnimProfile.UpDownMirrorIdleWalk)
	{
		if (Mathf.Abs(x) > facingDeadZone)
			horizontalFacing.Value = x < 0 ? 1 : 2;

		if (y > facingDeadZone)
			facing.Value = 3; // UpSet
		else if (y < -facingDeadZone)
			facing.Value = 0; // DownSet

		return;
	}

	if (animProfile == EnemyAnimProfile.FrontOnlyIdleWalk)
	{
		facing.Value = 0;
		return;
	}

	if (animProfile == EnemyAnimProfile.FrontBackIdleWalk)
	{
		if (y > facingDeadZone)
			facing.Value = 3;
		else if (y < -facingDeadZone)
			facing.Value = 0;

		return;
	}

	if (animProfile == EnemyAnimProfile.SideIdleWalk)
	{
		if (Mathf.Abs(x) > facingDeadZone)
			facing.Value = x < 0 ? 1 : 2;

		return;
	}

	if (Mathf.Abs(x) > Mathf.Abs(y) + facingDeadZone)
	{
		facing.Value = x < 0 ? 1 : 2;
	}
	else if (Mathf.Abs(y) > Mathf.Abs(x) + facingDeadZone)
	{
		facing.Value = y > 0 ? 3 : 0;
	}
}

	public virtual void Update()
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

		if (useAttackIndexBlendTree)
		{
			animator.SetFloat("AttackIndex", GetAttackIndex());
		}
	}

	if (spriteRenderer == null) return;

	if (!useFlipForSideDirections)
	{
		spriteRenderer.flipX = false;
		return;
	}

	// Not-so Nice Guy：Down/Up 两套动画，每套都支持左右镜像。
	// 现在的素材特点：
	// DownSet 的左右镜像方向是正常的。
	// UpSet 的左右镜像方向刚好相反，所以 UpSet 要反转一次 flip 逻辑。
	if (animProfile == EnemyAnimProfile.UpDownMirrorIdleWalk)
	{
		bool shouldFlip;

		if (sideSpriteFacesLeft)
		{
			// 默认规则：右边翻转，左边不翻转
			shouldFlip = horizontalFacing.Value == 2;
		}
		else
		{
			// 如果原图默认朝右，则左边翻转，右边不翻转
			shouldFlip = horizontalFacing.Value == 1;
		}

		// 关键：UpSet 的镜像方向和 DownSet 相反
		if (facing.Value == 3) // UpSet
		{
			shouldFlip = !shouldFlip;
		}

		spriteRenderer.flipX = shouldFlip;
		return;
	}

	// 原来的普通左右镜像逻辑
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
	// 0 = Down / Front
	// 1 = Left
	// 2 = Right
	// 3 = Up / Back

	switch (animProfile)
	{
		case EnemyAnimProfile.SingleIdle:
			return 0f;

		case EnemyAnimProfile.FourDirIdle:
			if (dir == 0) return 0f; // Idle_Down / Front
			if (dir == 1) return 1f; // Idle_Left
			if (dir == 2) return 2f; // Idle_Right
			if (dir == 3) return 3f; // Idle_Up / Back
			return 0f;

		case EnemyAnimProfile.ThreeDirMirrorIdleWalk:
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

		case EnemyAnimProfile.FrontBackIdleWalk:
			bool back = dir == 3;

			if (!moving)
				return back ? 1f : 0f; // 0 Idle_Front, 1 Idle_Back

			return back ? 3f : 2f;     // 2 Walk_Front, 3 Walk_Back

		case EnemyAnimProfile.SideIdleWalk:
			return moving ? 1f : 0f;   // 0 Idle, 1 Walk

		case EnemyAnimProfile.FrontOnlyIdleWalk:
			return moving ? 1f : 0f;   // 0 Idle_Front, 1 Walk_Front
		case EnemyAnimProfile.UpDownMirrorIdleWalk:
			{
				bool upSet = dir == 3;

				if (!moving)
					return upSet ? 1f : 0f; // 0 Idle_DownSet, 1 Idle_UpSet

				return upSet ? 3f : 2f;     // 2 Walk_DownSet, 3 Walk_UpSet
			}
	}

	return 0f;
}

private float GetAttackIndex()
{
	int dir = facing.Value;

	switch (animProfile)
	{
		case EnemyAnimProfile.FrontBackIdleWalk:
			return dir == 3 ? 1f : 0f; 
			// 0 Attack_Front
			// 1 Attack_Back
		case EnemyAnimProfile.UpDownMirrorIdleWalk:
			return dir == 3 ? 1f : 0f;
			// 0 Attack_DownSet
			// 1 Attack_UpSet
		default:
			if (dir == 0) return 0f; // Attack_Down / Front
			if (dir == 1) return 1f; // Attack_Left
			if (dir == 2) return 2f; // Attack_Right
			if (dir == 3) return 3f; // Attack_Up / Back
			return 0f;
	}
}

[SerializeField] private float moveThreshold = 0.05f;
[SerializeField] private float facingDeadZone = 0.15f;

protected void UpdateFacingFromMove(Vector2 movementVector)
{
	if (!IsServer) return;
	// facing = DownSet / UpSet;
	// horizontalFacing = Left / Right;
	isMoving.Value = movementVector.sqrMagnitude > moveThreshold * moveThreshold;

	if (!isMoving.Value) return;

	float x = movementVector.x;
	float y = movementVector.y;

	if (animProfile == EnemyAnimProfile.FrontOnlyIdleWalk)
	{
		facing.Value = 0;
		return;
	}

	if (animProfile == EnemyAnimProfile.FrontBackIdleWalk)
	{
		if (y > facingDeadZone)
			facing.Value = 3; // Back
		else if (y < -facingDeadZone)
			facing.Value = 0; // Front

		return;
	}

	if (animProfile == EnemyAnimProfile.SideIdleWalk)
	{
		if (Mathf.Abs(x) > facingDeadZone)
			facing.Value = x < 0 ? 1 : 2;

		return;
	}
	if (animProfile == EnemyAnimProfile.UpDownMirrorIdleWalk)
	{
		// 左右只控制 flip
		if (Mathf.Abs(x) > facingDeadZone)
			horizontalFacing.Value = x < 0 ? 1 : 2;

		// 上下控制用哪一套动画：DownSet / UpSet
		if (y > facingDeadZone)
			facing.Value = 3; // UpSet
		else if (y < -facingDeadZone)
			facing.Value = 0; // DownSet

		return;
	}
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
			if (randomAlertedMovement && !alerted)
			{
				AlertEnemy();
			}

			float distance = DistanceToSelf(target);
			Vector2 dir = DirToTarget();

			if(randomAlertedMovement && alerted)
			{
                randMovementVector = dir;
            }

			Vector2 strafeDir = new Vector2(-dir.y, dir.x) * strafeDirection;

			if (distance > standingRange)
				ApplyMoveVector(dir * speed + strafeDir * strafeSpeed);
			else if (distance < standingRange * 0.8f)
				ApplyMoveVector(-dir * speed + strafeDir * strafeSpeed);
			else
				ApplyMoveVector(strafeDir * strafeSpeed);

			if (distance <= attackRange){
				UpdateFacingOnly(dir);
				AttemptAttack();
			}
			
		}
		else
		{
			if (randomAlertedMovement && alerted)
			{
				ApplyMoveVector(randMovementVector * speed);

            } else
			{
				ApplyMoveVector(Vector2.zero);
			}
		}
	}

    private void OnCollisionEnter2D(Collision2D collision)
    {
		/*
		Debug.Log("Collision?");

		Debug.Log(randomAlertedMovement);
        Debug.Log(alerted);
        Debug.Log(collision.gameObject.layer == 11);
        Debug.Log(target == null);
		*/

        if (randomAlertedMovement && alerted && collision.gameObject.layer == 11 && target == null)
		{
			RandVectorBounce(collision.contacts[0].normal);
		}
    }


	private void RandomizeRandPeriodic()
	{
		RandomizeRandVector();

		if (randomAlertedMovement && alerted)
		{
			Invoke("RandomizeRandPeriodic", UnityEngine.Random.Range(0,newRandTimer));
		}
    }

	private void RandomizeRandVector()
	{
        float angle = UnityEngine.Random.Range(0, 360);

		SetRandVectorByAngle(angle);

		//Debug.Log("RANDOMIZE");
    }

	private void RandVectorBounce(Vector2 normal)
	{
		float normalAngle = FFUtilities.CounterClockwiseAngle(new Vector2(1,0), normal);

		float oldAngle = FFUtilities.CounterClockwiseAngle(-randMovementVector, normal);

		float newAngle = normalAngle - oldAngle;

        SetRandVectorByAngle(newAngle);
    }

	private void SetRandVectorByAngle(float angle)
	{
		randMovementVector = new Vector2((Mathf.Cos(Mathf.Deg2Rad * angle)), Mathf.Sin(Mathf.Deg2Rad * angle));
    }

	private void AlertEnemy()
	{
		alerted = true;

		RandomizeRandPeriodic();
	}

    //applies the desired movement vector to motion, but takes into account knockback
    protected void ApplyMoveVector(Vector2 movementVector)
	{
		rb.linearVelocity = movementVector + knockbackVector;
		UpdateFacingFromMove(movementVector);
	}

	private void DecayKnockbackVector()
	{
		if (knockbackVector.magnitude > 0.01)
		{
			knockbackVector -= knockbackVector.normalized * knockbackDecayMultiplier * Time.deltaTime * 10;
		}
		else
		{
			knockbackVector = Vector2.zero;
		}
	}

	public void AddToInventory(string itemName)
	{
		AddToInventoryOwnerRpc(itemName);
	}

	[Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
	public void AddToInventoryOwnerRpc(string itemName)
	{
		inventory.Add(itemName);
	}

	public void RemoveFromInventory(string itemName)
	{
		RemoveFromInventoryOwnerRpc(itemName);
	}

	[Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
	public void RemoveFromInventoryOwnerRpc(string itemName)
	{
		inventory.Remove(itemName);
	}

	public void DropInventory()
	{
		for (int i = 0; i < inventory.Count; i++)
		{
			string toDrop = inventory[i].ToString();

			Debug.Log("Trying to drop: " + toDrop);
			SpawnerUtil.Instance.NetworkSpawnGameObject(toDrop, transform.position);
		}
	}
}


