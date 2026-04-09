using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.Rendering;

public class MultiPlayerMovement : NetworkBehaviour
{

	[SerializeField] private Transform spawnedObjectPrefab;
	public float speed = 5f;

	public float attackCooldown = 1f;

	private float attackCooldownCurr = 1f;

	[Header("Shoot")]
	public GameObject bulletPrefab;
	public float bulletOffset = 0.6f;

	private Rigidbody2D rb;
	private Vector2 movement;
	private Vector2 lastMoveDirection = Vector2.down;
	private Animator animator;

	private Camera cam;

	void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		animator = GetComponent<Animator>();
		cam = Camera.main;
	}

	void Update()
	{
		if (!IsOwner) return;

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
			Shoot();
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

	void FixedUpdate()
	{
		rb.linearVelocity = movement.normalized * speed;
	}

	void AttemptShoot()
	{
		if (attackCooldown >= 0)
		{

		}
	}

	void Shoot()
	{
        Vector2 mousePos = cam.ScreenToWorldPoint(new Vector2(Input.mousePosition.x, Input.mousePosition.y));

        Vector2 selfPos = new Vector2(transform.position.x, transform.position.y);

        Vector2 directionVector = mousePos - selfPos;

        spawnObjectServerRpc(selfPos, directionVector);
    }
}