using System.Globalization;
using Unity.Netcode;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

public class GhostScript : Spawnable
{

    private Vector2 movement;

    private Rigidbody2D rb;

    public NetworkVariable<bool> isMoving = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
    );

    public float speed = 1f;

    [SerializeField] private int type = 0;

    private Camera cam;

    public Animator animator; 

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        //animator = GetComponent<Animator>();
        cam = Camera.main;

        animator.SetInteger("Type", type);
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        cam.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, -10);

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // update isMoving
        isMoving.Value = movement != Vector2.zero;
    }

    void FixedUpdate()
    {
        // 
        if (!IsOwner) return;

        rb.linearVelocity = movement.normalized * speed;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        GameplayManager.Instance.AddGhost(GetComponent<NetworkObject>().NetworkObjectId);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        GameplayManager.Instance.RemoveGhost(GetComponent<NetworkObject>().NetworkObjectId);
    }
}
