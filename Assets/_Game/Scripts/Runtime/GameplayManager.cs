using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assemblies;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using Unity.VisualScripting;

public class GameplayManager : NetworkBehaviour
{
	public List<GhostScript> ghosts = new List<GhostScript>();

	public List<CharacterBasic> characters = new List<CharacterBasic>();


	public List<EnemyBasic> enemies = new List<EnemyBasic>();

	public static GameplayManager Instance { get; private set; }

	public NetworkVariable<bool> levelStarted = new NetworkVariable<bool>(
		false,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner
	);

	public NetworkVariable<int> level = new NetworkVariable<int>(
		0,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner
	);

	[SerializeField] private int[] levelMinInterestBase;

	// 改：Transform[] → SpawnPointSet[]，每个关卡有多个出生点
	public SpawnPointSet[] levelSpawnPoints;

	public SpawnPointController[] spawnControllers;

	public NetworkVariable<bool> gameOver = new NetworkVariable<bool>(
		false,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner
	);

	public NetworkVariable<bool> allLivingSafe = new NetworkVariable<bool>(
		true,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner
	);

    public NetworkVariable<int> currentMinInterest = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> partyGoldSafe = new NetworkVariable<int>(
		0,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner
	);

	public NetworkVariable<int> exitZoneGold = new NetworkVariable<int>(
		0,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner
	);

	public Wiper wiper;
		[SerializeField] private string gameSceneName = "Demo Map";
		private bool isRestarting = false;

	[Header("Music")]
	public string[] levelMusicNames;

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		PlayLevelMusicDelayed();

		level.OnValueChanged += UpdateMinInterest;

		Invoke("DelayedUpdateInterest", 0.1f);
	}

	private void DelayedUpdateInterest()
	{
        UpdateMinInterest(0, level.Value);
    }

	private void UpdateMinInterest(int prev, int next)
	{
        int numberOfPlayers = ghosts.Count + characters.Count;

        int minInterest = levelMinInterestBase[next];

        minInterest += levelMinInterestBase[next] * (numberOfPlayers - 1) / 2;

		Debug.Log("WHAT?");

        Debug.Log(numberOfPlayers);

        Debug.Log(minInterest);
				if (!IsServer) return;
				currentMinInterest.Value = minInterest;
    }

	// 新增辅助方法：根据关卡和玩家索引取出生点
	private Vector3 GetSpawnPoint(int levelIndex, int playerIndex)
	{
		if (levelSpawnPoints == null || levelIndex >= levelSpawnPoints.Length) return Vector3.zero;
		SpawnPointSet set = levelSpawnPoints[levelIndex];
		if (set == null) return Vector3.zero;
		return set.GetPoint(playerIndex);
	}

	public void AddPlayerCharacter(ulong objectNetworkId)
	{
		if (!IsServer) {
			if (IsSpawned)
			{
				throw new System.Exception("SHOULD BE CALLED FROM SERVER ONLY");
			} else
			{
				throw new System.Exception("ADDING CHARACTER BEFORE GAMEPLAYMANAGER HAS NETWORK OBJECT");
			}
		}

		bool found = NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
			objectNetworkId,
			out NetworkObject netObj
		);

		if (found)
		{
			CharacterBasic character = netObj.gameObject.GetComponent<CharacterBasic>();

			if (character != null)
			{
				characters.Add(character);

				GameStateCheck();
			}
			else
			{
				throw new System.Exception("Object is not a player character");
			}

		}
		else
		{
			throw new System.Exception("Network object does not exist");
		}
	}


	public void RemovePlayerCharacter(ulong objectNetworkId)
	{
		if (!IsServer)
		{
			if (IsSpawned)
			{
				throw new System.Exception("SHOULD BE CALLED FROM SERVER ONLY");
			}
			else
			{
				throw new System.Exception("REMOVING CHARACTER BEFORE GAMEPLAYMANAGER HAS NETWORK OBJECT");
			}
		}

		for (int i = 0; i < characters.Count; i++)
		{
			CharacterBasic curr = characters[i];
			if (curr.gameObject.GetComponent<NetworkObject>().NetworkObjectId == objectNetworkId)
			{
				characters.RemoveAt(i);

				GameStateCheck();

				return;
			}
		}

		throw new System.Exception("No such player character found");
	}

	public void AddGhost(ulong objectNetworkId)
	{
		if (!IsServer)
		{
			if (IsSpawned)
			{
				throw new System.Exception("SHOULD BE CALLED FROM SERVER ONLY");
			}
			else
			{
				throw new System.Exception("ADDING GHOST BEFORE GAMEPLAYMANAGER HAS NETWORK OBJECT");
			}
		}

		bool found = NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
			objectNetworkId,
			out NetworkObject netObj
		);

		Debug.Log("TRYING TO ADD GHOST");

		if (found)
		{
			GhostScript ghost = netObj.gameObject.GetComponent<GhostScript>();

			if (ghost != null)
			{
				ghosts.Add(ghost);

				GameStateCheck();
			}
			else
			{
				throw new System.Exception("Object is not a player character");
			}

		}
		else
		{
			throw new System.Exception("Network object does not exist");
		}
	}


	public void RemoveGhost(ulong objectNetworkId)
	{
		if (!IsServer)
		{
			if (IsSpawned)
			{
				throw new System.Exception("SHOULD BE CALLED FROM SERVER ONLY");
			}
			else
			{
				throw new System.Exception("REMOVING GHOST BEFORE GAMEPLAYMANAGER HAS NETWORK OBJECT");
			}
		}

		for (int i = 0; i < ghosts.Count; i++)
		{
			GhostScript curr = ghosts[i];
			if (curr.gameObject.GetComponent<NetworkObject>().NetworkObjectId == objectNetworkId)
			{
				ghosts.RemoveAt(i);

				GameStateCheck();

				return;
			}
		}

		throw new System.Exception("No such player character found");
	}

	public void AddEnemy(ulong objectNetworkId)
	{
		if (!IsServer)
		{
			if (IsSpawned)
			{
				throw new System.Exception("SHOULD BE CALLED FROM SERVER ONLY");
			}
			else
			{
				throw new System.Exception("ADDING ENEMY BEFORE GAMEPLAYMANAGER HAS NETWORK OBJECT");
			}
		}

		bool found = NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
			objectNetworkId,
			out NetworkObject netObj
		);

		if (found)
		{
			EnemyBasic enemy = netObj.gameObject.GetComponent<EnemyBasic>();

			if (enemy != null)
			{
				enemies.Add(enemy);
			}
			else
			{
				Debug.Log("Object is not an enemy");
			}

		}
		else
		{
			throw new System.Exception("Network object does not exist");
		}
	}


	public void RemoveEnemy(ulong objectNetworkId)
	{
		if (!IsServer)
		{
			if (IsSpawned)
			{
				throw new System.Exception("SHOULD BE CALLED FROM SERVER ONLY");
			}
			else
			{
				throw new System.Exception("REMOVING ENEMY BEFORE GAMEPLAYMANAGER HAS NETWORK OBJECT");
			}
		}

		for (int i = 0; i < enemies.Count; i++)
		{
			EnemyBasic enemy = enemies[i];
			if (enemy.gameObject.GetComponent<NetworkObject>().NetworkObjectId == objectNetworkId)
			{
				enemies.RemoveAt(i);

				return;
			}
		}

		throw new System.Exception("No such enemy found");
	}

	public void GameStateCheck()
	{
		if (isRestarting) return;

		Debug.Log("Living players: " + characters.Count);

		Debug.Log("Ghosts: " + ghosts.Count);

		if (characters.Count == 0 && ghosts.Count != 0)
		{
			GameOver();
		} else
		{
			if (gameOver.Value)
			{
				gameOver.Value = false;
			}
		}
	}

	public void GameOver()
	{
		gameOver.Value = true;
	}

	public void Restart()
	{
		RestartServerRpc();
	}


	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	public void RestartServerRpc()
	{
		if (!IsServer) return;

		isRestarting = true;

		gameOver.Value = false;
		levelStarted.Value = false;
		level.Value = 0;
		partyGoldSafe.Value = 0;

		for (int i = characters.Count - 1; i >= 0; i--)
		{
			if (characters[i] == null) continue;
			NetworkObject netObj = characters[i].GetComponent<NetworkObject>();
			if (netObj != null && netObj.IsSpawned)
				netObj.Despawn(true);
		}
		characters.Clear();

		for (int i = ghosts.Count - 1; i >= 0; i--)
		{
			if (ghosts[i] == null) continue;
			NetworkObject netObj = ghosts[i].GetComponent<NetworkObject>();
			if (netObj != null && netObj.IsSpawned)
				netObj.Despawn(true);
		}
		ghosts.Clear();

		for (int i = enemies.Count - 1; i >= 0; i--)
		{
			if (enemies[i] == null) continue;
			NetworkObject netObj = enemies[i].GetComponent<NetworkObject>();
			if (netObj != null && netObj.IsSpawned)
				netObj.Despawn(true);
		}
		enemies.Clear();

		PlayLevelMusicDelayed();

		StartCoroutine(WipeAllThenSpawn());
	}

private IEnumerator WipeAllThenSpawn()
{
	if (wiper != null)
		wiper.transform.position = new Vector3(-112.4f, 32.9f, 0f);

	yield return new WaitForSeconds(0.1f);

	Spawnable[] allSpawnables = FindObjectsOfType<Spawnable>();
	foreach (Spawnable s in allSpawnables)
	{
		if (s.GetComponent<CharacterBasic>() != null) continue;
		if (s.GetComponent<EnemyBasic>() != null) continue;
		if (s.GetComponent<GhostScript>() != null) continue;
		NetworkObject netObj = s.GetComponent<NetworkObject>();
		if (netObj != null && netObj.IsSpawned)
			netObj.Despawn(true);
	}

	yield return new WaitForSeconds(0.3f);

	// 改：用 GetSpawnPoint 代替 levelSpawnPoints[0].position
	MoveCameraToSpawnClientRpc(GetSpawnPoint(0, 0));
	yield return new WaitForSeconds(0.1f);

	if (LobbyNetworkState.Instance == null)
	{
		Debug.LogError("LobbyNetworkState is NULL!");
		isRestarting = false;
		yield break;
	}

	FogOfWar[] fogs = FindObjectsOfType<FogOfWar>();
	foreach (FogOfWar fog in fogs)
	{
		fog.Reset();
	}

	// 改：每个玩家用不同出生点
	for (int i = 0; i < LobbyNetworkState.Instance.Players.Count; i++)
	{
		PlayerLobbyData data = LobbyNetworkState.Instance.Players[i];
		LobbyNetworkState.Instance.SpawnHeroForPlayer(
			data.ClientId,
			data.HeroId,
			GetSpawnPoint(0, i)
		);
	}

	yield return new WaitForSeconds(0.5f);

	isRestarting = false;

	SelectPlateController plateController = FindObjectOfType<SelectPlateController>();
	if (plateController != null)
		plateController.DisablePlates();
}

[ClientRpc]
private void MoveCameraToSpawnClientRpc(Vector3 position)
{
	if (Camera.main != null)
	{
		Camera.main.transform.position = new Vector3(
			position.x,
			position.y,
			Camera.main.transform.position.z
		);
	}
}

	public void GuildSmite()
	{
		GuildSmiteSelective(characters);
	}

	public void GuildSmiteSelective(List<CharacterBasic> charToSmite)
	{
		Debug.Log("SMITING " + charToSmite.Count + " players");

		for (int i = 0; i < charToSmite.Count; i++)
		{
			Debug.Log("SMITING: " + charToSmite[i].gameObject.name);
			charToSmite[i].TakeDamage(1000);
		}
	}


	public void ChangeLevelStarted(bool newVal)
	{
		ChangeLevelStartedRpc(newVal);
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	public void ChangeLevelStartedRpc(bool newVal)
	{
		levelStarted.Value = newVal;

		if (newVal)
		{
			spawnControllers[level.Value].SpawnAll();
		}
	}

	public void UpdateInterestReached(List<CharacterBasic> exitZoneCharacters)
	{
		if (!IsServer) { return; }

		int goldSum = 0;

		foreach (CharacterBasic character in exitZoneCharacters)
		{
			goldSum += character.coinCount.Value;
		}

		exitZoneGold.Value = goldSum;

		allLivingSafe.Value = characters.Count == exitZoneCharacters.Count;

	}

	public int GetCurrentMinInterest()
	{
		return currentMinInterest.Value;
    }

	public bool MinInterestReached()
	{
		return (partyGoldSafe.Value + exitZoneGold.Value) >= GetCurrentMinInterest();
	}

	public void NextLevel()
	{
		if (!IsServer) { return; }

		if (level.Value == 2)
		{
			WinScreen();
			return;
		}

		levelStarted.Value = false;

		PersistGold();

		level.Value += 1;

		TeleportPlayersToLevel();

		EnemyWipe();

		FullWipe();
		DespawnAllGhosts();
		ClearInventories();

		RespawnAndRestorePlayers();

		PlayLevelMusicDelayed();
	}

	public void WinScreen()
	{
        level.Value += 1;
        TeleportPlayersToLevel();
		PlayLevelMusicDelayed();
	}

    private void PlayLevelMusicDelayed()
    {
		Invoke("PlayLevelMusic", 0.1f);
    }

    private void PlayLevelMusic()
	{
		AudioManager.Instance.PlayBackgroundSong(levelMusicNames[level.Value], 1);
	}

	private void PersistGold()
	{
		partyGoldSafe.Value += exitZoneGold.Value;
		exitZoneGold.Value = 0;

		partyGoldSafe.Value -= GetCurrentMinInterest();

		foreach (CharacterBasic character in characters)
		{
			character.DeleteCoins();
		}
	}

	public void TeleportPlayersToLevel()
	{
			for (int i = 0; i < characters.Count; i++)
			{
					ulong clientId = characters[i].GetComponent<NetworkObject>().OwnerClientId;
					int playerIndex = GetPlayerIndex(clientId);
					characters[i].Teleport(GetSpawnPoint(level.Value, playerIndex));
			}

			for (int i = 0; i < ghosts.Count; i++)
			{
					ulong clientId = ghosts[i].GetComponent<NetworkObject>().OwnerClientId;
					int playerIndex = GetPlayerIndex(clientId);
					ghosts[i].Teleport(GetSpawnPoint(level.Value, playerIndex));
			}
	}

private void DespawnAllGhosts()
{
	for (int i = ghosts.Count - 1; i >= 0; i--)
	{
		GhostScript ghost = ghosts[i];
		if (ghost == null) continue;

		NetworkObject netObj = ghost.GetComponent<NetworkObject>();
		if (netObj != null && netObj.IsSpawned)
			netObj.Despawn(true);
	}
	ghosts.Clear();
}

	public void EnemyWipe()
	{
		foreach(EnemyBasic enemy in enemies)
		{
			enemy.TakeDamage(1000);
		}
	}

	public void FullWipe()
	{
		wiper.Wipe(level.Value-1);
	}

	public void RespawnAndRestorePlayers()
	{
		foreach (CharacterBasic character in characters)
		{
			character.HealAmount(1000);
		}

		if (LobbyNetworkState.Instance == null) return;

		for (int i = 0; i < LobbyNetworkState.Instance.Players.Count; i++)
		{
			PlayerLobbyData data = LobbyNetworkState.Instance.Players[i];

			bool hasAliveCharacter = false;
			foreach (CharacterBasic character in characters)
			{
				NetworkObject netObj = character.GetComponent<NetworkObject>();
				if (netObj != null && netObj.OwnerClientId == data.ClientId)
				{
					hasAliveCharacter = true;
					break;
				}
			}

			// 改：每个玩家用不同出生点
			if (!hasAliveCharacter)
			{
					LobbyNetworkState.Instance.SpawnHeroForPlayer(
							data.ClientId,
							data.HeroId,
							GetSpawnPoint(level.Value, i) // i 就是 LobbyNetworkState 里的玩家索引，不变
					);
			}
		}
	}
	// 新增辅助方法：根据 ClientId 获取固定玩家索引
	private int GetPlayerIndex(ulong clientId)
	{
			if (LobbyNetworkState.Instance == null) return 0;
			for (int i = 0; i < LobbyNetworkState.Instance.Players.Count; i++)
			{
					if (LobbyNetworkState.Instance.Players[i].ClientId == clientId)
							return i;
			}
			return 0;
	}
	public void ClearInventories()
	{
		foreach (CharacterBasic character in characters)
		{
			character.ClearInventory();
		}
	}
}
