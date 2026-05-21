using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assemblies;

public class GameplayManager : MonoBehaviour
{
	List<GhostScript> ghosts = new List<GhostScript>();

	List<CharacterBasic> characters = new List<CharacterBasic>();
	public static GameplayManager Instance { get; private set; }

    public bool GameStarted = false;

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
        AddPlayerCharacterServerRpc(objectNetworkId);
    }

    public void RemovePlayerCharacter(ulong objectNetworkId)
    {
        RemovePlayerCharacterServerRpc(objectNetworkId);
    }

    public void AddGhost(ulong objectNetworkId)
    {
        AddGhostServerRpc(objectNetworkId);
    }

    public void RemoveGhost(ulong objectNetworkId)
    {
        RemoveGhostServerRpc(objectNetworkId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddPlayerCharacterServerRpc(ulong objectNetworkId)
    {
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


    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RemovePlayerCharacterServerRpc(ulong objectNetworkId)
    {
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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddGhostServerRpc(ulong objectNetworkId)
    {
        bool found = NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
            objectNetworkId,
            out NetworkObject netObj
        );

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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RemoveGhostServerRpc(ulong objectNetworkId)
    {
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

    public void GameStateCheck()
    {
        Debug.Log("Living players: " + characters.Count);

        Debug.Log("Ghosts: " + ghosts.Count);
    }


}
