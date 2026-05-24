using UnityEditor;
using UnityEngine;

public class AnchorToCorners : EditorWindow
{
    [MenuItem("Tools/Anchor To Corners %&a")]
    static void Apply()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect == null) continue;

            RectTransform parent = rect.parent as RectTransform;
            if (parent == null) continue;

            Vector2 parentSize = parent.rect.size;

            Vector2 anchorMin = new Vector2(
                rect.anchorMin.x + rect.offsetMin.x / parentSize.x,
                rect.anchorMin.y + rect.offsetMin.y / parentSize.y
            );
            Vector2 anchorMax = new Vector2(
                rect.anchorMax.x + rect.offsetMax.x / parentSize.x,
                rect.anchorMax.y + rect.offsetMax.y / parentSize.y
            );

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}