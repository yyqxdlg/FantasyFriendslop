using UnityEngine;

public class EnemySlime : EnemyBasic
{
    [Header("Slime - Particles")]
    [SerializeField] private string deathParticleName = "SlimeDeath";
   

    public override void Die()
    {
        if (ParticleManager.Instance != null)
            ParticleManager.Instance.PlayParticle(deathParticleName, transform.position);

        base.Die();
    }

    
}