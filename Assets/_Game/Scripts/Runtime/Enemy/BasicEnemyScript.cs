using UnityEngine;
using Unity.Netcode;

public class BasicEnemyScript : NetworkBehaviour
{

    public float maxHealth = 10.0f;

    public NetworkVariable<float> health = new NetworkVariable<float>();

    [SerializeField] private EnemyHealthBar healthBar;


    void Awake()
    {
        healthBar = GetComponentInChildren<EnemyHealthBar>();
    }

    // Update is called once per frame
    void Update()
    {
        healthBar.UpdateHealthBar(health.Value, maxHealth);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        health.Value = maxHealth;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (!IsServer) return;

    
        BulletMoveMP impactBulletScript = collision.gameObject.GetComponent<BulletMoveMP>();

        if (impactBulletScript != null)
        {
            health.Value -= 1;

            if(health.Value <= 0)
            {
                gameObject.GetComponent<NetworkObject>().Despawn(true);
            }
        }
        
    }
}
