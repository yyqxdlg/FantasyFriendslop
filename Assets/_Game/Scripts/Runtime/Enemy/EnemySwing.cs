using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class EnemySwing : SwordSwing
{
    // List of players hit by swing, to prevent hitting the same player multiple times in one swing
    private List<ulong> playersHit = new List<ulong>();

    // Unity calls OnTriggerEnter2D on all scripts in the hierarchy, so this runs alongside
    // BulletMoveMP's version. BulletMoveMP looks for EnemyBasic (finds nothing), we look for
    // CharacterBasic (finds the player). No conflict.
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        if (collision.gameObject == creator || collision.isTrigger) return;

        CharacterBasic player = collision.gameObject.GetComponent<CharacterBasic>();
        if (player == null) return;

        NetworkObject netObj = collision.gameObject.GetComponent<NetworkObject>();
        if (netObj == null) return;

        // Don't hit the same player twice in one swing
        if (playersHit.Contains(netObj.NetworkObjectId)) return;
        playersHit.Add(netObj.NetworkObjectId);

        player.TakeDamage(damage);
    }
}
