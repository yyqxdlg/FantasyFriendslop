using UnityEngine;

public class ItemPickup : Spawnable
{

	public string prefabName;
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (!collision.isTrigger)
		{
			CharacterBasic player = collision.gameObject.GetComponent<CharacterBasic>();

			if(player != null)
			{
				player.AddToInventory(prefabName);

				NetworkDestroy();
			}
		}
	}
}
