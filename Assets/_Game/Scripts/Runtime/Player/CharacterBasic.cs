using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.Rendering;

public class CharacterBasic : NetworkBehaviour
{
	public float speed = 5f;

	public float attackCooldown = 1f;

	private float attackCooldownCurr = 1f;

	public float maxHealth = 10f;

    [SerializeField] private Healthbar healthBar;

    public NetworkVariable<float> health = new NetworkVariable<float>();

	public NetworkVariable<bool> alive = new NetworkVariable<bool>();

	private Vector2 mousePos = Vector2.zero;
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

	[SerializeField] private RuntimeAnimatorController yellowController;
	[SerializeField] private RuntimeAnimatorController greenController;
	[SerializeField] private RuntimeAnimatorController blueController;
	[SerializeField] private RuntimeAnimatorController redController;

	// 0 = Yellow
	// 1 = Green
	// 2 = Blue
	// 3 = Red
	public NetworkVariable<int> characterType = new NetworkVariable<int>(
    0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
	);
    [Header("Attack")]

    [SerializeField] private Transform projectilePrefab;
    public float bulletOffset = 0.6f;

	private Rigidbody2D rb;
	private Vector2 movement;
	private Vector2 lastMoveDirection = Vector2.down;
	[SerializeField] private Animator animator;

	private Camera cam;

	private bool shooting;

	void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		//animator = GetComponent<Animator>();
		cam = Camera.main;

        healthBar = GetComponentInChildren<Healthbar>();
    }

    public override void OnNetworkSpawn()
	{
			if (IsServer)
			{
					health.Value = maxHealth;
					alive.Value = true;
			}

			characterType.OnValueChanged += OnCharacterTypeChanged;

			ApplyCharacterType(characterType.Value);

			if (IsOwner)
			{
					SetCharacterTypeServerRpc(CharacterSelectData.SelectedCharacter);
			}
	}

    void Update()
	{
        healthBar.UpdateHealthBar(health.Value, maxHealth);
		UpdateAnimatorVisuals();
        if (!IsOwner) return;
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


        if (attackCooldownCurr > 0f)
		{
			attackCooldownCurr -= Time.deltaTime;
		}
	}

    public void TakeDamage(float Damage)
    {
		if (!alive.Value) return;

        health.Value -= Damage;

        if (health.Value <= 0)
        {
			Die();
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
		alive.Value = false;
		rb.linearVelocity = new Vector2 (0, 0);
	}

    void FixedUpdate()
    {
			// 
      if (!IsOwner) return;
        

			if (!alive.Value)
			{
        rb.linearVelocity = new Vector2(0, 0);
				return;
      }
			rb.linearVelocity = movement.normalized * speed;
    }

    [ServerRpc]
	private void spawnObjectServerRpc(Vector2 spawnPos, Vector2 directionVector)
	{
		Transform spawnedObjectTransform = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

		BulletMoveMP bulletScript = spawnedObjectTransform.GetComponent<BulletMoveMP>();

		bulletScript.Fire(gameObject, directionVector.normalized, 1);

		spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);
	}

	void AttemptAttack()
	{
		if (attackCooldownCurr <= 0)
		{
            Attack();
			attackCooldownCurr = attackCooldown;
		}
	}

	void updateMousePos()
	{
        mousePos = cam.ScreenToWorldPoint(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
    }

    void updateWeaponPos()
	{
        if (!IsOwner) { return; }

        Vector2 playerPos2D = new Vector2(gameObject.transform.position.x, gameObject.transform.position.y);

        Vector2 dirVector = (mousePos - playerPos2D).normalized * weaponDistFromCenter;

        weaponPos = playerPos2D + dirVector;

		weaponScript.updatePosAndRot(weaponPos, dirVector);
    }


    void Attack()
	{
        updateMousePos();

        Vector2 directionVector = mousePos - weaponPos;

        spawnObjectServerRpc(weaponPos, directionVector);
    }

	// player choose 
	private void ApplyCharacterType(int type)
	{
			if (animator == null)
			{
					animator = GetComponent<Animator>();
			}

			switch (type)
			{
					case 0:
							animator.runtimeAnimatorController = yellowController;
							break;
					case 1:
							animator.runtimeAnimatorController = greenController;
							break;
					case 2:
							animator.runtimeAnimatorController = blueController;
							break;
					case 3:
							animator.runtimeAnimatorController = redController;
							break;
					default:
							animator.runtimeAnimatorController = yellowController;
							break;
			}
	}
	private void OnCharacterTypeChanged(int oldValue, int newValue)
	{
			ApplyCharacterType(newValue);
	}
	[ServerRpc]
	private void SetCharacterTypeServerRpc(int type)
	{
			type = Mathf.Clamp(type, 0, 3);
			characterType.Value = type;
	}
}