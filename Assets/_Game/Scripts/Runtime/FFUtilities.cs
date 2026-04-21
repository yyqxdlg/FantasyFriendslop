using UnityEngine;

public class FFUtilities : MonoBehaviour
{
    public static float CounterClockwiseAngle(Vector2 a, Vector2 b)
    {
        return -Mathf.Atan2((a.x * b.y - a.y * b.x), Vector2.Dot(a, b)) * (180 / Mathf.PI);
    }
}
