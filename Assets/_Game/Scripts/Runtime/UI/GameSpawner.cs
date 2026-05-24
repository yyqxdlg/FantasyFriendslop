using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class GameSpawner : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private SelectPlateController selectPlateController; // ← 加这行

    private void Start()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        StartCoroutine(SpawnNextFrame());
    }

    private IEnumerator SpawnNextFrame()
    {
        yield return null;
        SpawnAllPlayers();

        if (selectPlateController != null)
            selectPlateController.DisablePlates();
    }

    private void SpawnAllPlayers()
    {
        if (LobbyNetworkState.Instance == null)
        {
            Debug.LogError("LobbyNetworkState missing in Demo Map!");
            return;
        }

        int spawnIndex = 0;

        for (int i = 0; i < LobbyNetworkState.Instance.Players.Count; i++)
        {
            PlayerLobbyData data = LobbyNetworkState.Instance.Players[i];

            Vector3 spawnPos = spawnPoints != null && spawnIndex < spawnPoints.Length
                ? spawnPoints[spawnIndex].position
                : Vector3.zero;

            LobbyNetworkState.Instance.SpawnHeroForPlayer(data.ClientId, data.HeroId, spawnPos);
            spawnIndex++;
        }
    }
}