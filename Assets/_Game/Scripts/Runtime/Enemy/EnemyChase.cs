#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

public class EnemyChase : MonoBehaviour
{
    public float speed = 2f;
    public float chaseRange = 4f;
    public float stopDistance = 0.8f;

    private Rigidbody2D rb;
    private Transform player;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= chaseRange && distance > stopDistance)
        {
            Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * speed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Handles.color = Color.yellow;
        Handles.DrawWireDisc(transform.position, Vector3.forward, chaseRange);

        Handles.color = Color.red;
        Handles.DrawWireDisc(transform.position, Vector3.forward, stopDistance);
    }
    #endif
}