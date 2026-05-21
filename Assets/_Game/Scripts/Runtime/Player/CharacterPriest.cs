using Unity.Netcode;
using UnityEngine;

public class CharacterPriest : CharacterBasic
{
	public GameObject radiusObject;

	private ObjectListCollider objectsInAura;

	private SpriteRenderer auraRenderer;


	public float auraCooldownMax = 1f;

	private float auraCooldownCurr;

	public float auraHeal = 1f;

	public float auraDamage = 1f;

	public float selfApplyPart = 0.2f;

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

		if (!IsOwner){
			return;
		}

		objectsInAura = radiusObject.GetComponent<ObjectListCollider>();

		auraRenderer = radiusObject.GetComponent<SpriteRenderer>();

		Color newColor = Color.green;
		newColor.a = 0.5f;

		auraRenderer.color = newColor;

        auraRenderer.enabled = true;
    }

	private void ApplyAura()
	{
		if (!IsOwner) { return; }
		if (healAura)
		{
            HealAmount(auraHeal*selfApplyPart);

            for (int i = 0; i < objectsInAura.GetNumberOfTargets(); i++)
			{
				CharacterBasic currPlayerScript = objectsInAura.GetTarget(i).GetComponent<CharacterBasic>();
                EnemyBasic currEnemyScript = objectsInAura.GetTarget(i).GetComponent<EnemyBasic>();

                if (currPlayerScript != null)
				{
					currPlayerScript.HealAmount(auraHeal);
				}

                if (currEnemyScript != null)
                {
                    currEnemyScript.HealAmount(auraHeal);
                }
            }
		}
		else
		{
			TakeDamage(auraDamage*selfApplyPart);

			for (int i = 0; i < objectsInAura.GetNumberOfTargets(); i++)
			{
                CharacterBasic currPlayerScript = objectsInAura.GetTarget(i).GetComponent<CharacterBasic>();
                EnemyBasic currEnemyScript = objectsInAura.GetTarget(i).GetComponent<EnemyBasic>();

                if (currEnemyScript != null)
				{
					currEnemyScript.TakeDamage(auraDamage);
				}

                if (currPlayerScript != null)
                {
                    currPlayerScript.TakeDamage(auraDamage);
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
                Color newColor = Color.green;
                newColor.a = 0.5f;

                auraRenderer.color = newColor;
            }
            else
            {
                Color newColor = Color.red;
                newColor.a = 0.5f;

                auraRenderer.color = newColor;
            }
        }
	}
}
