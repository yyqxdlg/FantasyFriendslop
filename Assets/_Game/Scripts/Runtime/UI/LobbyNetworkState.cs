using System;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct PlayerLobbyData : INetworkSerializable, IEquatable<PlayerLobbyData>
{
    public ulong ClientId;
    public FixedString64Bytes PlayerName;
    public int HeroId;
    public ulong CharacterObjectId;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref HeroId);
        serializer.SerializeValue(ref CharacterObjectId);
    }

    public bool Equals(PlayerLobbyData other)
    {
        return ClientId == other.ClientId
            && PlayerName.Equals(other.PlayerName)
            && HeroId == other.HeroId
            && CharacterObjectId == other.CharacterObjectId;
    }
}

public class LobbyNetworkState : NetworkBehaviour
{
    public static LobbyNetworkState Instance { get; private set; }

    [Header("Hero spawn names must match SpawnerUtil.spawnablesNames")]
    [SerializeField] private string[] heroSpawnableNames =
    {
        "CharacterPriest",  // Yellow
        "CharacterArcher",  // Green
        "CharacterMage",    // Blue
        "CharacterWarrior"  // Red
    };

    [Header("Start game")]
    [SerializeField] private string gameSceneName = "Demo Map";
    [SerializeField] private Transform fallbackSpawnPoint;

    public NetworkVariable<bool> allowSameHero = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkList<PlayerLobbyData> Players { get; private set; }

    public event Action OnPlayersReady;
    public event Action<bool, string> OnSelectionResponse;
    public event Action<string> OnLobbyMessage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Players = new NetworkList<PlayerLobbyData>();
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            allowSameHero.Value = LocalGameSettings.AllowSameHero;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        if (IsClient)
        {
            RegisterLocalPlayerName();
        }

        OnPlayersReady?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

        base.OnNetworkDespawn();
    }

    private void OnDestroy()
    {
        if (Players != null)
            Players.Dispose();
    }

    public void RegisterLocalPlayerName()
    {
        if (!IsClient) return;
        RegisterPlayerServerRpc(LocalGameSettings.PlayerName);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RegisterPlayerServerRpc(string playerName, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        Debug.Log("Registering player: " + playerName + " clientId: " + clientId);
        RegisterOrUpdatePlayer(clientId, playerName);
    }

    private void RegisterOrUpdatePlayer(ulong clientId, string playerName)
    {
        int index = FindPlayerIndex(clientId);
        FixedString64Bytes safeName = new FixedString64Bytes(
            string.IsNullOrWhiteSpace(playerName) ? "Player" + clientId : playerName.Trim()
        );

        if (index >= 0)
        {
            PlayerLobbyData data = Players[index];
            data.PlayerName = safeName;
            Players[index] = data;
            return;
        }

        Players.Add(new PlayerLobbyData
        {
            ClientId = clientId,
            PlayerName = safeName,
            HeroId = -1,
            CharacterObjectId = ulong.MaxValue
        });
    }

    private void OnClientDisconnected(ulong clientId)
    {
        int index = FindPlayerIndex(clientId);
        if (index >= 0)
            Players.RemoveAt(index);
    }

    public void RequestSelectHero(int heroId)
    {
        if (!IsClient) return;
        RequestSelectHeroServerRpc(heroId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSelectHeroServerRpc(int heroId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (heroId < 0 || heroId >= heroSpawnableNames.Length)
        {
            SendSelectionResult(clientId, false, "Invalid hero.");
            return;
        }

        int playerIndex = FindPlayerIndex(clientId);
        if (playerIndex < 0)
        {
            RegisterOrUpdatePlayer(clientId, "Player" + clientId);
            playerIndex = FindPlayerIndex(clientId);
        }

        if (!allowSameHero.Value && IsHeroTakenByOtherClient(heroId, clientId))
        {
            SendSelectionResult(clientId, false, "This hero is already selected.");
            return;
        }

        PlayerLobbyData data = Players[playerIndex];
        data.HeroId = heroId;
        data.CharacterObjectId = ulong.MaxValue;
        Players[playerIndex] = data;

        SendSelectionResult(clientId, true, "Hero selected.");
    }

    public void RequestStartGame()
    {
        if (!IsClient) return;
        RequestStartGameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestStartGameServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (senderClientId != NetworkManager.ServerClientId)
        {
            SendLobbyMessage(senderClientId, "Only host can start the game.");
            return;
        }

        if (!AllConnectedPlayersSelectedOnServer())
        {
            SendLobbyMessage(senderClientId, "Every player must choose a hero first.");
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    public bool IsHeroTakenByOtherClient(int heroId, ulong localClientId)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId != localClientId && Players[i].HeroId == heroId)
                return true;
        }
        return false;
    }

    public bool AllRegisteredPlayersSelectedLocal()
    {
        if (Players.Count <= 0) return false;
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].HeroId < 0) return false;
        }
        return true;
    }

    public string GetHeroTakenText(int heroId)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < Players.Count; i++)
        {
            PlayerLobbyData data = Players[i];
            if (data.HeroId != heroId) continue;
            if (builder.Length > 0) builder.Append(", ");
            builder.Append(data.PlayerName.ToString());
        }
        return builder.ToString();
    }

    public int GetLocalPlayerHeroId()
    {
        if (NetworkManager.Singleton == null) return -1;
        ulong localId = NetworkManager.Singleton.LocalClientId;
        int index = FindPlayerIndex(localId);
        return index >= 0 ? Players[index].HeroId : -1;
    }

    public string BuildPlayerListText()
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < Players.Count; i++)
        {
            PlayerLobbyData data = Players[i];
            builder.Append(data.PlayerName.ToString());
            builder.Append(" - ");
            builder.Append(GetHeroDisplayName(data.HeroId));
            builder.AppendLine();
        }
        return builder.ToString();
    }

    public string GetHeroDisplayName(int heroId)
    {
        switch (heroId)
        {
            case 0: return "Yellow / Priest";
            case 1: return "Green / Archer";
            case 2: return "Blue / Mage";
            case 3: return "Red / Warrior";
            default: return "Not selected";
        }
    }

    private bool AllConnectedPlayersSelectedOnServer()
    {
        if (!IsServer) return false;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            int index = FindPlayerIndex(clientId);
            if (index < 0) return false;
            if (Players[index].HeroId < 0) return false;
        }
        return true;
    }

    private int FindPlayerIndex(ulong clientId)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == clientId)
                return i;
        }
        return -1;
    }

    private NetworkObject GetCurrentControlledObject(ulong clientId, ulong currentObjectId)
    {
        if (currentObjectId != ulong.MaxValue &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(currentObjectId, out NetworkObject objectFromData))
            return objectFromData;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
            return client.PlayerObject;

        return null;
    }

    private void DespawnCurrentControlledObject(ulong clientId, ulong currentObjectId)
    {
        NetworkObject oldObject = GetCurrentControlledObject(clientId, currentObjectId);
        if (oldObject == null || !oldObject.IsSpawned) return;
        oldObject.Despawn(true);
    }

    public NetworkObject SpawnHeroForClient(string spawnableName, Vector3 spawnPosition, ulong ownerClientId)
    {
        if (SpawnerUtil.Instance == null)
        {
            Debug.LogError("SpawnerUtil.Instance is missing.");
            return null;
        }

        Transform prefab = SpawnerUtil.Instance.GetGobByName(spawnableName);
        Transform spawned = Instantiate(prefab, spawnPosition, Quaternion.identity);

        Spawnable spawnable = spawned.GetComponent<Spawnable>();
        if (spawnable != null) spawnable.SetCreator(ulong.MaxValue);

        NetworkObject netObj = spawned.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError(spawnableName + " has no NetworkObject.");
            Destroy(spawned.gameObject);
            return null;
        }

        netObj.SpawnWithOwnership(ownerClientId);
        return netObj;
    }

    public void SpawnHeroForPlayer(ulong clientId, int heroId, Vector3 spawnPos)
    {
        if (heroId < 0 || heroId >= heroSpawnableNames.Length) return;

        NetworkObject character = SpawnHeroForClient(heroSpawnableNames[heroId], spawnPos, clientId);
        if (character == null) return;

        int index = FindPlayerIndex(clientId);
        if (index < 0) return;

        PlayerLobbyData data = Players[index];
        data.CharacterObjectId = character.NetworkObjectId;
        Players[index] = data;
    }

    private void SendSelectionResult(ulong targetClientId, bool success, string message)
    {
        ClientRpcParams target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { targetClientId } }
        };
        SelectionResultClientRpc(success, message, target);
    }

    [ClientRpc]
    private void SelectionResultClientRpc(bool success, string message, ClientRpcParams clientRpcParams = default)
    {
        OnSelectionResponse?.Invoke(success, message);
    }

    private void SendLobbyMessage(ulong targetClientId, string message)
    {
        ClientRpcParams target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { targetClientId } }
        };
        LobbyMessageClientRpc(message, target);
    }

    [ClientRpc]
    private void LobbyMessageClientRpc(string message, ClientRpcParams clientRpcParams = default)
    {
        OnLobbyMessage?.Invoke(message);
    }
}
