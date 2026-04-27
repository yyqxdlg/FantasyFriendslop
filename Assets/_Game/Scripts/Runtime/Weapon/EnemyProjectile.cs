using Unity.Netcode;
using UnityEngine;

public class EnemyProjectile : BasicProjectile
{
    public override void Fire()
    {
        GameObject creator = GetCreator();

        if (creator == null)
        {
            Debug.LogError($"{name}: EnemyProjectile creator is null.");
            return;
        }

        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError($"{name}: Missing Rigidbody2D.");
            return;
        }

        CharacterBasic nearestTarget = FindNearestAlivePlayer(creator.transform.position);

        if (nearestTarget == null)
        {
            Debug.LogWarning($"{name}: No alive player found.");
            return;
        }

        Vector2 creatorPos = creator.transform.position;
        Vector2 targetPos = nearestTarget.transform.position;

        movementDir = (targetPos - creatorPos).normalized;
        speed = speedFromProjectile;

        FireBehaviour();
    }

    public override void OnHitAnyEffect(Collider2D collision)
    {
        CharacterBasic playerHit = collision.GetComponentInParent<CharacterBasic>();

        if (playerHit == null) return;
        if (!playerHit.alive.Value) return;

        playerHit.TakeDamage(damage);
    }

    private CharacterBasic FindNearestAlivePlayer(Vector2 fromPosition)
    {
        CharacterBasic[] players = FindObjectsByType<CharacterBasic>(FindObjectsSortMode.None);

        CharacterBasic nearest = null;
        float nearestDistSq = float.PositiveInfinity;

        foreach (CharacterBasic player in players)
        {
            if (player == null) continue;
            if (!player.alive.Value) continue;

            NetworkObject netObj = player.GetComponentInParent<NetworkObject>();
            if (netObj == null || !netObj.IsSpawned) continue;

            float distSq = ((Vector2)player.transform.position - fromPosition).sqrMagnitude;

            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearest = player;
            }
        }

        return nearest;
    }
}