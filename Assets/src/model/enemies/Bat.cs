using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Bat : AIActor {
  public Bat(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 6;
    faction = Faction.Enemy;
    ai = AI().GetEnumerator();
    OnDealAttackDamage += HandleDealDamage;
    OnTakeAttackDamage += HandleTakeDamage;
    if (UnityEngine.Random.value < 0.2f) {
      inventory.AddItem(new ItemBatTooth());
    }
  }

  /// bats hide in corners and occasionally attack the closest target
  private IEnumerable<ActorTask> AI() {
    while (true) {
      for (int i = 0; i < 5; i++) {
        var potentialTargets = actor.floor
          .AdjacentActors(actor.pos)
          .Where((t) => !(t is Bat));
        if (potentialTargets.Any()) {
          var target = Util.RandomPick(potentialTargets);
          yield return new AttackTask(actor, target);
        } else {
          yield return new MoveRandomlyTask(actor);
        }
      }
      yield return new SleepTask(actor, 5, true);
    }
  }

  private void HandleTakeDamage(int dmg, int hp, Actor source) {
    // SetTasks(new AttackTask(this, source));
  }

  private void HandleDealDamage(int dmg, Actor target) {
    if (dmg > 0) {
      Heal(1);
    }
  }

  internal override int BaseAttackDamage() {
    return 1;
  }
}

[ObjectInfo("bat-tooth", "Sharp with a little hole on the end to extract blood.")]
internal class ItemBatTooth : EquippableItem, IWeapon, IDurable {
  public ItemBatTooth() {
    OnEquipped += HandleEquipped;
    OnUnequipped += HandleUnequipped;
  }

  private void HandleEquipped(Player obj) {
    obj.OnDealAttackDamage += HandleDealAttackDamage;
  }

  private void HandleUnequipped(Player obj) {
    obj.OnDealAttackDamage -= HandleDealAttackDamage;
  }

  private void HandleDealAttackDamage(int dmg, Actor target) {
    if (dmg > 0) {
      GameModel.main.player.Heal(1);
    }
  }

  public (int, int) AttackSpread => (3, 3);

  public int durability { get; set; }

  public int maxDurability => 1;
  public override EquipmentSlot slot => EquipmentSlot.Weapon;

  internal override string GetStats() => "Heal 1 HP when you deal damage with this weapon.";
}