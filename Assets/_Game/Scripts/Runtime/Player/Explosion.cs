using UnityEngine;

public class Explosion : Spawnable
{
    public float initialScale;

    public float finalScale;

    public float damage;

    public float timeToFullScale;

    private float time;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        time = 0;

        if (!IsOwner) { return; }
        Invoke("NetworkDestroy", timeToFullScale);
    }

    private float GetCurrentScale()
    {
        float t = (time / timeToFullScale);

        return (initialScale) + t * (finalScale - initialScale);
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;

        float scale = GetCurrentScale();

        gameObject.transform.localScale = new Vector3(scale, scale, scale);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) { return; }

        if (!collision.isTrigger)
        {
            CharacterBasic playerScript = collision.gameObject.GetComponent<CharacterBasic>();
            EnemyBasic enemyScript = collision.gameObject.GetComponent<EnemyBasic>();

            if (playerScript != null)
            {
                playerScript.TakeDamage(damage);
            }

            if (enemyScript != null)
            {
                enemyScript.TakeDamage(damage);
            }
        }
    }
}
