using UnityEngine;

public class ItemPickup : Spawnable
{

	public string prefabName;
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (!collision.isTrigger)
		{
			CharacterBasic player = collision.gameObject.GetComponent<CharacterBasic>();
			EnemyBasic enemy = collision.gameObject.GetComponent<EnemyBasic>();

			if(player != null)
			{
				player.AddToInventory(prefabName);

				NetworkDestroy();
			}

			if (enemy != null)
			{
				if (enemy.hasInventory)
				{
                    enemy.AddToInventory(prefabName);

                    NetworkDestroy();
                }
            }
		}
	}
}
