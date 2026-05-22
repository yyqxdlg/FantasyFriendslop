using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ExitZone : NetworkBehaviour
{
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!IsServer) { return; }

        if (!col.isTrigger)
        {
            GameplayManager.Instance.UpdateInterestReached(GetPlayersInExitZone());
        }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (!IsServer) { return; }

        if (!col.isTrigger)
        {
            GameplayManager.Instance.UpdateInterestReached(GetPlayersInExitZone());
        }
    }

    public List<CharacterBasic> GetPlayersInExitZone()
    {
        ObjectListCollider objectList = GetComponent<ObjectListCollider>();

        List<CharacterBasic> characters = new List<CharacterBasic>();

        for (int i = 0; i < objectList.GetNumberOfTargets(); i++)
        {
            CharacterBasic curr = objectList.GetTarget(i).GetComponent<CharacterBasic>();

            if (curr != null)
            {
                characters.Add(curr);
            }
        }

        return characters;
    }
}
