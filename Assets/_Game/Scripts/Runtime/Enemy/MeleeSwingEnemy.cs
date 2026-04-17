using UnityEngine;
using Unity.Netcode;

public class MeleeSwingEnemy : EnemyBasic
{
    private enum State
    {
        Moving,
        Charging,
        Attacking
    }

    private State currentState = State.Moving;

    [Header("Swing Attack")]
    public float chargeTime = 1f;
    private float chargeTimer;

    public float lungeForce = 6f;
    public float lungeDuration = 0.2f;
    private float lungeTimer;

    private Vector2 attackDirection;

    void Update()
    {
        healthBar.UpdateHealthBar(health.Value, maxHealth);

        if (!IsServer) return;

        attackCooldownCurr -= Time.deltaTime;
        target = NearestLivingTarget();

        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distance = DistanceToSelf(target);

        switch (currentState)
        {
            case State.Moving:
                HandleMoving(distance);
                break;

            case State.Charging:
                HandleCharging();
                break;

            case State.Attacking:
                HandleAttacking();
                break;
        }
    }
    void HandleMoving(float distance)
    {
        if (distance > attackRange)
        {
            rb.linearVelocity = DirToTarget() * speed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;

            if (attackCooldownCurr <= 0)
            {
                StartCharge();
            }
        }
    }
    void StartCharge()
    {
        currentState = State.Charging;
        chargeTimer = chargeTime;

        rb.linearVelocity = Vector2.zero;

        // Lock direction at start of attack
        attackDirection = DirToTarget();
    }

    void HandleCharging()
    {
        chargeTimer -= Time.deltaTime;

        if (chargeTimer <= 0)
        {
            StartAttack();
        }
    }
    void StartAttack()
    {
        currentState = State.Attacking;
        lungeTimer = lungeDuration;

        attackCooldownCurr = attackCooldown;

        // Do damage instantly (or you can delay this slightly)
        if (target != null && DistanceToSelf(target) <= attackRange * 1.5f)
        {
            target.GetComponent<CharacterBasic>().TakeDamage(attackDamage);
        }
    }

    void HandleAttacking()
    {
        lungeTimer -= Time.deltaTime;

        // Move forward during lunge
        rb.linearVelocity = attackDirection * lungeForce;

        if (lungeTimer <= 0)
        {
            currentState = State.Moving;
            rb.linearVelocity = Vector2.zero;
        }
    }
}