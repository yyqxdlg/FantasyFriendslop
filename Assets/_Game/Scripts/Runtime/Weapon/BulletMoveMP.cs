
using System;
using Unity.Netcode;
using UnityEngine;

public class BulletMoveMP : NetworkBehaviour
{

	public float speed = 8f;
	public float lifeTime = 2f;
    public float damage = 1f;

    [NonSerialized] public Boolean despawnOnHit = true;

    [NonSerialized] public Rigidbody2D rb;

    public GameObject creator;

	public void Awake()
	{
        rb = GetComponent<Rigidbody2D>();
        AwakeBehaviour();
    }

    public virtual void AwakeBehaviour()
    {
        rb.linearVelocity = new Vector2(1, 0) * speed;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        Invoke("networkDestroy", lifeTime);
    }

    public void SetCreator(GameObject creator)
    {
        this.creator = creator;
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject != creator && !collision.isTrigger)
        {

            if (IsServer)
            {
                EnemyBasic scriptEnemyHit = collision.gameObject.GetComponent<EnemyBasic>();
                if(scriptEnemyHit != null)
                {
                    scriptEnemyHit.TakeDamage(damage);
                }

                if (despawnOnHit)
                {
                    gameObject.GetComponent<NetworkObject>().Despawn(true);
                }
                
            }
        }
    }

    private void networkDestroy()
    {
        gameObject.GetComponent<NetworkObject>().Despawn(true);
    }

}
