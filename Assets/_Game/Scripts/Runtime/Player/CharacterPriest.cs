using Unity.Netcode;
using UnityEngine;

public class CharacterPriest : CharacterBasic
{
	public GameObject radiusObject;

	private ObjectListCollider objectsInAura;

	private SpriteRenderer auraRenderer;


	public float auraCooldownMax;

	private float auraCooldownCurr;

	public float auraHeal;

	public float auraDamage;

	private bool healAura = true;
	public override void Update()
	{
		base.Update();

		if(auraCooldownCurr > 0)
		{
			auraCooldownCurr -= Time.deltaTime;
		}
		else
		{
			auraCooldownCurr = auraCooldownMax;

            ApplyAura();
		}
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		if (!IsOwner){return;}

		objectsInAura = radiusObject.GetComponent<ObjectListCollider>();

		auraRenderer = radiusObject.GetComponent<SpriteRenderer>();

		auraRenderer.color = Color.green;
	}

	private void ApplyAura()
	{
		if (healAura)
		{
			for (int i = 0; i < objectsInAura.GetNumberOfTargets(); i++)
			{
				CharacterBasic currPlayerScript = objectsInAura.GetTarget(i).GetComponent<CharacterBasic>();
				if (currPlayerScript != null)
				{
					currPlayerScript.HealAmount(auraHeal);
				}
			}
		}
		else
		{

			for (int i = 0; i < objectsInAura.GetNumberOfTargets(); i++)
			{
				EnemyBasic currEnemyScript = objectsInAura.GetTarget(i).GetComponent<EnemyBasic>();

                if (currEnemyScript != null)
				{
					currEnemyScript.TakeDamage(auraDamage);
				}
			}
		}
	}

	public override void DoAbility(int abilityId)
	{
		if (abilityId == 0)
		{
			healAura = !healAura;

			if (healAura)
			{
                auraRenderer.color = Color.green;
            }
            else
            {
                auraRenderer.color = Color.red;
            }
        }
	}
}
