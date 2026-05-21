using UnityEngine;

// Not-so Nice Guy 敌人：
//   使用 DownSet / UpSet 两套动画，并且每套都支持左右镜像。
//
// 动画含义：
//   Down 那套：用于玩家/移动方向在敌人下方时
//     Idle_Down
//     Walk_Down
//     Attack_Down
//
//   Up 那套：用于玩家/移动方向在敌人上方时
//     Idle_Up
//     Walk_Up
//     Attack_Up
//
// 左右含义：
//   玩家/移动方向在左边：使用原图
//   玩家/移动方向在右边：SpriteRenderer.flipX 镜像
//
// Animator 参数：
//   AnimIndex (Float)
//     0 = Idle_DownSet
//     1 = Idle_UpSet
//     2 = Walk_DownSet
//     3 = Walk_UpSet
//
//   AttackIndex (Float)
//     0 = Attack_DownSet
//     1 = Attack_UpSet
//
//   Attack   (Trigger)
//   IsDead   (Bool)
//   IsMoving (Bool)
//   Facing   (Int)
//
// Prefab 上 EnemyBasic 设置：
//   Anim Profile                 = UpDownMirrorIdleWalk
//   Use Anim Index Blend Tree    = true
//   Use Attack Index Blend Tree  = true
//   Use Flip For Side Directions = true
//   Side Sprite Faces Left       = true
//
//   Has Attack Animation = true
//   Has Death Animation  = true
//   Drop Loot On Death   = false
//
// Death:
//   如果死亡只有一套，可以直接用 Death_DownSet，不需要镜像。

public class EnemyNotSoNiceGuy : EnemyBasic
{
}