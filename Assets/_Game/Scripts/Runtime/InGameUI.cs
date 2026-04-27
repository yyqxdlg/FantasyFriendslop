using TMPro;
using UnityEngine;

public class InGameUI : MonoBehaviour
{
	private CanvasGroup canvasGroup;

	[SerializeField] private TMP_Text text;

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
		canvasGroup.blocksRaycasts = true;

		text.text = "Hello world";
	}

	// Update is called once per frame
	public void setText(string newText)
	{
		text.text = newText;
		 // Show when there's text, hide when empty
    canvasGroup.alpha = string.IsNullOrEmpty(newText) ? 0 : 1;
	}
}
