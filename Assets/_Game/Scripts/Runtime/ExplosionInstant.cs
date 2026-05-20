using NUnit.Framework;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class ExplosionInstant : Spawnable
{
	public float explodeDelay;

	public float radius;

	public float damage;

	public BlackHoleCollider col;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	public override void OnNetworkSpawn()
	{
		col.explosionScript = gameObject.GetComponent<ExplosionInstant>();

		Invoke("Explode", explodeDelay);

		Animator animator = GetComponent<Animator>();

		float destroyDelay = animator.GetCurrentAnimatorClipInfo(0)[0].clip.length;

		Invoke("NetworkDestroy", destroyDelay);
    }

	private void Explode()
	{
		col.Explode(radius);
	} 

	public void Damage(GameObject target)
	{
		Debug.Log("DAMAGING: " + target.name);

        CharacterBasic playerScript = target.GetComponent<CharacterBasic>();
        EnemyBasic enemyScript = target.GetComponent<EnemyBasic>();

        if (playerScript != null)
        {
            playerScript.TakeDamage(damage);
        }

        if (enemyScript != null)
        {
            enemyScript.TakeDamage(damage);
        }
    }

}
