using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class Coin : Spawnable
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Player" && IsOwner)
        {
            CharacterBasic playerScript = collision.gameObject.GetComponent<CharacterBasic>();

            if (playerScript != null)
            {
                playerScript.AddCoin(1);
                NetworkDestroy();

            }
        }
       
    }
}
