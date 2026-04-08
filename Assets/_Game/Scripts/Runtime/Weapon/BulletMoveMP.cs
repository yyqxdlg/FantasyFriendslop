
using System;
using Unity.Netcode;
using UnityEngine;

public class BulletMoveMP : NetworkBehaviour
{

	public float speed = 8f;
	public float lifeTime = 2f;

	private Rigidbody2D rb;

    public GameObject creator;

	void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
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
        if (collision.gameObject != creator)
        {

            if (IsServer)
            {
                gameObject.GetComponent<NetworkObject>().Despawn(true);
            }
        }
    }

    private void networkDestroy()
    {
        gameObject.GetComponent<NetworkObject>().Despawn(true);
    }

}
