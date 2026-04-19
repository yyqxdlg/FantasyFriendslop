using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.Rendering;

public class CharacterBasic : NetworkBehaviour
{

	[SerializeField] private Transform spawnedObjectPrefab;
	public float speed = 5f;

	public float attackCooldown = 1f;

	private float attackCooldownCurr = 1f;

	public float maxHealth = 10f;

    [SerializeField] private Healthbar healthBar;

    public NetworkVariable<float> health = new NetworkVariable<float>();

	public NetworkVariable<bool> alive = new NetworkVariable<bool>();

    [Header("Attack")]
	public GameObject bulletPrefab;
	public float bulletOffset = 0.6f;

	private Rigidbody2D rb;
	private Vector2 movement;
	private Vector2 lastMoveDirection = Vector2.down;
	private Animator animator;

	private Camera cam;

	private bool shooting;

	void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		animator = GetComponent<Animator>();
		cam = Camera.main;

        healthBar = GetComponentInChildren<Healthbar>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        health.Value = maxHealth;

		alive.Value = true;
    }

    void Update()
	{
        healthBar.UpdateHealthBar(health.Value, maxHealth);

        if (!IsOwner) return;

		if (!alive.Value) return;

        movement.x = Input.GetAxisRaw("Horizontal");
		movement.y = Input.GetAxisRaw("Vertical");

		if (movement != Vector2.zero)
		{
			lastMoveDirection = movement.normalized;
		}

		if (animator != null)
		{
			animator.SetFloat("Speed", movement.magnitude);
		}

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

	public void Die()
	{
		alive.Value = false;
		rb.linearVelocity = new Vector2 (0, 0);
	}

    void FixedUpdate()
    {
        rb.linearVelocity = movement.normalized * speed;

		if (!alive.Value)
		{
            rb.linearVelocity = new Vector2(0, 0);
        }
    }

    [ServerRpc]
	private void spawnObjectServerRpc(Vector2 spawnPos, Vector2 directionVector)
	{
		Transform spawnedObjectTransform = Instantiate(spawnedObjectPrefab, spawnPos, Quaternion.identity);

		Rigidbody2D bulletRb = spawnedObjectTransform.GetComponent<Rigidbody2D>();

		bulletRb.linearVelocity = bulletRb.linearVelocity.magnitude * directionVector.normalized;

		BulletMoveMP bulletScript = spawnedObjectTransform.GetComponent<BulletMoveMP>();

		bulletScript.SetCreator(gameObject);

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

	void Attack()
	{
        Vector2 mousePos = cam.ScreenToWorldPoint(new Vector2(Input.mousePosition.x, Input.mousePosition.y));

        Vector2 selfPos = new Vector2(transform.position.x, transform.position.y);

        Vector2 directionVector = mousePos - selfPos;

        spawnObjectServerRpc(selfPos, directionVector);
    }
}