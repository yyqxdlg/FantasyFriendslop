using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class CharacterBasic : Spawnable
{
	public bool isMe = false;

	public float speed = 5f;

	public float attackCooldown = 1f;

	private float attackCooldownCurr = 1f;

	public float maxHealth = 10f;

	[SerializeField] private Healthbar healthBar;

	public NetworkVariable<int> coinCount = new NetworkVariable<int>(
		1,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner
	);

	public NetworkVariable<float> health = new NetworkVariable<float>(
		1,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner
	);

	public NetworkVariable<bool> alive = new NetworkVariable<bool>(
		true,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner
	);

	[Header("Spawn Invincibility")]
	[SerializeField] private float spawnInvincibleDuration = 3f;
	private float invincibleTimer = 0f;

	public bool IsInvincible => invincibleTimer > 0f;

	public Vector2 mousePos = Vector2.zero;
	private Vector2 weaponPos = Vector2.zero;
	[SerializeField] private float weaponDistFromCenter = 1;
	[SerializeField] private WeaponSprite weaponScript;

	//for animation
	public NetworkVariable<int> facing = new NetworkVariable<int>(
		0,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner
	);

	public NetworkVariable<bool> isMoving = new NetworkVariable<bool>(
			false,
			NetworkVariableReadPermission.Everyone,
			NetworkVariableWritePermission.Owner
	);

	[Header("Attack")]

	[SerializeField] private string projectileSpawnableName;

	[SerializeField] private string summonPrefabName;

	private Rigidbody2D rb;
	private Vector2 movement;
	private Vector2 lastMoveDirection = Vector2.down;
	[SerializeField] private Animator animator;

	private Camera cam;

	public bool shooting;

	public bool[] attemptingAbilities = new bool[] { false };

	public float[] abilityCooldownsMax = new float[] { 10 };

	private float[] abilityCooldownsCurrent = new float[] { 0 };

	public int ghostType = 0;



	[Header("Sounds")]
	[SerializeField] private string attackSoundName;
	[SerializeField] private float attackSoundVolume = 1f;
	[SerializeField] private float attackSoundRange = 20f;
	[SerializeField] private string abilitySoundName = null;

	[Header("Inventory")]
	public NetworkList<FixedString64Bytes> inventory = new NetworkList<FixedString64Bytes>(
		null,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner
	);

	[Header("Hit Feedback")]
	[SerializeField] private string hurtParticleName = "Player Hurt";
	[SerializeField] private float hurtParticleDuration = 0.35f;
	
	[Header("Knockdown")]
	[SerializeField] private float knockdownVelocityDecay = 10f;

private float knockdownTimer = 0f;
private Vector2 knockdownVelocity = Vector2.zero;
public virtual void Awake()
{
	rb = GetComponent<Rigidbody2D>();

	if (animator == null)
		animator = GetComponent<Animator>();

	cam = Camera.main;

	if (healthBar == null)
		healthBar = GetComponentInChildren<Healthbar>(true);
}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		NetworkObject netObj = GetComponent<NetworkObject>();

		Debug.Log(
			$"CharacterBasic OnNetworkSpawn: {name}, " +
			$"IsServer={IsServer}, " +
			$"IsOwner={IsOwner}, " +
			$"OwnerClientId={OwnerClientId}, " +
			$"IsPlayerObject={(netObj != null && netObj.IsPlayerObject)}, " +
			$"IsSpawned={(netObj != null && netObj.IsSpawned)}"
		);

		if (IsOwner)
		{
			health.Value = maxHealth;

			// 玩家自己的世界血条不一定存在，所以必须判空
			if (healthBar != null)
			{
				healthBar.Hide();
			}

			coinCount.Value = 0;
			UpdateCoinVisual();
		}

		if (IsOwner)
		{
			if(AudioManager.Instance != null)
			{
                AudioManager.Instance.player = gameObject;
            }

            isMe = true;

            InGameUI.Instance.SetType(ghostType);
		}

		SpawnBehavior();

		if (IsServer)
		{
			AddSelfToCharacterList();
		}
	}

	private void AddSelfToCharacterList()
	{
		if (GameplayManager.Instance.GetComponent<NetworkObject>().IsSpawned)
		{
            GameplayManager.Instance.AddPlayerCharacter(GetComponent<NetworkObject>().NetworkObjectId);
        } else
		{
			Debug.Log("WAIT WITH ADD FOR GAMEPLAYMANAGER SPAWN");
			Invoke("AddSelfToCharacterList", 0.1f);
		}
	}
	public override void OnNetworkDespawn()
	{
		if (IsServer)
		{
			GameplayManager.Instance.RemovePlayerCharacter(GetComponent<NetworkObject>().NetworkObjectId);

			DropInventory();
		}

		base.OnNetworkDespawn();
	}

	// start as dead, do animation, then become alive
	private void SpawnBehavior()
	{
		if (IsOwner)
		{
			alive.Value = false;
		}

		animator.Play("Spawn");

		AudioManager.Instance.PlaySound("birth", transform.position);

		//animator.Update(0f);

		float spawnClipLength = animator.GetCurrentAnimatorClipInfo(0)[0].clip.length;

		Debug.Log("Clip? " + animator.GetCurrentAnimatorClipInfo(0)[0].clip.name);

		Invoke("BecomeAlive", spawnClipLength);
	}

	private void BecomeAlive()
	{
		if (IsOwner)
		{
			alive.Value = true;
			StartInvincibility(spawnInvincibleDuration);
		}
	}

	public void StartInvincibility(float duration)
	{
		if (!IsOwner) return;

		invincibleTimer = Mathf.Max(invincibleTimer, duration);
	}

	public virtual void Update()
	{
		UpdateHealth();

		UpdateAnimatorVisuals();
		if (!IsOwner) return;

		if (invincibleTimer > 0f)
		{
			invincibleTimer -= Time.deltaTime;
		}

		cam.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, -10) ;

		//the player is dead
		if (!alive.Value)
		{
			movement = Vector2.zero;
			isMoving.Value = false;
			return;
		}
		if (knockdownTimer > 0f)
		{
				knockdownTimer -= Time.deltaTime;

				movement = Vector2.zero;
				isMoving.Value = false;
				shooting = false;

				return;
		}
		movement.x = Input.GetAxisRaw("Horizontal");
		movement.y = Input.GetAxisRaw("Vertical");
		// update direction
		if (movement.x > 0)
		{
				facing.Value = 2; // Right
		}
		else if (movement.x < 0)
		{
				facing.Value = 1; // Left
		}
		else if (movement.y > 0)
		{
				facing.Value = 3; // Up
		}
		else if (movement.y < 0)
		{
				facing.Value = 0; // Down
		}

		// update isMoving
		isMoving.Value = movement != Vector2.zero;

		// update mouse position
		updateMousePos();

		//update weapon position
		updateWeaponPos();

		// if (movement != Vector2.zero)
		// {
		// 	lastMoveDirection = movement.normalized;
		// }

		// if (animator != null)
		// {
		// 	animator.SetFloat("Speed", movement.magnitude);
		// }

		if (Input.GetMouseButtonDown(0))
		{
			shooting = true;
		}

		if (Input.GetMouseButtonUp(0))
		{
			shooting = false;
		}

		if (shooting)
		{
			AttemptAttack();
		}

		if (Input.GetKeyDown(KeyCode.E))
		{
			attemptingAbilities[0] = true;
		}

		if (Input.GetKeyUp(KeyCode.E))
		{
			attemptingAbilities[0] = false;
		}

		if (shooting)
		{
			AttemptAttack();
		}

		if (attemptingAbilities[0])
		{
			AttemptAbility(0);
		}

		if (attackCooldownCurr > 0f)
		{
			attackCooldownCurr -= Time.deltaTime;
		}

		for (int i = 0; i < attemptingAbilities.Length; i++)
		{
			if (abilityCooldownsCurrent[i] > 0f)
			{
				abilityCooldownsCurrent[i] -= Time.deltaTime;
			}
		}

		InGameUI.Instance.setText(abilityCooldownsCurrent[0].ToString("F2"));
	}

	private void UpdateHealth()
	{
		if (IsOwner)
		{
			InGameUI.Instance.SetHealthMax(maxHealth);
			InGameUI.Instance.SetHealthValue(health.Value);
		} else
		{
			healthBar.UpdateHealthBar(health.Value, maxHealth);
		}

	}

	public void AddCoin(int numCoin)
	{
		AddCoinOwnerRpc(numCoin);
	}

	[Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
	private void AddCoinOwnerRpc(int numCoin)
	{
		coinCount.Value += numCoin;
		UpdateCoinVisual();
	}

	private void UpdateCoinVisual()
	{
		InGameUI.Instance.SetCoins(coinCount.Value);
	}

	public void TakeDamage(float damage)
	{
		TakeDamageOwnerRpc(damage);
	}

	[Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
	private void TakeDamageOwnerRpc(float damage)
	{
		Debug.Log("TAKE DAMAGE " + damage);
		if (!alive.Value) return;
		if (invincibleTimer > 0f) return;

		float oldHealth = health.Value;

		health.Value = Mathf.Max(0, health.Value - damage);

		if (health.Value < oldHealth)
		{
			PlayHurtFeedback();
		}

		if (health.Value <= 0)
		{
			Die();
		}
	}

private void PlayHurtFeedback()
{
	AudioManager.Instance.PlaySound("hit", transform.position);

	if (string.IsNullOrEmpty(hurtParticleName)) return;
	if (ParticleManager.Instance == null) return;

	ParticleManager.Instance.PlayParticle(
		hurtParticleName,
		transform.position,
		hurtParticleDuration,
		gameObject
	);
}

	public void HealAmount(float heal)
	{
		HealAmountOwnerRpc(heal);
	}

	[Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
	public void HealAmountOwnerRpc(float heal)
	{
		if (!alive.Value) return;

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

	// update animator
	void UpdateAnimatorVisuals()
	{
			if (animator == null) return;

			animator.SetBool("IsMoving", isMoving.Value);
			animator.SetInteger("Facing", facing.Value);
			animator.SetBool("IsDead", !alive.Value);
	}

	public void Die()
	{
		if (!IsOwner) return;

		alive.Value = false;

		animator.Play("Die");
		animator.Update(0f);

		float deathClipLength = animator.GetCurrentAnimatorClipInfo(0)[0].clip.length;

		AudioManager.Instance.PlaySound("death", transform.position, 0.5f);

		//Debug.Log("Clip? " + animator.GetCurrentAnimatorClipInfo(0)[0].clip.name);

		rb.linearVelocity = Vector2.zero;

		Invoke("BecomeGhost", deathClipLength);
	}

	private void BecomeGhost()
	{
		SpawnerUtil.Instance.NetworkSpawnGameObject("Ghost_" + ghostType.ToString(), gameObject.transform.position, gameObject.GetComponent<NetworkObject>().OwnerClientId, ulong.MaxValue);

		NetworkDestroy();
	}

	void FixedUpdate()
{
	if (!IsOwner) return;

	if (!alive.Value)
	{
		rb.linearVelocity = Vector2.zero;
		return;
	}

	if (knockdownTimer > 0f)
	{
		rb.linearVelocity = knockdownVelocity;
		knockdownVelocity = Vector2.Lerp(
			knockdownVelocity,
			Vector2.zero,
			knockdownVelocityDecay * Time.fixedDeltaTime
		);
		return;
	}

	rb.linearVelocity = movement.normalized * speed;
}
public void ApplyKnockdown(Vector2 velocity, float duration)
{
	ApplyKnockdownOwnerRpc(velocity, duration);
}

[Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
private void ApplyKnockdownOwnerRpc(Vector2 velocity, float duration)
{
	if (!alive.Value) return;

	knockdownVelocity = velocity;
	knockdownTimer = Mathf.Max(knockdownTimer, duration);

	movement = Vector2.zero;
	isMoving.Value = false;
	shooting = false;
}
	void AttemptAttack()
	{
		if (attackCooldownCurr <= 0)
		{
			Attack();
			attackCooldownCurr = attackCooldown;
		}
	}

	public void updateMousePos()
	{
		mousePos = cam.ScreenToWorldPoint(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
	}

	void updateWeaponPos()
	{
		if (!IsOwner) { return; }

		if(weaponScript == null) { return; }

		Vector2 playerPos2D = new Vector2(gameObject.transform.position.x, gameObject.transform.position.y);

		Vector2 dirVector = (mousePos - playerPos2D).normalized * weaponDistFromCenter;

		weaponPos = playerPos2D + dirVector;

		weaponScript.updatePosAndRot(weaponPos, dirVector);
	}


	public virtual void Attack()
	{
		updateMousePos();

		SpawnerUtil.Instance.NetworkSpawnGameObject(projectileSpawnableName, weaponPos, OwnerClientId, gameObject.GetComponent<NetworkObject>().NetworkObjectId);

		PlayAttackSound();

		PlayAttackAnimation();
	}

	public void PlayAttackSound()
	{
		AudioManager.Instance.PlaySound(attackSoundName, (Vector2)gameObject.transform.position, attackSoundVolume, attackSoundRange);
	}

	public void PlayAttackAnimation()
	{
		weaponScript.PlayAnimation();
	}

	void AttemptAbility(int abilityId)
	{
		if (abilityCooldownsCurrent[abilityId] <= 0)
		{
			DoAbility(abilityId);
			abilityCooldownsCurrent[abilityId] = abilityCooldownsMax[abilityId];
		}
	}

	public virtual void DoAbility(int abilityId)
	{
		if(abilityId == 0)
		{
			updateMousePos();

			SpawnerUtil.Instance.NetworkSpawnGameObject(summonPrefabName, gameObject.transform.position, OwnerClientId, gameObject.GetComponent<NetworkObject>().NetworkObjectId);

			PlayAttackAnimation();

			PlayAbilitySound();
		}
	}

	public virtual void PlayAbilitySound()
	{
		if (abilitySoundName != null)
		{
			AudioManager.Instance.PlaySound(abilitySoundName, transform.position);
		}
	}

	public bool CheckIfInInventory(string itemName)
	{
		for (int i = 0; i < inventory.Count; i++)
		{
			string currItem = inventory[i].ToString();

			if(currItem.Equals(itemName))
			{
				return true;
			}
		}

		return false;
	}

	public void AddToInventory(string itemName)
	{
		AddToInventoryOwnerRpc(itemName);
	}

	[Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
	public void AddToInventoryOwnerRpc(string itemName)
	{
		inventory.Add(itemName);

        InGameUI.Instance.SetHasKey(CheckIfInInventory("DoorKey"));
    }

	public void RemoveFromInventory(string itemName)
	{
		RemoveFromInventoryOwnerRpc(itemName);
	}

	[Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
	public void RemoveFromInventoryOwnerRpc(string itemName)
	{
		inventory.Remove(itemName);

        InGameUI.Instance.SetHasKey(CheckIfInInventory("DoorKey"));
    }

	private void DropInventory()
	{
		for (int i = 0; i < inventory.Count; i++)
		{
			string toDrop = inventory[i].ToString();

			SpawnerUtil.Instance.NetworkSpawnGameObject(toDrop, transform.position);
		}

		DropCoins();
	}

	private void DropCoins()
	{
		int coinsLeft = coinCount.Value;

		int hundreds = (int) Math.Floor( (double) coinsLeft / 100);

		coinsLeft -= hundreds * 100;

		int tens = (int)Math.Floor((double)coinsLeft / 10); ;

		coinsLeft -= tens * 10;

		for (int i = 0; i < hundreds; i++)
		{
			SpawnerUtil.Instance.NetworkSpawnGameObject("Coin x100", transform.position);
		}

		for (int i = 0; i < tens; i++)
		{
			SpawnerUtil.Instance.NetworkSpawnGameObject("Coin x10", transform.position);
		}

		for (int i = 0; i < coinsLeft; i++)
		{
			SpawnerUtil.Instance.NetworkSpawnGameObject("Coin", transform.position);
		}
	}

	public void Teleport(Vector3 posTo)
	{
		TeleportOwnerRpc(posTo);
	}

	[Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
	public void TeleportOwnerRpc(Vector3 posTo)
	{
		gameObject.transform.position = posTo;
	}
}