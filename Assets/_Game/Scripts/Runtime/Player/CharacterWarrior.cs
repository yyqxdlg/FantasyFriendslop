using UnityEngine;

public class CharacterWarrior : CharacterBasic
{
    public ObjectListCollider swingBoxSmall;

    public ObjectListCollider swingBoxLarge;

    public float swingKnockback = 1f;

    public float swingDamage = 1f;

    public float largeSwingKnockback = 5f;

    public float largeSwingDamage = 10f;

    public override void Attack()
    {
        updateMousePos();

        HitAllInCone(swingBoxSmall, swingKnockback, swingDamage);

        PlayAttackSound();
    }

    public override void DoAbility(int abilityId)
    {
        if (abilityId == 0)
        {
            updateMousePos();

            HitAllInCone(swingBoxLarge, largeSwingDamage, largeSwingKnockback);
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
