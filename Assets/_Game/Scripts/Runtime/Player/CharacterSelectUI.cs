using UnityEngine;

public class CharacterSelectUI : MonoBehaviour
{
    public void SelectYellow()
    {
        CharacterSelectData.SelectedCharacter = 0;
    }

    public void SelectGreen()
    {
        CharacterSelectData.SelectedCharacter = 1;
    }

    public void SelectBlue()
    {
        CharacterSelectData.SelectedCharacter = 2;
    }

    public void SelectRed()
    {
        CharacterSelectData.SelectedCharacter = 3;
    }
}
