using UnityEngine;
using Unity.Netcode;

public class SwordSwing : BulletMoveMP
{

    private float t = 0;

    public float swingAngleWidth = 45;

    public override void FireBehaviour()   
    {
        despawnOnHit = false;

        lifeTime = 1 / speedFromProjectile;

        Invoke("networkDestroy", lifeTime);
    }

    private float angleFromT(float t)
    {
        return (-(swingAngleWidth / 2) + t * swingAngleWidth);
    }

    public void Update()
    {
        gameObject.transform.position = creator.transform.position;

        t += Time.deltaTime * speedFromProjectile;

        gameObject.transform.rotation = Quaternion.Euler(0, 0, angleFromT(t));
    }
}
