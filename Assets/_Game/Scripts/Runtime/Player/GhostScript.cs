using Unity.Netcode;
using UnityEngine;

public class GhostScript : Spawnable
{
    private Vector2 movement;
    private Rigidbody2D rb;
    private Camera cam;

    [Header("Ghost Movement")]
    [SerializeField] private float speed = 1f;

    [Header("Ghost Visual")]
    [SerializeField] private int type = 0;
    [SerializeField] private Animator animator;

    public NetworkVariable<bool> isMoving = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator != null)
            animator.SetInteger("Type", type);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            cam = Camera.main;
        }

        NetworkObject netObj = GetComponent<NetworkObject>();

        if (GameplayManager.Instance != null && netObj != null)
        {
            GameplayManager.Instance.AddGhost(netObj.NetworkObjectId);
        }
    }

    public override void OnNetworkDespawn()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();

        if (GameplayManager.Instance != null && netObj != null)
        {
            GameplayManager.Instance.RemoveGhost(netObj.NetworkObjectId);
        }

        base.OnNetworkDespawn();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (cam == null)
            cam = Camera.main;

        if (cam != null)
        {
            cam.transform.position = new Vector3(
                transform.position.x,
                transform.position.y,
                -10f
            );
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        isMoving.Value = movement != Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        if (rb == null) return;

        rb.linearVelocity = movement.normalized * speed;
    }
}