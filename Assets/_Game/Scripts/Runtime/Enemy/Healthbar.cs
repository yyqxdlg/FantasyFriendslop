using UnityEngine;
using UnityEngine.UI;

public class Healthbar : MonoBehaviour
{

    public Slider mainSlider;


    public void UpdateHealthBar(float health, float maxHealth)
    {
        mainSlider.value = health / maxHealth;
    }
}
