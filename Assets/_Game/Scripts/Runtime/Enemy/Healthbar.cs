using UnityEngine;
using UnityEngine.UI;

public class Healthbar : MonoBehaviour
{

    public Slider mainSlider;

    public void Hide()
    {
        CanvasGroup barGroup = gameObject.GetComponentInParent<CanvasGroup>();

        barGroup.alpha = 0;

        barGroup.blocksRaycasts = true;
    }

    public void UnHide()
    {
        CanvasGroup barGroup = gameObject.GetComponentInParent<CanvasGroup>();

        barGroup.alpha = 1;

        barGroup.blocksRaycasts = false;
    }
    public void UpdateHealthBar(float health, float maxHealth)
    {
        mainSlider.value = health / maxHealth;
    }
}
