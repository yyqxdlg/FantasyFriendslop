using UnityEngine;
using Unity.Netcode;

public class RangedEnemy : EnemyBasic
{
    [Header("Ranged Attack")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint; // Optional: where bullets spawn from
    [SerializeField] private float bulletSpeed = 10f;

    public override void Attack()
    {
        if (target == null) return;

        // Determine spawn position
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        // Instantiate bullet
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        // Configure bullet before spawning
        EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
        if (bulletScript != null)
        {
            bulletScript.SetCreator(gameObject);
            bulletScript.SetDirection(DirToTarget());
            bulletScript.damage = attackDamage;
            bulletScript.speed = bulletSpeed;
        }

        // Spawn it on the network
        NetworkObject netObj = bullet.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
    }
}