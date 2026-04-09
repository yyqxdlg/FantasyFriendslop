using UnityEngine;
using Unity.Netcode;

public class BasicEnemyScript : NetworkBehaviour
{

	public float maxHealth = 10.0f;

	public float speed = 2f;

	public NetworkVariable<float> health = new NetworkVariable<float>();

	[SerializeField] private EnemyHealthBar healthBar;

	[SerializeField] private GameObject targetingRangeObject;

	private EnemyTargetRange targetingRange;

	private GameObject target;

	private Rigidbody2D rb;


	void Awake()
	{
		healthBar = GetComponentInChildren<EnemyHealthBar>();

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
            gameObject.GetComponent<NetworkObject>().Despawn(true);
        }
    }

	private float DistanceToSelf(GameObject obj)
	{
		return Vector2.Distance(gameObject.transform.position, obj.transform.position);
	}

	public GameObject NearestTarget()
	{
		GameObject targetOut = targetingRange.GetTarget(0);
		
		if(targetOut == null) return null;

		float targetOutDistance = DistanceToSelf(targetOut);

		for (int i = 1; i < targetingRange.GetNumberOfTargets(); i++)
		{
			GameObject currentTarget = targetingRange.GetTarget(i);
			float currentDistance = DistanceToSelf(currentTarget);

			if (DistanceToSelf(currentTarget) < targetOutDistance)
			{
				targetOut = currentTarget;
				targetOutDistance = currentDistance;
			}
		}

		return targetOut;
	}

	// Update is called once per frame
	void Update()
	{
		healthBar.UpdateHealthBar(health.Value, maxHealth);

		if (!IsServer) return;

		target = NearestTarget();

		if (target != null)
		{
			rb.linearVelocity = (target.transform.position - gameObject.transform.position).normalized * speed;
		}
	}

}
