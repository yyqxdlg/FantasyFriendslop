using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assemblies;
using UnityEngine.SceneManagement;
using System.Collections;

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

	public int[] levelMinInterest;

	public Transform[] levelSpawnPoints;

	public NetworkVariable<bool> minInterestReached = new NetworkVariable<bool>(
		false,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner
	);

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

		PlayLevelMusic();
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

		Debug.Log("TRYING TO ADD PLAYER");

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

		//Debug.Log(NetworkManager.Singleton == null);

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

		//Debug.Log(NetworkManager.Singleton == null);

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
		if (isRestarting) return; // 

		Debug.Log("Living players: " + characters.Count);

		Debug.Log("Ghosts: " + ghosts.Count);

		if (characters.Count == 0 && levelStarted.Value)
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

		// 重置所有状态
		gameOver.Value = false;
		levelStarted.Value = false;
		level.Value = 0;
		minInterestReached.Value = false;
		partyGoldSafe.Value = 0;

		// Despawn 所有角色
		for (int i = characters.Count - 1; i >= 0; i--)
		{
			if (characters[i] == null) continue;
			NetworkObject netObj = characters[i].GetComponent<NetworkObject>();
			if (netObj != null && netObj.IsSpawned)
				netObj.Despawn(true);
		}
		characters.Clear();

		// Despawn 所有 Ghost
		for (int i = ghosts.Count - 1; i >= 0; i--)
		{
			if (ghosts[i] == null) continue;
			NetworkObject netObj = ghosts[i].GetComponent<NetworkObject>();
			if (netObj != null && netObj.IsSpawned)
				netObj.Despawn(true);
		}
		ghosts.Clear();

		// 清掉所有敌人
		for (int i = enemies.Count - 1; i >= 0; i--)
		{
			if (enemies[i] == null) continue;
			NetworkObject netObj = enemies[i].GetComponent<NetworkObject>();
			if (netObj != null && netObj.IsSpawned)
				netObj.Despawn(true);
		}
		enemies.Clear();

		PlayLevelMusic();

		// isRestarting = false;
		StartCoroutine(WipeAllThenSpawn());
	}

private IEnumerator WipeAllThenSpawn()
{
		// 把 Wiper 移回初始位置，不盖住关卡1
if (wiper != null)
	wiper.transform.position = new Vector3(-112.4f, 32.9f, 0f);

yield return new WaitForSeconds(0.1f);
	// 清掉落物...
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

	MoveCameraToSpawnClientRpc(levelSpawnPoints[0].position);
	yield return new WaitForSeconds(0.1f);

	if (LobbyNetworkState.Instance == null)
	{
		Debug.LogError("LobbyNetworkState is NULL!");
		isRestarting = false; // ← 失败也要设回
		yield break;
	}
		// 重置所有 FogOfWar，让关卡1地图重新显示
FogOfWar[] fogs = FindObjectsOfType<FogOfWar>();
foreach (FogOfWar fog in fogs)
{
	fog.Reset(); // 重新启用所有 SpriteRenderer
}
	for (int i = 0; i < LobbyNetworkState.Instance.Players.Count; i++)
	{
		PlayerLobbyData data = LobbyNetworkState.Instance.Players[i];
		LobbyNetworkState.Instance.SpawnHeroForPlayer(
			data.ClientId,
			data.HeroId,
			levelSpawnPoints[0].position
		);
	}

	// 等角色生成完成
	yield return new WaitForSeconds(0.5f);

	isRestarting = false; // ← 这里才设回 false！

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

		minInterestReached.Value = exitZoneGold.Value + partyGoldSafe.Value >= levelMinInterest[level.Value];

		allLivingSafe.Value = characters.Count == exitZoneCharacters.Count;

	}

	public int GetCurrentMinInterest()
	{
		return levelMinInterest[level.Value];
	}

	public void NextLevel()
	{
		if (!IsServer) { return; }

		levelStarted.Value = false;

		PersistGold();

		level.Value += 1;

		TeleportPlayersToLevel();

		EnemyWipe();

		FullWipe();
		DespawnAllGhosts(); //clear all ghosts
		ClearInventories();

		RespawnAndRestorePlayers();

		PlayLevelMusic();
	}

	private void PlayLevelMusic()
	{
		AudioManager.Instance.PlayBackgroundSong(levelMusicNames[level.Value], 1);
	}

	private void PersistGold()
	{
		partyGoldSafe.Value += exitZoneGold.Value;
		exitZoneGold.Value = 0;

		partyGoldSafe.Value -= levelMinInterest[level.Value];

		foreach (CharacterBasic character in characters)
		{
			character.DeleteCoins();
		}
	}

	public void TeleportPlayersToLevel()
	{
		for (int i = 0; i < characters.Count; i++)
		{
			characters[i].Teleport(levelSpawnPoints[level.Value].position);
		}

		for (int i = 0; i < ghosts.Count; i++)
		{
			ghosts[i].Teleport(levelSpawnPoints[level.Value].position);
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
	// 治愈现有存活角色
	foreach (CharacterBasic character in characters)
	{
		character.HealAmount(1000);
	}

	// 给死亡玩家（原来的Ghost）重新生成角色
	if (LobbyNetworkState.Instance == null) return;

	for (int i = 0; i < LobbyNetworkState.Instance.Players.Count; i++)
	{
		PlayerLobbyData data = LobbyNetworkState.Instance.Players[i];

		// 检查这个玩家是否已经有存活角色
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

		// 没有存活角色的玩家重新生成
		if (!hasAliveCharacter)
		{
			LobbyNetworkState.Instance.SpawnHeroForPlayer(
				data.ClientId,
				data.HeroId,
				levelSpawnPoints[level.Value].position
			);
		}
	}
}

	public void ClearInventories()
	{
		foreach (CharacterBasic character in characters)
		{
			character.ClearInventory();
		}
	}
}
