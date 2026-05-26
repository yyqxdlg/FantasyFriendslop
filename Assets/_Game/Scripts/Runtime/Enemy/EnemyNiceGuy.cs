using UnityEngine;

// Nice Guy 敌人：
//   行走时显示正面/背面，不攻击玩家（或攻击力极低）
//   无掉落物
//
// Animator 参数：
//   AnimIndex (Float)
//     0 = Idle_Front
//     1 = Idle_Back
//     2 = Walk_Front
//     3 = Walk_Back
//   IsDead (Bool)
//
// ★ Prefab 上的 EnemyBasic 设置：
//   Anim Profile              = FrontBackIdleWalk
//   Use Anim Index Blend Tree = true
//   Has Death Animation       = true
//   Drop Loot On Death        = false  ← 无掉落
//   Attack Damage             = 0      ← 不攻击（或设一个极低值）
//   Speed                     = 1.2   ← 走得慢一些更"悠闲"

public class EnemyNiceGuy : EnemyBasic
{
    // Nice Guy 的核心行为和普通 EnemyBasic 一样：发现玩家就走过去
    // 但如果你想让他"无害"地游荡（不追玩家），
    // 可以把 Targeting Range 的 Collider 半径设得很小，或者取消攻击

    public override void TakeDamage(float Damage)
    {
        SpawnerUtil.Instance.NetworkSpawnGameObject("NotNiceGuy", transform.position);

        PlayDeathSound();

        NetworkDestroy();
    }
    public override void Attack()
    {
        target.GetComponent<CharacterBasic>().HealAmount(attackDamage);

        PlayAttackSound();
    }
}
