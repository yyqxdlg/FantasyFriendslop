using UnityEngine;

public class EnemySlime : EnemyBasic
{
    [Header("Slime - Particles")]
    [SerializeField] private string deathParticleName = "SlimeDeath";
    [SerializeField] private string attackParticleName = "SlimeAttack";

    public override void Die()
    {
        if (ParticleManager.Instance != null)
            ParticleManager.Instance.PlayParticle(deathParticleName, transform.position);

        base.Die();
    }

    public override void Attack()
    {
        base.Attack();

        if (target != null && ParticleManager.Instance != null)
            ParticleManager.Instance.PlayParticle(attackParticleName, target.transform.position);
    }
}