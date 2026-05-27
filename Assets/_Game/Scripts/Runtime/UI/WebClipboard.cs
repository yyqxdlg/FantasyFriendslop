using System.Runtime.InteropServices;
using UnityEngine;

public static class WebClipboard
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void WebCopyToClipboard(string text);
#endif

    public static void Copy(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

#if UNITY_WEBGL && !UNITY_EDITOR
        WebCopyToClipboard(text);
#else
        GUIUtility.systemCopyBuffer = text;
#endif
    }
}