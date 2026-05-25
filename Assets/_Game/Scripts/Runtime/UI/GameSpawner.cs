using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameSpawner : MonoBehaviour
{
    // ── 改动：把原来的 Transform[] 换成 SpawnPointSet ────────────────────
    // 原来：[SerializeField] private Transform[] spawnPoints;
    // 现在：用 GameplayManager 里的 levelSpawnPoints[0]，不重复拖槽
    // ──────────────────────────────────────────────────────────────────────
    [SerializeField] private SelectPlateController selectPlateController;

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

        if (GameplayManager.Instance == null)
        {
            Debug.LogError("GameplayManager missing!");
            return;
        }

        // ── 改动：每个玩家用不同出生点 ───────────────────────────────────
        // 原来：所有人都用 spawnPoints[spawnIndex].position
        // 现在：玩家i 用 GameplayManager 的 levelSpawnPoints[0] 第i个点
        for (int i = 0; i < LobbyNetworkState.Instance.Players.Count; i++)
        {
            PlayerLobbyData data = LobbyNetworkState.Instance.Players[i];

            SpawnPointSet set = GameplayManager.Instance.levelSpawnPoints != null
                && GameplayManager.Instance.levelSpawnPoints.Length > 0
                ? GameplayManager.Instance.levelSpawnPoints[0]
                : null;

            Vector3 spawnPos = set != null ? set.GetPoint(i) : Vector3.zero;

            LobbyNetworkState.Instance.SpawnHeroForPlayer(data.ClientId, data.HeroId, spawnPos);
        }
        // ──────────────────────────────────────────────────────────────────
    }
}
