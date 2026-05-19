using Unity.Netcode;
using UnityEngine;

public class SpawnEnemiesMP : NetworkBehaviour
{
    public float timerMax = 5.0f;

    public Vector2 spawnPos;

    public Vector2 spawnPosRange;

    public string spawnName;

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

            Vector2 randSpawnPos = new Vector2(spawnPos.x + Random.Range(-spawnPosRange.x / 2, spawnPosRange.x / 2), Random.Range(-spawnPosRange.x / 2, spawnPosRange.y / 2));

            SpawnerUtil.Instance.NetworkSpawnGameObject(spawnName, randSpawnPos);
        }
    }
}
