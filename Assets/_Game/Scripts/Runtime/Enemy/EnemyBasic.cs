using UnityEngine;
using Unity.Netcode;
using System;

public class EnemyBasic : Spawnable
{
    // add roomid, every own a roomid
    public static event Action<int, Vector3> OnEnemyDiedInRoom;

    [SerializeField] private int roomId = -1;

    private bool hasDied = false;

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


    protected void Awake()
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

	public virtual void TakeDamage(float Damage)
	{
		TakeDamageServerRpc(Damage);
	}

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TakeDamageServerRpc(float Damage)
	{
        health.Value -= Damage;

        if (health.Value <= 0)
        {
            Die();
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

        OnEnemyDiedInRoom?.Invoke(roomId, transform.position);

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
    DecayKnockbackVector();
    healthBar.UpdateHealthBar(health.Value, maxHealth);
    ServerUpdate();
}

protected virtual void ServerUpdate()
{
    if (!IsServer) return;

    attackCooldownCurr -= Time.deltaTime;
    target = NearestLivingTarget();

    if (target != null)
    {
        float distance = DistanceToSelf(target);
        Vector2 dir = DirToTarget();
        Vector2 strafeDir = new Vector2(-dir.y, dir.x) * strafeDirection;

        if (distance > standingRange)
            ApplyMoveVector(dir * speed + strafeDir * strafeSpeed);
        else if (distance < standingRange * 0.8f)
            ApplyMoveVector(-dir * speed + strafeDir * strafeSpeed);
        else
            ApplyMoveVector(strafeDir * strafeSpeed);

        if (distance <= attackRange)
            AttemptAttack();
    }
    else
    {
        ApplyMoveVector(Vector2.zero);
    }
}

	//applies the desired movement vector to motion, but takes into account knockback
    private void ApplyMoveVector(Vector2 movementVector)
    {
		rb.linearVelocity = movementVector + knockbackVector;
    }

	private void DecayKnockbackVector()
	{
        if (knockbackVector.magnitude > 0.01)
		{
			knockbackVector -= knockbackVector.normalized * knockbackDecayMultiplier * Time.deltaTime;
		}
        else
        {
			knockbackVector = Vector2.zero;
        }
    }
}


