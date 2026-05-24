using UnityEngine;

public class FogOfWar : MonoBehaviour
{

    public void Start()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();

        SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>();

        renderer.enabled = true;

        foreach(SpriteRenderer childRenderer in childRenderers)
        {
            childRenderer.enabled = true;
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
