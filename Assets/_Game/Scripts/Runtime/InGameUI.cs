using TMPro;
using UnityEngine;

public class InGameUI : MonoBehaviour
{
	private CanvasGroup canvasGroup;

	[SerializeField] private TMP_Text cooldownText;

	public int coinValue = 0;

	private float healthMax = 0;
	private float healthValue = 0;

	private float cooldownMax = 0;
    private float cooldownValue = 0;

	public PersonalStatusbar statusBar;

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

	public void SetHasKey(bool hasKey)
	{
		statusBar.SetHasKey(hasKey);
	}

	public void SetType(int type)
	{
		statusBar.SetType(type);
	}

	private void UpdateCoin()
	{
		statusBar.UpdateCoin(coinValue);
    }

    private void UpdateHealth()
    {
		statusBar.UpdateHealth(healthValue, healthMax);
    }

    private void UpdateCooldown()
    {
        cooldownText.text = cooldownValue.ToString() + " / " + cooldownMax.ToString();
    }
}
