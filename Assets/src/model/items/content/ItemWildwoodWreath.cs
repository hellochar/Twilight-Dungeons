using System;
using System.Linq;

public class ItemWildwoodWreath : EquippableItem, IDurable, IActionPerformedHandler {
  public ItemWildwoodWreath() {
    durability = 8;
  }

  public override EquipmentSlot slot => EquipmentSlot.Head;
  public int durability { get; set; }
  public int maxDurability => 8;

  internal override string GetStats() => "Whenever you move, deal 1 damage to all adjacent enemies.";

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (final.Type == ActionType.MOVE) {
      var actor = final.actor;

      var adjacentEnemies = actor.floor
        .AdjacentActors(actor.pos)
        .Where((a) => a.faction == Faction.Enemy);

      if (adjacentEnemies.Any()) {
        foreach (var enemy in adjacentEnemies) {
          enemy.TakeDamage(1, final.actor);
        }
        this.ReduceDurability();
      }
    }
  }
}

interface IActionPerformedHandler {
  void HandleActionPerformed(BaseAction final, BaseAction initial);
}