using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Attacks at range 2.\nRuns away if you get too close.")]
public class Octopus : AIActor {
  public override float turnPriority => task is AttackGroundTask ? 90 : base.turnPriority;

  public Octopus(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 3;
  }

  public static bool IsInRange(Entity octopus, Entity target) {
    return Util.DiamondMagnitude(target.pos - octopus.pos) <= 2;
  }

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;
    if (IsNextTo(player)) {
      return new RunAwayTask(this, player.pos, 1, true);
    }
    if (CanTargetPlayer()) {
      if (IsInRange(this, player)) {
        return new AttackGroundTask(this, player.pos, 1);
      } else {
        var chase = new ChaseTargetTask(this, player);
        chase.maxMoves = 1;
        return chase;
      }
    } else {
      return new WaitTask(this, 1);
    }
  }

  internal override (int, int) BaseAttackDamage() => (1, 2);
}

// [Serializable]
// [ObjectInfo("octopus-stinger", description: "Can attack at range 2.")]
// internal class ItemOctopusStinger : EquippableItem, IWeapon, ITargetedAction<Body> {
//   public override EquipmentSlot slot => EquipmentSlot.Weapon;
//   public override int stacksMax => int.MaxValue;

//   public (int, int) AttackSpread => (1, 2);

//   string ITargetedAction<Body>.TargettedActionName => "Attack";

//   string ITargetedAction<Body>.TargettedActionDescription => "Choose a target.";

//   void ITargetedAction<Body>.PerformTargettedAction(Player player, Entity target) {
//     player.SetTasks(new AttackGroundTask(player, target.pos, 0));
//     // player.SetTasks(new AttackTask(player, target as Body));
//   }

//   IEnumerable<Body> ITargetedAction<Body>.Targets(Player player) {
//     return player.floor.bodies.Where(b => Octopus.IsInRange(player, b));
//   }
// }