using UnityEngine;

public class ConstantCheckOLC : ObjectListCollider
{

    private bool ColliderContains(GameObject target)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            if(target == targets[i])
            {
                return true;
            }
        }

        return false;
    }

    private void ConditionalAdd(GameObject target)
    {
        if (!ColliderContains(target))
        {
            targets.Add(target);
        }
    }

    public void OnTriggerStay2D(Collider2D collision)
    {
        Debug.Log("ON TRIGGER STAY? " + collision.gameObject.name);

        if (!IsOwner) return;

        if (!collision.isTrigger)
        {
            ConditionalAdd(collision.gameObject);
        }
    }
}
