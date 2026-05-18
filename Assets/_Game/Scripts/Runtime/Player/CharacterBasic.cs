using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.Rendering;

public class CharacterBasic : Spawnable
{
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

	[SerializeField] private RuntimeAnimatorController animController;

    

    [Header("Attack")]

    [SerializeField] private string projectileSpawnableName;

    [SerializeField] private string summonPrefabName;

	private Rigidbody2D rb;
	private Vector2 movement;
	private Vector2 lastMoveDirection = Vector2.down;
	[SerializeField] private Animator animator;

	private Camera cam;

	private bool shooting;

	private bool[] attemptingAbilities = new bool[] { false };

	public float[] abilityCooldownsMax = new float[] { 10 };

	private float[] abilityCooldownsCurrent = new float[] { 0 };

    [Header("Sounds")]
    [SerializeField] private string attackSoundName;
    [SerializeField] private float attackSoundVolume = 1f;
    [SerializeField] private float attackSoundRange = 20f;

    void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		//animator = GetComponent<Animator>();
		cam = Camera.main;

        healthBar = GetComponentInChildren<Healthbar>();
		
    }

    public override void OnNetworkSpawn()
	{
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
			alive.Value = true;

			healthBar.Hide();

			coinCount.Value = 0;
			UpdateCoinVisual();
		}

        animator.runtimeAnimatorController = animController;

        if (IsOwner)
		{
            AudioManager.Instance.player = gameObject;
        }
	}

    public virtual void Update()
	{
		UpdateHealth();

		UpdateAnimatorVisuals();
        if (!IsOwner) return;

		cam.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, -10) ;

		//the player is dead
		if (!alive.Value)
		{
			movement = Vector2.zero;
			isMoving.Value = false;
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
        if (!alive.Value) return;

        health.Value = Mathf.Max(0, health.Value - damage);

        if (health.Value <= 0)
        {
            Die();
        }
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
			rb.linearVelocity = Vector2.zero;
	}

    void FixedUpdate()
    {
      if (!IsOwner) return;
		if (!alive.Value)
		{
			rb.linearVelocity = new Vector2(0, 0);
			return;
		}
		rb.linearVelocity = movement.normalized * speed;
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
    }

	public void PlayAttackSound()
	{
        AudioManager.Instance.PlaySound(attackSoundName, (Vector2)gameObject.transform.position, attackSoundVolume, attackSoundRange);
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
		}
	}
}