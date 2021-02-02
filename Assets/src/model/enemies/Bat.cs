using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Chases and attacks nearest creature.\nLifesteals.\nGoes into Deep Sleep after 7 turns awake.\nOccasionally drops Bat Tooth.")]
public class Bat : AIActor, IActionPerformedHandler, IDealAttackDamageHandler {
  public Bat(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 7;
    ClearTasks();
    faction = Faction.Enemy;
    if (UnityEngine.Random.value < 0.2f) {
      inventory.AddItem(new ItemBatTooth());
    }
  }

  int turnsUntilSleep = 7;
  public void HandleActionPerformed(BaseAction arg1, BaseAction arg2) {
    if (!(task is SleepTask)) {
      turnsUntilSleep--;
      if (turnsUntilSleep <= 0) {
        var sleep = new SleepTask(this, 5, true);
        SetTasks(sleep);
      }
    }
  }

  protected override void TaskChanged() {
    if (task is SleepTask) {
      turnsUntilSleep = 7;
    }
    base.TaskChanged();
  }

  /// bats hide in corners and occasionally attack the closest target
  protected override ActorTask GetNextTask() {
    var target = SelectTarget();
    if (target == null) {
      return new MoveRandomlyTask(this);
    }
    if (IsNextTo(target)) {
      return new AttackTask(this, target);
    }
    // chase until you are next to any target
    return new ChaseDynamicTargetTask(this, SelectTarget);
  }

  Actor SelectTarget() {
    var potentialTargets = floor
      .ActorsInCircle(pos, 7)
      .Where((t) => floor.TestVisibility(pos, t.pos) && !(t is Bat));
    if (potentialTargets.Any()) {
      return potentialTargets.Aggregate((t1, t2) => DistanceTo(t1) < DistanceTo(t2) ? t1 : t2);
    }
    return null;
  }

  public void HandleDealAttackDamage(int dmg, Body target) {
    if (target is Actor && dmg > 0) {
      Heal(1);
    }
  }

  internal override (int, int) BaseAttackDamage() {
    return (1, 1);
  }
}

[Serializable]
[ObjectInfo("bat-tooth", "Sharp with a little hole on the end to extract blood.")]
internal class ItemBatTooth : EquippableItem, IWeapon, IDealAttackDamageHandler {
  public ItemBatTooth() {
    durability = maxDurability;
  }

  public void HandleDealAttackDamage(int dmg, Body target) {
    if (target is Actor && dmg > 0) {
      GameModel.main.player.Heal(1);
    }
    Destroy();
  }

  public (int, int) AttackSpread => (1, 1);

  public int durability { get; set; }

  public int maxDurability => 1;
  public override EquipmentSlot slot => EquipmentSlot.Weapon;

  internal override string GetStats() => "Heal 1 HP when this deals damage.\nConsumed on use.";
}