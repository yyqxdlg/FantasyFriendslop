using Unity.Netcode;
using UnityEngine;

public class Coin : Spawnable
{
    [Header("Pickup")]
    [SerializeField] private float pickupRadius = 0.9f;
    [SerializeField] private string playerTag = "Player";

    private bool pickedUp = false;

    private void Update()
    {
        if (!IsServer) return;
        if (pickedUp) return;

        TryPickupNearbyPlayer();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        if (pickedUp) return;

        TryPickupFromCollider(collision);
    }

    private void TryPickupNearbyPlayer()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius);

        foreach (Collider2D hit in hits)
        {
            if (TryPickupFromCollider(hit))
            {
                return;
            }
        }
    }

    private bool TryPickupFromCollider(Collider2D collision)
    {
        CharacterBasic playerScript = collision.GetComponent<CharacterBasic>();

        if (playerScript == null)
        {
            playerScript = collision.GetComponentInParent<CharacterBasic>();
        }

        if (playerScript == null)
        {
            return false;
        }

        if (!playerScript.CompareTag(playerTag) && !collision.CompareTag(playerTag))
        {
            return false;
        }

        pickedUp = true;
        playerScript.AddCoin(1);
        DespawnCoin();
        return true;
    }

    private void DespawnCoin()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();

        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
