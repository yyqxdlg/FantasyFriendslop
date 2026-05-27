#if UNITY_EDITOR
using System.Reflection;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public static class PrintNetworkPrefabHashes
{
    [MenuItem("Tools/Netcode/Print NetworkObject Hashes")]
    public static void PrintHashes()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
                continue;

            NetworkObject[] networkObjects = prefab.GetComponentsInChildren<NetworkObject>(true);

            foreach (NetworkObject netObj in networkObjects)
            {
                uint hash = TryGetGlobalObjectIdHash(netObj);

                if (hash == 0)
                {
                    Debug.LogWarning($"Could not read hash | {prefab.name} | {GetTransformPath(netObj.transform)} | {path}", prefab);
                    continue;
                }

                Debug.Log($"{hash} | Prefab: {prefab.name} | Object: {GetTransformPath(netObj.transform)} | Path: {path}", prefab);
            }
        }
    }

    private static uint TryGetGlobalObjectIdHash(NetworkObject netObj)
    {
        if (netObj == null)
            return 0;

        System.Type type = typeof(NetworkObject);

        // Some Netcode versions expose it as a public/non-public property.
        PropertyInfo property = type.GetProperty(
            "GlobalObjectIdHash",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (property != null)
        {
            object value = property.GetValue(netObj);

            if (value is uint uintValue)
                return uintValue;

            if (value is int intValue)
                return unchecked((uint)intValue);
        }

        // Some versions store it as a serialized/private field.
        string[] possibleFieldNames =
        {
            "GlobalObjectIdHash",
            "m_GlobalObjectIdHash"
        };

        foreach (string fieldName in possibleFieldNames)
        {
            FieldInfo field = type.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (field == null)
                continue;

            object value = field.GetValue(netObj);

            if (value is uint uintValue)
                return uintValue;

            if (value is int intValue)
                return unchecked((uint)intValue);
        }

        // Last fallback: read serialized property directly.
        SerializedObject serializedObject = new SerializedObject(netObj);

        foreach (string propertyName in possibleFieldNames)
        {
            SerializedProperty serializedProperty = serializedObject.FindProperty(propertyName);

            if (serializedProperty == null)
                continue;

            return unchecked((uint)serializedProperty.longValue);
        }

        return 0;
    }

    private static string GetTransformPath(Transform transform)
    {
        if (transform == null)
            return "";

        string path = transform.name;

        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }
}
#endif