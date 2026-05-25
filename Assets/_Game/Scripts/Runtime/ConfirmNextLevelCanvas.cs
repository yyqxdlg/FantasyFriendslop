using TMPro;
using UnityEngine;

public class ConfirmNextLevelCanvas : MonoBehaviour
{
	private CanvasGroup group;

	public TMP_Text text;

	private EndLevelButton endBtn;

	public static ConfirmNextLevelCanvas Instance { get; private set; }
	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
	}

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{
		group = GetComponent<CanvasGroup>();

		group.interactable = false;
		group.blocksRaycasts = false;
		group.alpha = 0f;
	}

	public void Hide()
	{
		group.interactable = false;
		group.blocksRaycasts = false;
		group.alpha = 0f;
	}

	public void Show()
	{
		group.interactable = true;
		group.blocksRaycasts = true;
		group.alpha = 1f;
	}

	public void SetText(string newText)
	{
		text.text = newText;
	}

	public void SetOnclickEvent(EndLevelButton btn)
	{
        endBtn = btn;
	}

	public void ClickYes()
	{
		Debug.Log("CONFIRM CLICK");
        Hide();
        endBtn.ConfirmClick();
    }

	public void ClickNo()
	{
		Hide();
	}
}
