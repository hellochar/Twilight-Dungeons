using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ObjectInfo(description: "Chases and attacks nearest creature.\nLifesteals.\nGoes into Deep Sleep after 7 turns awake.\nOccasionally drops Bat Tooth.")]
public class Bat : AIActor {
  public Bat(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 7;
    ClearTasks();
    faction = Faction.Enemy;
    ai = AI().GetEnumerator();
    OnDealAttackDamage += HandleDealDamage;
    OnActionPerformed += HandleActionPerformed;
    if (UnityEngine.Random.value < 0.2f) {
      inventory.AddItem(new ItemBatTooth());
    }
  }

  int turnsUntilSleep = 7;
  private void HandleActionPerformed(BaseAction arg1, BaseAction arg2) {
    if (!(task is SleepTask)) {
      turnsUntilSleep--;
      if (turnsUntilSleep <= 0) {
        var sleep = new SleepTask(this, 5, true);
        SetTasks(sleep);
      }
    } else {
      turnsUntilSleep = 7;
    }
  }

  /// bats hide in corners and occasionally attack the closest target
  private IEnumerable<ActorTask> AI() {
    while (true) {
      var target = SelectTarget();
      if (target == null) {
        yield return new MoveRandomlyTask(this);
        continue;
      }
      if (IsNextTo(target)) {
        yield return new AttackTask(this, target);
        continue;
      }
      // chase until you are next to any target
      yield return new ChaseDynamicTargetTask(this, SelectTarget);
    }
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

  private void HandleDealDamage(int dmg, Body target) {
    if (target is Actor && dmg > 0) {
      Heal(1);
    }
  }

  internal override (int, int) BaseAttackDamage() {
    return (1, 1);
  }
}

[ObjectInfo("bat-tooth", "Sharp with a little hole on the end to extract blood.")]
internal class ItemBatTooth : EquippableItem, IWeapon {
  public ItemBatTooth() {
    durability = maxDurability;
  }

  public override void OnEquipped(Player obj) {
    obj.OnDealAttackDamage += HandleDealAttackDamage;
  }

  public override void OnUnequipped(Player obj) {
    obj.OnDealAttackDamage -= HandleDealAttackDamage;
  }

  private void HandleDealAttackDamage(int dmg, Body target) {
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