using UnityEngine;
using Unity.Netcode;

public class RangedEnemy : EnemyBasic
{
    [Header("Ranged Attack")]

    [SerializeField] private string bulletPrefabName;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float stopDistance = 8f; // Distance to stop and attack
    [SerializeField] private Transform crossHair;
    [SerializeField] private float crossHairDistance;

    public NetworkVariable<Vector3> crossHairLocation = new NetworkVariable<Vector3>();

    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField][Range(0f, 1f)] private float shootVolume = 1f;

    private AudioSource _audioSource;


    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.spatialBlend = 0f;
        _audioSource.playOnAwake = false;
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

            dirVector = dirVector.normalized * crossHairDistance;

            crossHairLocation.Value = transform.position + dirVector;

        }
    }

    private void RenderCrosshair()
    {
        crossHair.transform.position = crossHairLocation.Value;
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
        if (shootSound != null)
            _audioSource.PlayOneShot(shootSound, shootVolume);

        SpawnerUtil.Instance.NetworkSpawnGameObject(bulletPrefabName, crossHair.transform.position, 0, GetComponent<NetworkObject>().NetworkObjectId);
    }
}