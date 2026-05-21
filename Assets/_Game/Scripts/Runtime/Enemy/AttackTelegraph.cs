using Unity.Netcode;
using UnityEngine;

public class AttackTelegraph : NetworkBehaviour
{
    [Header("Warning Prefabs")]
    [SerializeField] private GameObject circleWarningPrefab;
    [SerializeField] private GameObject boxWarningPrefab;

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "AttackWarning";
    [SerializeField] private int sortingOrder = 100;

    [ClientRpc]
    public void ShowCircleClientRpc(Vector2 center, float radius, float duration)
    {
        if (circleWarningPrefab == null) return;

        GameObject warning = Instantiate(
            circleWarningPrefab,
            center,
            Quaternion.identity
        );

        warning.transform.localScale = Vector3.one * radius * 2f;

        ApplySorting(warning);

        Destroy(warning, duration);
    }

    [ClientRpc]
    public void ShowBoxClientRpc(Vector2 center, Vector2 size, float angle, float duration)
    {
        if (boxWarningPrefab == null) return;

        GameObject warning = Instantiate(
            boxWarningPrefab,
            center,
            Quaternion.Euler(0f, 0f, angle)
        );

        warning.transform.localScale = new Vector3(size.x, size.y, 1f);

        ApplySorting(warning);

        Destroy(warning, duration);
    }

    private void ApplySorting(GameObject obj)
    {
        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();

        foreach (SpriteRenderer sr in renderers)
        {
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrder;
        }

        ParticleSystemRenderer[] particleRenderers =
            obj.GetComponentsInChildren<ParticleSystemRenderer>();

        foreach (ParticleSystemRenderer pr in particleRenderers)
        {
            pr.sortingLayerName = sortingLayerName;
            pr.sortingOrder = sortingOrder;
        }
    }
}