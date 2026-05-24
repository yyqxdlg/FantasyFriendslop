using System.Collections;
using TMPro;
using UnityEngine;

public class SystemMessage : MonoBehaviour
{
    public static SystemMessage Instance { get; private set; }

    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private GameObject messageBar;

    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 普通白色提示
    public static void Show(string message)
    {
        Instance?.ShowInternal(message, Color.white);
    }

    // 红色报错
    public static void ShowError(string message)
    {
        Instance?.ShowInternal(message, new Color(1f, 0.4f, 0.4f));
    }

    // 绿色成功
    public static void ShowSuccess(string message)
    {
        Instance?.ShowInternal(message, new Color(0.4f, 1f, 0.6f));
    }

    private void ShowInternal(string message, Color color)
    {
        if (messageBar != null) messageBar.SetActive(true);
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = color;
        }

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        if (messageBar != null) messageBar.SetActive(false);
    }
}