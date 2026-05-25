using UnityEngine;

public class SpawnPointSet : MonoBehaviour
{
    public Transform[] points;

    public Vector3 GetPoint(int playerIndex)
    {
        if (points == null || points.Length == 0) return transform.position;
        int idx = Mathf.Clamp(playerIndex, 0, points.Length - 1);
        return points[idx].position;
    }
}
