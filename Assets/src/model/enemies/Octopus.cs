using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Attacks at range 2 or 3.\nRuns away if you get too close.")]
public class Octopus : AIActor {
  public static Item HomeItem => new ItemOctopusStinger();
  public override float turnPriority => task is AttackGroundTask ? 90 : base.turnPriority;

  public Octopus(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 3;
  }

  public static bool IsInRange(Entity octopus, Entity target) {
    return Util.DiamondMagnitude(target.pos - octopus.pos) <= 3;
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

[Serializable]
[ObjectInfo("octopus-stinger")]
internal class ItemOctopusStinger : EquippableItem, IWeapon, ITargetedAction<Body> {
  public override EquipmentSlot slot => EquipmentSlot.Weapon;

  public (int, int) AttackSpread => (1, 2);

  string ITargetedAction<Body>.TargettedActionName => "Attack";

  string ITargetedAction<Body>.TargettedActionDescription => "Choose a target.";

  void ITargetedAction<Body>.PerformTargettedAction(Player player, Entity target) {
    player.SetTasks(new AttackGroundTask(player, target.pos, 1));
  }

  IEnumerable<Body> ITargetedAction<Body>.Targets(Player player) {
    return player.floor.bodies.Where(b => Octopus.IsInRange(player, b));
  }
}