using UnityEngine;
using UnityEngine.UI;

public class BossHealthbarUI : MonoBehaviour
{
    public static BossHealthbarUI Instance { get; private set; }

    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image bossIcon;
    [SerializeField] private Text bossNameText;

    private EnemyBasic currentBoss;

    private void Awake()
    {
        Instance = this;

        if (rootGroup == null)
            rootGroup = GetComponent<CanvasGroup>();

        Hide();
    }

    private void Update()
    {
        if (currentBoss == null)
        {
            Hide();
            return;
        }

        healthSlider.value = Mathf.Clamp01(currentBoss.health.Value / currentBoss.maxHealth);
    }

    public void Show(EnemyBasic boss, string displayName, Sprite icon = null)
    {
        currentBoss = boss;

        if (bossNameText != null)
            bossNameText.text = displayName;

        if (bossIcon != null && icon != null)
            bossIcon.sprite = icon;

        rootGroup.alpha = 1f;
        rootGroup.blocksRaycasts = false;
        rootGroup.interactable = false;
    }

    public void Hide()
    {
        currentBoss = null;

        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.blocksRaycasts = false;
            rootGroup.interactable = false;
        }
    }

    public void HideIfShowing(EnemyBasic boss)
    {
        if (currentBoss == boss)
            Hide();
    }
}