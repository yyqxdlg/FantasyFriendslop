using TMPro;
using UnityEngine;

public class InGameUI : MonoBehaviour
{
	private CanvasGroup canvasGroup;

	[SerializeField] private TMP_Text cooldownText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text coinText;

	public int coinValue = 0;

	private float healthMax = 0;
	private float healthValue = 0;

	private float cooldownMax = 0;
    private float cooldownValue = 0;

	public static InGameUI Instance { get; private set; }

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;

		canvasGroup = GetComponent<CanvasGroup>();

		canvasGroup.alpha = 0;

        cooldownText.text = "NaN";
	}

	public void setText(string newText)
	{
        cooldownText.text = newText;
		 // Show when there's text, hide when empty
		canvasGroup.alpha = string.IsNullOrEmpty(newText) ? 0 : 1;
	}

	public void SetCoins(int newCoinValue)
	{
		coinValue = newCoinValue;
		UpdateCoin();
    }

    public void SetHealthMax(float newHealthMax)
    {
        healthMax = newHealthMax;
		UpdateHealth();
    }

    public void SetHealthValue(float newHealthValue)
    {
        healthValue = newHealthValue;
		UpdateHealth();
    }

    public void SetCooldownMax(float newCooldownMax)
    {
        cooldownMax = newCooldownMax;
		UpdateCooldown();

    }

    public void SetCooldownValue(float newCooldownValue)
    {
        cooldownValue = newCooldownValue;
		UpdateCooldown();
    }

	private void UpdateCoin()
	{
        coinText.text = coinValue.ToString();
    }

    private void UpdateHealth()
    {
		healthText.text = healthValue.ToString() + " / " + healthMax.ToString();
    }

    private void UpdateCooldown()
    {
        cooldownText.text = cooldownValue.ToString() + " / " + cooldownMax.ToString();
    }
}
