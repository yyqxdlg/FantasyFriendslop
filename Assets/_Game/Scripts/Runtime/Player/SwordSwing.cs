using UnityEngine;
using Unity.Netcode;

public class SwordSwing : BulletMoveMP
{

    private float t = 0;

    public float swingAngleWidth = 45;

    private float offsetAngle;

    public override void FireBehaviour()   
    {
        despawnOnHit = false;

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
        gameObject.transform.position = creator.transform.position;

        t += Time.deltaTime * speed;

        gameObject.transform.rotation = Quaternion.Euler(0, 0, AngleFromT(t));
    }
}
