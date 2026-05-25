using UnityEngine;
using Unity.Netcode;
using System.Linq;
using UnityEngine.UIElements;

public class RangedEnemy : EnemyBasic
{
    [Header("Ranged Attack")]

    [SerializeField] private string bulletPrefabName;
    //[SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float stopDistance = 8f; // Distance to stop and attack
    [SerializeField] private Transform crossHairPivot;
    [SerializeField] private Transform crossHairPoint;

    public NetworkVariable<float> crossHairAngle = new NetworkVariable<float>();

    [Header("Audio")]
    [SerializeField] private string shootSoundName;
    //[SerializeField][Range(0f, 1f)] private float shootVolume = 1f;

    /*
    public override void Attack()
    {
        if (target == null) return;
        if (shootSound != null)
            _audioSource.PlayOneShot(shootSound, shootVolume);

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
    */

    public override void Update()
    {
        base.Update();

        if (IsServer)
        {
            UpdateCrosshairPosition();
        }

        RenderCrosshair();
    }

    private void UpdateCrosshairPosition()
    {
        if (target != null)
        {
            Vector3 dirVector = target.transform.position - transform.position;

            dirVector = dirVector.normalized;

            crossHairAngle.Value = FFUtilities.CounterClockwiseAngle(dirVector, new Vector2(1, 0));
        }
    }

    private void RenderCrosshair()
    {
        crossHairPivot.transform.rotation = Quaternion.Euler(0, 0, crossHairAngle.Value);
    }

    /*
    public override void Attack()
    {
        if (target == null) return;
        if (shootSound != null)
            _audioSource.PlayOneShot(shootSound, shootVolume);

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
    */

    public override void Attack()
    {
        if (target == null) return;
        if (shootSoundName != null)
            AudioManager.Instance.PlaySound(shootSoundName, transform.position);

        SpawnerUtil.Instance.NetworkSpawnGameObject(bulletPrefabName, crossHairPoint.transform.position, 0, GetComponent<NetworkObject>().NetworkObjectId);
    }
}