using UnityEngine;

public class PlayerMovement2D : MonoBehaviour
{
    public float speed = 5f;

    [Header("Shoot")]
    public GameObject bulletPrefab;
    public float bulletOffset = 0.6f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private Vector2 lastMoveDirection = Vector2.down;
    private Animator animator;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Shoot();
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movement.normalized * speed;
    }

    void Shoot()
    {
        if (bulletPrefab == null) return;

        Vector3 spawnPosition = transform.position + (Vector3)(lastMoveDirection * bulletOffset);

        GameObject bulletObj = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.SetDirection(lastMoveDirection);
        }
    }
}