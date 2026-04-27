using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class EnemySwing : SwordSwing
{
    private List<ulong> playersHit = new List<ulong>();

    public override void Fire()
    {
        GameObject creator = GetCreator();

        if (creator == null)
        {
            Debug.LogError($"{name}: EnemySwing creator is null.");
            return;
        }

        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError($"{name}: EnemySwing missing Rigidbody2D.");
            return;
        }

        CharacterBasic nearestPlayer = FindNearestAlivePlayer(creator.transform.position);

        if (nearestPlayer == null)
        {
            Debug.LogWarning($"{name}: EnemySwing found no alive player.");
            return;
        }

        Vector2 creatorPos = creator.transform.position;
        Vector2 targetPos = nearestPlayer.transform.position;

        movementDir = (targetPos - creatorPos).normalized;
        speed = speedFromProjectile;

        FireBehaviour();
    }

    public override void OnHitAnyEffect(Collider2D collision)
    {
        if (!IsServer) return;
        if (collision.gameObject == GetCreator()) return;
        if (collision.isTrigger) return;

        CharacterBasic player = collision.GetComponentInParent<CharacterBasic>();
        if (player == null) return;

        NetworkObject netObj = player.GetComponentInParent<NetworkObject>();
        if (netObj == null) return;

        if (playersHit.Contains(netObj.NetworkObjectId)) return;
        playersHit.Add(netObj.NetworkObjectId);

        player.TakeDamage(damage);
    }

    private CharacterBasic FindNearestAlivePlayer(Vector2 fromPosition)
    {
        CharacterBasic[] players = FindObjectsByType<CharacterBasic>(FindObjectsSortMode.None);

        CharacterBasic nearest = null;
        float nearestDistanceSq = float.PositiveInfinity;

        foreach (CharacterBasic player in players)
        {
            if (player == null) continue;
            if (!player.alive.Value) continue;

            NetworkObject netObj = player.GetComponentInParent<NetworkObject>();
            if (netObj == null || !netObj.IsSpawned) continue;

            float distanceSq = ((Vector2)player.transform.position - fromPosition).sqrMagnitude;

            if (distanceSq < nearestDistanceSq)
            {
                nearestDistanceSq = distanceSq;
                nearest = player;
            }
        }

        return nearest;
    }
}