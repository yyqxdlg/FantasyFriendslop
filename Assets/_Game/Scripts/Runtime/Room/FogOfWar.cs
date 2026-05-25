using UnityEngine;
using Unity.Netcode;
public class FogOfWar : NetworkBehaviour
{
    public NetworkVariable<bool> revealed = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        revealed.OnValueChanged += OnRevealedChange;

        OnRevealedChange(false, revealed.Value);
    }

    public void OnRevealedChange(bool prev, bool next)
    {
        if (next)
        {
            Reveal();
        } else
        {
            Obscure();
        }
    }

    public void Reset()
    {
        revealed.Value = false;
    }

    public void Obscure()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();

        SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>();

        renderer.enabled = true;
        renderer.color = Color.black;

        foreach (SpriteRenderer childRenderer in childRenderers)
        {
            childRenderer.enabled = true;
            childRenderer.color = Color.black;
        }
    }

    public void Reveal()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();

        SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>();

        renderer.enabled = false;

        foreach (SpriteRenderer childRenderer in childRenderers)
        {
            childRenderer.enabled = false;
        }
    }
}
