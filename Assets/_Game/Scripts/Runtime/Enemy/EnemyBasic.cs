using UnityEngine;
using Unity.Netcode;

public class EnemyBasic : NetworkBehaviour
{

	public float maxHealth = 10.0f;

	public float speed = 2f;

	public NetworkVariable<float> health = new NetworkVariable<float>();

	[SerializeField] private Healthbar healthBar;

	[SerializeField] private GameObject targetingRangeObject;

	private EnemyTargetRange targetingRange;

	private GameObject target;

	private Rigidbody2D rb;


    [Header("Attack")]
    public float attackCooldown = 1f;

    private float attackCooldownCurr = 1f;

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
        health.Value -= 1;

        if (health.Value <= 0)
        {
			Die();
        }
    }

	public void Die()
	{
        gameObject.GetComponent<NetworkObject>().Despawn(true);
    }

	private float DistanceToSelf(GameObject obj)
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
		if(attackCooldownCurr <= 0)
		{
			attackCooldownCurr = attackCooldown;
			Attack();
		}
	}

	public void Attack()
	{
        target.GetComponent<CharacterBasic>().TakeDamage(attackDamage);
	}

	// Update is called once per frame
	void Update()
	{
		healthBar.UpdateHealthBar(health.Value, maxHealth);

		if (!IsServer) return;

		attackCooldownCurr -= Time.deltaTime;

		target = NearestLivingTarget();

		if (target != null)
		{
			rb.linearVelocity = DirToTarget() * speed;

			if(DistanceToSelf(target) < attackRange)
			{
				AttemptAttack();
			}
		}

	}

}
