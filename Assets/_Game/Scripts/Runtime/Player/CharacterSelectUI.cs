using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    public void updateUI()
    {
        if(CharacterSelectData.SelectedCharacter == 0)
        {
            text.text = "Priest";
            text.color = Color.yellow;
        }

        if (CharacterSelectData.SelectedCharacter == 1)
        {
            text.text = "Archer";
            text.color = Color.green;
        }

        if (CharacterSelectData.SelectedCharacter == 2)
        {
            text.text = "Mage";
            text.color = new Color(0,32,84);
        }

        if (CharacterSelectData.SelectedCharacter == 3)
        {
            text.text = "Warrior";
            text.color = Color.red;
        }
    }

    public void SelectYellow()
    {
        CharacterSelectData.SelectedCharacter = 0;
        updateUI();
    }

    public void SelectGreen()
    {
        CharacterSelectData.SelectedCharacter = 1;
        updateUI();
    }

    public void SelectBlue()
    {
        CharacterSelectData.SelectedCharacter = 2;
        updateUI();
    }

    public void SelectRed()
    {
        CharacterSelectData.SelectedCharacter = 3;
        updateUI();
    }
}
