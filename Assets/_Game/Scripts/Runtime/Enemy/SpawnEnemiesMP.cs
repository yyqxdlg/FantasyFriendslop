using Unity.Netcode;
using UnityEngine;

public class SpawnEnemiesMP : NetworkBehaviour
{
    public float timerMax = 5.0f;

    public Vector2 spawnPos;

    public Transform enemyPrefab;

    private float timer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timer = timerMax;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) return;

        timer -= Time.deltaTime;

        if(timer <= 0)
        {
            timer = timerMax;

            Transform spawnedObjectTransform = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);
        }
    }
}
