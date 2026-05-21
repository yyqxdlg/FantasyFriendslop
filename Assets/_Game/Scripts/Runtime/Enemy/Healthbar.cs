using UnityEngine;
using UnityEngine.UI;

public class Healthbar : MonoBehaviour
{
    public Slider mainSlider;

    [Header("Optional")]
    [SerializeField] private CanvasGroup barGroup;

    private void Awake()
    {
        if (mainSlider == null)
            mainSlider = GetComponentInChildren<Slider>(true);

        if (barGroup == null)
            barGroup = GetComponent<CanvasGroup>();

        if (barGroup == null)
            barGroup = GetComponentInChildren<CanvasGroup>(true);

        if (barGroup == null)
            barGroup = gameObject.AddComponent<CanvasGroup>();

        barGroup.interactable = false;
        barGroup.blocksRaycasts = false;
    }

    public void Hide()
    {
        EnsureCanvasGroup();

        barGroup.alpha = 0f;
        barGroup.interactable = false;
        barGroup.blocksRaycasts = false;
    }

    public void UnHide()
    {
        EnsureCanvasGroup();

        barGroup.alpha = 1f;
        barGroup.interactable = false;
        barGroup.blocksRaycasts = false;
    }

    public void UpdateHealthBar(float health, float maxHealth)
    {
        if (mainSlider == null) return;
        if (maxHealth <= 0f) return;

        mainSlider.value = health / maxHealth;
    }

    private void EnsureCanvasGroup()
    {
        if (barGroup != null) return;

        barGroup = GetComponent<CanvasGroup>();

        if (barGroup == null)
            barGroup = GetComponentInChildren<CanvasGroup>(true);

        if (barGroup == null)
            barGroup = gameObject.AddComponent<CanvasGroup>();

        barGroup.interactable = false;
        barGroup.blocksRaycasts = false;
    }
}