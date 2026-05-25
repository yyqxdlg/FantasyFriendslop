using UnityEngine;

public class FogOfWarColliderTrigger : MonoBehaviour
{

    public FogOfWar fow;
    public void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.isTrigger)
        {
            CharacterBasic player = col.gameObject.GetComponent<CharacterBasic>();

            if(player != null) {

                if (player.isMe)
                {
                    fow.Reveal();
                }
            }
        }
    }
}
