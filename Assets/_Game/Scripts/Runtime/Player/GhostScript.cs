using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

public class GhostScript : NetworkBehaviour
{

    private Vector2 movement;

    private Rigidbody2D rb;

    public NetworkVariable<bool> isMoving = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
    );

    public float speed = 1f;

    private Camera cam;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        //animator = GetComponent<Animator>();
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

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
}
