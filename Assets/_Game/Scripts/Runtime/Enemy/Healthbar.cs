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
    public void UpdateHealthBar(float health, float maxHealth)
    {
        mainSlider.value = health / maxHealth;
    }
}
