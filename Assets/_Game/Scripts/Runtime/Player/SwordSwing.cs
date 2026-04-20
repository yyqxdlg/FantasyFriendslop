using UnityEngine;
using Unity.Netcode;

public class SwordSwing : BulletMoveMP
{

    public override void AwakeBehaviour()
    {
        rb.linearVelocity = new Vector2(1, 0) * speed;

        despawnOnHit = false;
    }

    public void Update()
    {
        
    }
}
