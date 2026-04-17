using UnityEngine;
using Unity.Netcode;

public class RangedEnemy : EnemyBasic
{
    [Header("Ranged Attack")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint; 
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float stopDistance = 8f; // Distance to stop and attack



    public override void Attack()
    {
        if (target == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
        if (bulletScript != null)
        {
            bulletScript.SetCreator(gameObject);
            bulletScript.SetDirection(DirToTarget());
            bulletScript.damage = attackDamage;
            bulletScript.speed = bulletSpeed;
        }

        NetworkObject netObj = bullet.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
    }
}