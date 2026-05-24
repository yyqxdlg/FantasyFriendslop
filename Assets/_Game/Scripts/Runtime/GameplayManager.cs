using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assemblies;

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

	public int level;

	public int[] levelMinInterest;

	public Transform[] levelSpawnPoints;

	public NetworkVariable<bool> minInterestReached = new NetworkVariable<bool>(
		false,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner
	);

    public NetworkVariable<int> partyFund = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

	public SpawnPointController[] spawnControllers;

	public Wiper wiper;

    void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
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
		if (!IsServer) { throw new System.Exception("SHOULD BE CALLED FROM SERVER ONLY"); }

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
        if (!IsServer) { throw new System.Exception("SHOULD BE CALLED FROM SERVER ONLY"); }

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
        if (!IsServer) { throw new System.Exception("SHOULD BE CALLED FROM SERVER ONLY"); }

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
		Debug.Log("Living players: " + characters.Count);

		Debug.Log("Ghosts: " + ghosts.Count);
	}

	public void GameOver()
	{
		GameOverServerRpc();
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	public void GameOverServerRpc()
	{
		GuildSmite();
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
			spawnControllers[level].SpawnAll();
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


		minInterestReached.Value = goldSum >= levelMinInterest[level];
	}

	public int GetCurrentMinInterest()
	{
		return levelMinInterest[level];
	}

	public void NextLevel()
	{
		if (!IsServer) { return; }

		levelStarted.Value = false;

		level += 1;

		TeleportPlayersToLevel();

		EnemyWipe();

		FullWipe();
		DespawnAllGhosts(); //clear all ghosts
        RespawnAndRestorePlayers();
    }

	public void TeleportPlayersToLevel()
	{
		for (int i = 0; i < characters.Count; i++)
		{
			characters[i].Teleport(levelSpawnPoints[level].position);
		}

        for (int i = 0; i < ghosts.Count; i++)
        {
            ghosts[i].Teleport(levelSpawnPoints[level].position);
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
		wiper.Wipe(level-1);
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
                levelSpawnPoints[level].position
            );
        }
    }
}
}
