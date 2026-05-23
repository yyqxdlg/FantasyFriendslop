using UnityEngine;

public class SpawnPointCluster : SpawnPoint
{
    // spawns one enemy of type for each int in this list for which there are an equal or higher number of players
    public int[] playerCounts;

    public Vector2 spawnRange;

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnRange.x, spawnRange.y, 0));
    }

    public override void Spawn()
    {
        int realPlayerCount = GameplayManager.Instance.characters.Count;

        Debug.Log("SPAWNING with player count " + realPlayerCount);

        foreach (int playerCount in playerCounts) {
            if (playerCount <= realPlayerCount)
            {
                Vector3 spawnPos = transform.position;

                spawnPos.x += Random.Range(0, spawnRange.x) - spawnRange.x / 2;

                spawnPos.y += Random.Range(0, spawnRange.y) - spawnRange.y / 2;

                SpawnerUtil.Instance.NetworkSpawnGameObject(spawnableName, spawnPos);
            }
        }
    }
}
