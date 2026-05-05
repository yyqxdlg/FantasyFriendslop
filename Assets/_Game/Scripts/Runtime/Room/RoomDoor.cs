// RoomDoor.cs — minimal expandable room system
// Door trigger + proximity check + enemy clear detection + key drop

using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections;

public class RoomDoor : NetworkBehaviour
{
    // ── Inspector fields ───────────────────────────────────────────

    [Header("Room Identity")]
    [SerializeField] private int roomId = 1;

    [Header("Door Visual")]
    [SerializeField] private GameObject doorVisual;
    [Header("Door Blocker")]
    [SerializeField] private Collider2D[] doorBlockers;

    [Header("Enemy Spawning")]
    [SerializeField] private string[] enemySpawnableNames;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Key Drop")]
    [SerializeField] private bool dropKeyWhenCleared = true;
    [SerializeField] private string keySpawnableName = "RoomKey";

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float proximityRadius = 8f;
    // need teleport
    [Header("Teleport")]
    [SerializeField] private bool teleportPlayersOnTrigger = true;

    [Header("Player Entry Points (max 4)")]
    [SerializeField] private Transform[] playerEntryPoints;

    [Header("Hint Text")]
    [SerializeField] private TMP_Text hintText;

    [Header("Music")]
    [SerializeField] private string roomSong;

    // ── Network state ──────────────────────────────────────────────

    private NetworkVariable<bool> hasTriggered =
        new NetworkVariable<bool>(false);

    private NetworkVariable<bool> roomCleared =
        new NetworkVariable<bool>(false);

    private NetworkVariable<bool> keyCollected =
        new NetworkVariable<bool>(false);

    // ── Server-only runtime state ──────────────────────────────────

    private int aliveEnemyCount = 0;
    private bool keyHasSpawned = false;

    // ── Lifecycle ──────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        hasTriggered.OnValueChanged += OnHasTriggeredChanged;
        roomCleared.OnValueChanged += OnRoomClearedChanged;
        keyCollected.OnValueChanged += OnKeyCollectedChanged;

        EnemyBasic.OnEnemyDiedInRoom += Server_OnEnemyDiedInRoom;
        RoomKey.OnKeyPickedUpInRoom += Server_OnKeyPickedUpInRoom;

        if (hasTriggered.Value)
        {
            HideDoor();
        }
    }

    public override void OnNetworkDespawn()
    {
        hasTriggered.OnValueChanged -= OnHasTriggeredChanged;
        roomCleared.OnValueChanged -= OnRoomClearedChanged;
        keyCollected.OnValueChanged -= OnKeyCollectedChanged;

        EnemyBasic.OnEnemyDiedInRoom -= Server_OnEnemyDiedInRoom;
        RoomKey.OnKeyPickedUpInRoom -= Server_OnKeyPickedUpInRoom;
    }

    private void OnHasTriggeredChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            HideDoor();
        }
    }

    private void OnRoomClearedChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            ShowHint("Room cleared! A key dropped.");
        }
    }

    private void OnKeyCollectedChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            ShowHint("Key collected!");
        }
    }

    // ── Player enters door trigger ─────────────────────────────────

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (hasTriggered.Value) return;

        CharacterBasic player = GetPlayerFromCollider(col);
        if (player == null) return;

        CheckAndTryOpen(player.transform.position);
    }

    private CharacterBasic GetPlayerFromCollider(Collider2D col)
    {
        CharacterBasic player = col.GetComponent<CharacterBasic>();

        if (player == null)
        {
            player = col.GetComponentInParent<CharacterBasic>();
        }

        if (player == null) return null;

        if (!player.CompareTag(playerTag) && !col.CompareTag(playerTag))
        {
            return null;
        }

        return player;
    }

    // ── Checks before opening room ─────────────────────────────────

    private void CheckAndTryOpen(Vector3 triggerPos)
    {
        int totalConnected = NetworkManager.Singleton.ConnectedClientsIds.Count;

        CharacterBasic[] allPlayers = FindObjectsOfType<CharacterBasic>();

        // Check 1: everyone selected a character?
        if (allPlayers.Length < totalConnected)
        {
            ShowHint("Someone hasn't picked a class yet!");
            return;
        }

        // Check 2: are all teammates close enough?
        foreach (var player in allPlayers)
        {
            if (player == null) continue;

            float dist = Vector2.Distance(player.transform.position, triggerPos);

            if (dist < 0.1f) continue;

            if (dist > proximityRadius)
            {
                ShowHint("Gather your team first!");
                return;
            }
        }

        ShowHint("");
        TriggerRoomServerRpc();
    }

    // ── Server: activate this room ─────────────────────────────────

    [Rpc(SendTo.Server)]
    private void TriggerRoomServerRpc()
    {
        if (hasTriggered.Value) return;

        AudioManager.Instance.PlayBackgroundSong(roomSong, 1);

        hasTriggered.Value = true;
        roomCleared.Value = false;
        keyCollected.Value = false;

        aliveEnemyCount = 0;
        keyHasSpawned = false;

        SpawnEnemiesForThisRoom();
        if (teleportPlayersOnTrigger)
        {
            TeleportAllPlayersToEntryPoints();
        }

        // If this room has no enemies, immediately clear it.
        if (aliveEnemyCount <= 0)
        {
            Server_ClearRoom(transform.position);
        }
    }

    private void SpawnEnemiesForThisRoom()
    {
        for (int i = 0; i < enemySpawnableNames.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(enemySpawnableNames[i])) continue;

            Vector3 pos = transform.position;

            if (i < spawnPoints.Length && spawnPoints[i] != null)
            {
                pos = spawnPoints[i].position;
            }

            Server_SpawnEnemyForRoom(enemySpawnableNames[i], pos);
        }
    }

    private void Server_SpawnEnemyForRoom(string spawnableName, Vector3 position)
    {
        if (!IsServer) return;

        Transform prefab = SpawnerUtil.Instance.GetGobByName(spawnableName);

        Transform spawnedTransform = Instantiate(
            prefab,
            position,
            Quaternion.identity
        );

        EnemyBasic enemy = spawnedTransform.GetComponent<EnemyBasic>();

        if (enemy == null)
        {
            Debug.LogError($"{spawnableName} does not have EnemyBasic.");
            Destroy(spawnedTransform.gameObject);
            return;
        }

        enemy.SetRoomId(roomId);

        Spawnable spawnable = spawnedTransform.GetComponent<Spawnable>();

        if (spawnable != null)
        {
            spawnable.SetCreator(ulong.MaxValue);
        }

        NetworkObject netObj = spawnedTransform.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError($"{spawnableName} does not have NetworkObject.");
            Destroy(spawnedTransform.gameObject);
            return;
        }

        netObj.SpawnWithOwnership(NetworkManager.ServerClientId);

        aliveEnemyCount++;
    }

    // ── Enemy death tracking ───────────────────────────────────────

    private void Server_OnEnemyDiedInRoom(int deadEnemyRoomId, Vector3 deathPosition)
    {
        if (!IsServer) return;
        if (!hasTriggered.Value) return;
        if (roomCleared.Value) return;

        // Important:
        // Every RoomDoor hears every enemy death.
        // So each room must ignore enemies from other rooms.
        if (deadEnemyRoomId != roomId) return;

        aliveEnemyCount = Mathf.Max(0, aliveEnemyCount - 1);

        Debug.Log($"Room {roomId}: enemy died. Remaining = {aliveEnemyCount}");

        if (aliveEnemyCount <= 0)
        {
            Server_ClearRoom(deathPosition);
        }
    }

    private void Server_ClearRoom(Vector3 keyDropPosition)
    {
        if (!IsServer) return;
        if (roomCleared.Value) return;

        roomCleared.Value = true;

        if (dropKeyWhenCleared)
        {
            Server_SpawnKey(keyDropPosition);
        }
    }

    // ── Key spawning and pickup ────────────────────────────────────

    private void Server_SpawnKey(Vector3 position)
    {
        if (!IsServer) return;
        if (keyHasSpawned) return;

        if (string.IsNullOrWhiteSpace(keySpawnableName))
        {
            Debug.LogWarning($"Room {roomId}: keySpawnableName is empty.");
            return;
        }

        Transform prefab = SpawnerUtil.Instance.GetGobByName(keySpawnableName);

        Transform spawnedTransform = Instantiate(
            prefab,
            position,
            Quaternion.identity
        );

        RoomKey key = spawnedTransform.GetComponent<RoomKey>();

        if (key == null)
        {
            Debug.LogError($"{keySpawnableName} does not have RoomKey.");
            Destroy(spawnedTransform.gameObject);
            return;
        }

        key.SetRoomId(roomId);

        Spawnable spawnable = spawnedTransform.GetComponent<Spawnable>();

        if (spawnable != null)
        {
            spawnable.SetCreator(ulong.MaxValue);
        }

        NetworkObject netObj = spawnedTransform.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError($"{keySpawnableName} does not have NetworkObject.");
            Destroy(spawnedTransform.gameObject);
            return;
        }

        netObj.SpawnWithOwnership(NetworkManager.ServerClientId);

        keyHasSpawned = true;
    }

    private void Server_OnKeyPickedUpInRoom(int pickedKeyRoomId, ulong pickerClientId)
    {
        if (!IsServer) return;

        if (pickedKeyRoomId != roomId) return;
        if (keyCollected.Value) return;

        keyCollected.Value = true;

        Debug.Log($"Room {roomId}: key picked up by client {pickerClientId}.");

        // 后续扩展入口：
        // 1. 解锁下一扇门
        // 2. 给团队 inventory 加 key
        // 3. 打开商店
        // 4. 触发 boss room
        // 5. 开启下一段地图
    }

    // ── Teleport players ───────────────────────────────────────────

    private void TeleportAllPlayersToEntryPoints()
    {
        CharacterBasic[] allPlayers = FindObjectsOfType<CharacterBasic>();

        for (int i = 0; i < allPlayers.Length; i++)
        {
            if (i >= playerEntryPoints.Length) break;
            if (playerEntryPoints[i] == null) continue;

            Vector3 dest = playerEntryPoints[i].position;

            ulong ownerClientId = allPlayers[i].OwnerClientId;

            TeleportPlayerClientRpc(
                dest,
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { ownerClientId }
                    }
                }
            );
        }
    }

    [ClientRpc]
    private void TeleportPlayerClientRpc(
        Vector3 destination,
        ClientRpcParams rpcParams = default
    )
    {
        CharacterBasic[] allPlayers = FindObjectsOfType<CharacterBasic>();

        foreach (var player in allPlayers)
        {
            if (player.IsOwner)
            {
                player.transform.position = destination;
                break;
            }
        }
    }

    // ── Door visual ────────────────────────────────────────────────

    private void HideDoor()
    {
        if (doorVisual != null)
        {
            doorVisual.SetActive(false);
        }

        if (doorBlockers != null)
        {
            foreach (Collider2D col in doorBlockers)
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }
        }

        ShowHint("Room Strated");
    }

    // ── Hint UI ────────────────────────────────────────────────────

    private Coroutine hintCoroutine;

    private void ShowHint(string msg)
    {
        if (hintText == null) return;

        if (hintCoroutine != null)
        {
            StopCoroutine(hintCoroutine);
        }

        hintText.text = msg;

        bool shouldShow = !string.IsNullOrWhiteSpace(msg);
        hintText.gameObject.SetActive(shouldShow);

        if (shouldShow)
        {
            hintCoroutine = StartCoroutine(HideHintAfterDelay(3f));
        }
    }

    private IEnumerator HideHintAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (hintText != null)
        {
            hintText.gameObject.SetActive(false);
        }
    }
}