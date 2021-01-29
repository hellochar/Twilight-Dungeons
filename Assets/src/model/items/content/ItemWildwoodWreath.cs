using System;
using System.Linq;

public class ItemWildwoodWreath : EquippableItem, IDurable, IAttackHandler {
  public ItemWildwoodWreath() {
    durability = maxDurability;
  }

  public override EquipmentSlot slot => EquipmentSlot.Head;
  public int durability { get; set; }
  public int maxDurability => 15;

  public void OnAttack(int damage, Body target) {
    player.statuses.Add(new StatusWild(2));
    this.ReduceDurability();
  }

  internal override string GetStats() => "Attacking an enemy gives you the Wild status for 2 turns.";
}
