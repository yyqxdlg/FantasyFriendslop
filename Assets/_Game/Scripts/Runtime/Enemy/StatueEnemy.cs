using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class StatueEnemy : EnemyBasic
{
    [Header("Statue Attack")]
    [SerializeField] private GameObject swingPrefab; // assign the EnemySwing prefab in the inspector
    [SerializeField] private float swingFireSpeed = 3f; // controls swing arc speed and duration
    [SerializeField] private float dashForce = 400f;    // impulse applied toward target per swing
    [SerializeField] private float timeBetweenSwings = 0.45f; // wait between each swing in the combo
    [SerializeField] private float postComboCooldown = 1.5f;  // pause after all 3 swings

    private bool isActivated = false;
    private bool isDoingCombo = false;

    protected override void ServerUpdate()
    {
        if (!isActivated)
        {
            rb.linearVelocity = Vector2.zero;

            if (HasSeenAllAlivePlayers())
            {
                isActivated = true;
            }
            return;
        }

        // Once activated, move toward nearest target and attack when close enough
        target = NearestLivingTarget();

        if (target != null)
        {
            float distance = DistanceToSelf(target);

            // Only walk toward the target when not mid-combo
            if (!isDoingCombo)
            {
                if (distance > attackRange)
                {
                    rb.linearVelocity = DirToTarget() * speed;
                }
                else
                {
                    rb.linearVelocity = Vector2.zero;
                    StartCoroutine(ComboAttack());
                }
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    // Returns true once every alive player in the game has stepped into this enemy's targeting range.
    // Uses NetworkManager to count all alive players, then checks how many are in the trigger zone.
    private bool HasSeenAllAlivePlayers()
    {
        int totalAlivePlayers = 0;
        int alivePlayersInRange = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClients.Values)
        {
            if (client.PlayerObject == null) continue;

            CharacterBasic character = client.PlayerObject.GetComponent<CharacterBasic>();
            if (character == null || !character.alive.Value) continue;

            totalAlivePlayers++;

            // Check if this player is currently inside range trigger
            for (int i = 0; i < targetingRange.GetNumberOfTargets(); i++)
            {
                if (targetingRange.GetTarget(i) == client.PlayerObject.gameObject)
                {
                    alivePlayersInRange++;
                    break;
                }
            }
        }

        return totalAlivePlayers > 0 && alivePlayersInRange >= totalAlivePlayers;
    }

    private IEnumerator ComboAttack()
    {
        isDoingCombo = true;

        for (int i = 0; i < 3; i++)
        {
            if (target == null) break;

            SpawnSwing();

            // Small dash toward the target on each swing
            rb.AddForce(DirToTarget() * dashForce);

            yield return new WaitForSeconds(timeBetweenSwings);
        }

        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(postComboCooldown);

        isDoingCombo = false;
    }

    private void SpawnSwing()
    {
        if (target == null) return;

        GameObject swing = Instantiate(swingPrefab, transform.position, Quaternion.identity);

        BulletMoveMP swingScript = swing.GetComponent<BulletMoveMP>();
        if (swingScript != null)
        {
            swingScript.damage = attackDamage;
            // Fire(creator, direction, speed multiplier)
            swingScript.Fire(gameObject, DirToTarget(), swingFireSpeed);
        }

        swing.GetComponent<NetworkObject>().Spawn();
    }
}