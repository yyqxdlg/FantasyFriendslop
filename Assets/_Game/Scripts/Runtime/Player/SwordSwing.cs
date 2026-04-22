using UnityEngine;
using Unity.Netcode;

public class SwordSwing : BulletMoveMP
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

        Invoke("networkDestroy", lifeTime);
    }

    private float AngleFromT(float t)
    {
        return offsetAngle + ((-(swingAngleWidth / 2) + t * swingAngleWidth));
    }

    public void Update()
    {
        if (!IsServer) { return; }

        gameObject.transform.position = GetCreator().transform.position;

        t += Time.deltaTime * speed;

        gameObject.transform.rotation = Quaternion.Euler(0, 0, AngleFromT(t));
    }

    public override void OnEnemyHitEffect(EnemyBasic enemyHitScript)
    {
        enemyHitScript.TakeDamage(damage);

        Vector2 knockBackVector = (enemyHitScript.gameObject.transform.position - GetCreator().transform.position) * 1000 * knockBack;

        enemyHitScript.KnockBack(knockBackVector);
    }
}
