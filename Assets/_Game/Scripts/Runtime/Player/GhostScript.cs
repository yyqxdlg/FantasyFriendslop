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
            cam = Camera.main;

        if (IsServer)
            AddSelfToGhostList();

        // Ghost 生成时启用 Plates
        if (IsServer)
        {
            SelectPlateController plateController = FindObjectOfType<SelectPlateController>();
            if (plateController != null)
                plateController.EnablePlates();
        }
    }

    private void AddSelfToGhostList()
    {
        if (GameplayManager.Instance.GetComponent<NetworkObject>().IsSpawned)
        {
            NetworkObject netObj = GetComponent<NetworkObject>();
            GameplayManager.Instance.AddGhost(netObj.NetworkObjectId);
        }
        else
        {
            Debug.Log("WAIT WITH ADD FOR GAMEPLAYMANAGER SPAWN");
            Invoke("AddSelfToGhostList", 0.1f);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (GameplayManager.Instance != null && netObj != null)
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

    public void Teleport(Vector3 posTo)
    {
        TeleportOwnerRpc(posTo);
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Everyone)]
    public void TeleportOwnerRpc(Vector3 posTo)
    {
        gameObject.transform.position = posTo;
    }

    public void Respawn()
    {
        RespawnServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RespawnServerRpc()
    {
        ulong clientId = OwnerClientId;

        if (LobbyNetworkState.Instance == null)
        {
            Debug.LogError("LobbyNetworkState missing!");
            return;
        }

        int heroId = -1;
        for (int i = 0; i < LobbyNetworkState.Instance.Players.Count; i++)
        {
            if (LobbyNetworkState.Instance.Players[i].ClientId == clientId)
            {
                heroId = LobbyNetworkState.Instance.Players[i].HeroId;
                break;
            }
        }

        if (heroId < 0)
        {
            Debug.LogError("No hero found for client " + clientId);
            return;
        }

        Vector3 spawnPos = transform.position;

        // 先 Despawn Ghost
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
            netObj.Despawn(true);

        // 生成对应英雄
        LobbyNetworkState.Instance.SpawnHeroForPlayer(clientId, heroId, spawnPos);
    }
}