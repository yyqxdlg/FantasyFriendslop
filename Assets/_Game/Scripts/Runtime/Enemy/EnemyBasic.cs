using UnityEngine;
using Unity.Netcode;

public class EnemyBasic : Spawnable
{

	public float maxHealth = 10.0f;

	public float speed = 2f;

	public NetworkVariable<float> health = new NetworkVariable<float>();

	[SerializeField] protected Healthbar healthBar;

	[SerializeField] private GameObject targetingRangeObject;

    protected EnemyTargetRange targetingRange;

	protected GameObject target;

    protected Rigidbody2D rb;
    
    [Header("Movement")]
    public float standingRange = 0f;

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


    void Awake()
	{
		healthBar = GetComponentInChildren<Healthbar>();

		rb = GetComponent<Rigidbody2D>();

        targetingRange = targetingRangeObject.GetComponent<EnemyTargetRange>();
    }

	public override void OnNetworkSpawn()
	{
		if (!IsServer) return;

		health.Value = maxHealth;
	}

	public void TakeDamage(float Damage)
	{
        health.Value -= Damage;

        if (health.Value <= 0)
        {
			Die();
        }
    }

    public void KnockBack(Vector2 knockVector)
    {
        rb.AddForce(knockVector);
    }

	public void Die()
	{
        gameObject.GetComponent<NetworkObject>().Despawn(true);
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
        healthBar.UpdateHealthBar(health.Value, maxHealth);

        if (!IsServer) return;

        attackCooldownCurr -= Time.deltaTime;
        target = NearestLivingTarget();

        if (target != null)
        {
            float distance = DistanceToSelf(target);
            Vector2 dir = DirToTarget();

            // Perpendicular direction (for strafing)
            Vector2 strafeDir = new Vector2(-dir.y, dir.x) * strafeDirection;

            if (distance > standingRange)
            {
                //Move toward target and slighttly strafe
                rb.linearVelocity = (dir * speed + strafeDir * strafeSpeed);
            }
            else if (distance < standingRange * 0.8f)
            {
                //move away and strafe
                rb.linearVelocity = (-dir * speed + strafeDir * strafeSpeed);
            }
            else
            {
                //stay inside range
                rb.linearVelocity = strafeDir * strafeSpeed;
            }

            if (distance <= attackRange)
            {
                AttemptAttack();
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}


