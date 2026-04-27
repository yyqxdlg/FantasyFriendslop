using UnityEngine;
using Unity.Netcode;

public class SwordSwing : BasicProjectile
{

    private float t = 0;

    public float swingAngleWidth = 45;

    public float knockBack = 1;

    private float offsetAngle;

    public override void FireBehaviour()   
    {
        despawnOnHit = false;

        preventRepeatedHits = true;

        lifeTime = 1 / speed;

        offsetAngle = FFUtilities.CounterClockwiseAngle(movementDir, new Vector2(1, 0));

        if (!IsOwner) { return; }
        Invoke("NetworkDestroy", lifeTime);
    }

    private float AngleFromT(float t)
    {
        return offsetAngle + ((-(swingAngleWidth / 2) + t * swingAngleWidth));
    }

    public void Update()
    {
        if (!IsOwner) return;

        GameObject creator = GetCreator();

        if (creator == null)
        {
            if (IsServer && IsSpawned)
            {
                NetworkDestroy();
            }

            return;
        }

        transform.position = creator.transform.position;

        t += Time.deltaTime * speed;

        transform.rotation = Quaternion.Euler(0, 0, AngleFromT(t));
    }

    public override void OnEnemyHitEffect(EnemyBasic enemyHitScript)
    {
        
        GameObject creator = GetCreator();

        if (creator == null)
        {
            if (IsServer && IsSpawned)
            {
                NetworkDestroy();
            }

            return;
        }
        enemyHitScript.TakeDamage(damage);

        Vector2 knockBackVector = (enemyHitScript.gameObject.transform.position - GetCreator().transform.position) * 1 * knockBack;

        enemyHitScript.KnockBack(knockBackVector);
    }
}
