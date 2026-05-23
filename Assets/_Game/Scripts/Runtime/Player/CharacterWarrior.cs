using UnityEngine;

public class CharacterWarrior : CharacterBasic
{
	public ObjectListCollider swingBoxSmall;
	private SpriteRenderer swingBoxRendererSmall;

	public ObjectListCollider swingBoxLarge;
	private SpriteRenderer swingBoxRendererLarge;

	public float swingKnockback = 1f;

	public float swingDamage = 1f;

	public float largeSwingKnockback = 5f;

	public float largeSwingDamage = 10f;


	public override void Awake()
	{
		base.Awake();

		swingBoxRendererSmall = swingBoxSmall.gameObject.GetComponent<SpriteRenderer>();

		swingBoxRendererLarge = swingBoxLarge.gameObject.GetComponent<SpriteRenderer>();
	}

	public override void Update()
	{
		base.Update();

		if (shooting)
		{
			ShowRendererTransparent(swingBoxRendererSmall);
		} else
		{
			HideRenderer(swingBoxRendererSmall);
		}

        if (attemptingAbilities[0])
        {
            ShowRendererTransparent(swingBoxRendererLarge);
        }
        else
        {
            HideRenderer(swingBoxRendererLarge);
        }
    }

	public void HideRenderer(SpriteRenderer renderer)
	{
		Color tempColor = renderer.color;
		tempColor.a = 0f;
		renderer.color = tempColor;
	}

	public void ShowRendererTransparent(SpriteRenderer renderer)
	{
		Color tempColor = renderer.color;
		tempColor.a = 0.2f;
		renderer.color = tempColor;
	}


	public override void Attack()
	{
		updateMousePos();

		HitAllInCone(swingBoxSmall, swingKnockback, swingDamage);

		PlayAttackSound();

		PlayAttackAnimation();
	}

	public override void DoAbility(int abilityId)
	{
		if (abilityId == 0)
		{
			updateMousePos();

			HitAllInCone(swingBoxLarge, largeSwingDamage, largeSwingKnockback);

            PlayAttackAnimation();

			PlayAbilitySound();
        }
	}

	private void HitAllInCone(ObjectListCollider boxScript, float damage, float knockback)
	{
		for (int i = 0; i < boxScript.GetNumberOfTargets(); i++)
		{
			EnemyBasic currEnemy = boxScript.GetTarget(i).GetComponent<EnemyBasic>();

			if (currEnemy != null)
			{
				Vector2 knockBackVector = (currEnemy.gameObject.transform.position - gameObject.transform.position) * 1 * knockback;

				currEnemy.KnockBack(knockBackVector);

				currEnemy.TakeDamage(damage);
			}
		}
	}


}
