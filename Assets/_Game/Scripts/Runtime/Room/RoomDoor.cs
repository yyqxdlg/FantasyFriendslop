// RoomDoor.cs — with proximity check + player teleport

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections;

public class RoomDoor : NetworkBehaviour
{
    // ── Inspector fields ───────────────────────────────────────────
    [Header("Door Visual")]
    [SerializeField] private GameObject doorVisual;

    [Header("Enemy Spawning")]
    [SerializeField] private string[] enemySpawnableNames;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float proximityRadius = 8f;
    // How far away teammates can be from the triggering player (adjust in Inspector)

    [Header("Player Entry Points (max 4)")]
    [SerializeField] private Transform[] playerEntryPoints;
    // Drag in EntryPoint_1..4 — players teleport here when door opens
    
    [Header("Hint Text")]
    [SerializeField] private TMP_Text hintText;
    // Drag in HintText 

    // ── Network state ──────────────────────────────────────────────
    private NetworkVariable<bool> hasTriggered =
        new NetworkVariable<bool>(false);

    // ── Lifecycle ──────────────────────────────────────────────────
    public override void OnNetworkSpawn()
    {
        hasTriggered.OnValueChanged += (_, newVal) =>
        {
            if (newVal) HideDoor();
        };
        if (hasTriggered.Value) HideDoor();
    }

    // ── One player enters the trigger zone ────────────────────────
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (hasTriggered.Value) return;
        // if (!col.CompareTag(playerTag)) return;

        CheckAndTryOpen(col.transform.position);
    }

    // ── All the checks ────────────────────────────────────────────
    private void CheckAndTryOpen(Vector3 triggerPos)
    {
        int totalConnected = NetworkManager.Singleton.ConnectedClientsIds.Count;

        // Find all spawned characters in the scene
        CharacterBasic[] allPlayers = FindObjectsOfType<CharacterBasic>();

        // Check 1: everyone selected a character?
        if (allPlayers.Length < totalConnected)
        {
            ShowHint("Someone hasn't picked a class yet!");
            return;
        }

        // Check 2: are all teammates close enough to the triggering player?
        foreach (var player in allPlayers)
        {
            float dist = Vector2.Distance(player.transform.position, triggerPos);

            // Skip the triggering player themselves (distance = 0)
            if (dist < 0.1f) continue;

            if (dist > proximityRadius)
            {
                ShowHint(
                    $"Gather your team first!"
                );
                return;
            }
        }

        // All checks passed!
        ShowHint("");
        TriggerRoomServerRpc();
    }

    // ── Server: spawn enemies then teleport all players ───────────
    [Rpc(SendTo.Server)]
    private void TriggerRoomServerRpc()
    {
        if (hasTriggered.Value) return;
        hasTriggered.Value = true;

        // Spawn enemies
        for (int i = 0; i < enemySpawnableNames.Length; i++)
        {
            Vector3 pos = (i < spawnPoints.Length && spawnPoints[i] != null)
                ? spawnPoints[i].position
                : transform.position;

            SpawnerUtil.Instance.NetworkSpawnGameObject(
                enemySpawnableNames[i], pos, 0, ulong.MaxValue
            );
        }

        // Teleport each player to their entry point.
        // We collect all CharacterBasic objects and assign them positions by index.
        CharacterBasic[] allPlayers = FindObjectsOfType<CharacterBasic>();

        for (int i = 0; i < allPlayers.Length; i++)
        {
            if (i >= playerEntryPoints.Length) break;
            if (playerEntryPoints[i] == null) continue;

            Vector3 dest = playerEntryPoints[i].position;

            // Send a ClientRpc to the specific client that owns this character,
            // telling it to move to the assigned entry point.
            ulong ownerClientId = allPlayers[i].OwnerClientId;
            TeleportPlayerClientRpc(dest,
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { ownerClientId }
                    }
                });
        }
    }

    // ── Teleport one player on their own client ────────────────────
    // This runs on the specific client that owns the character being moved.
    [ClientRpc]
    private void TeleportPlayerClientRpc(Vector3 destination, ClientRpcParams rpcParams = default)
    {
        // Find this client's own character (IsOwner = belongs to me)
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

    // ── Hide door (runs everywhere via NetworkVariable) ───────────
    private void HideDoor()
    {
        if (doorVisual != null) doorVisual.SetActive(false);
        ShowHint("");
    }

    // show hint
    private Coroutine hintCoroutine;

    private void ShowHint(string msg)
    {
        if (hintText == null) return;
        
        // if exist one, cancel
        if (hintCoroutine != null) StopCoroutine(hintCoroutine);
        
        hintText.text = msg;
        hintText.gameObject.SetActive(true);
        
        // 3s hide
        hintCoroutine = StartCoroutine(HideHintAfterDelay(3f));
    }

    private IEnumerator HideHintAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        hintText.gameObject.SetActive(false);
    }
}