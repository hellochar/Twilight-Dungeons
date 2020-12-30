using System;
using System.Collections.Generic;
using System.Reflection;

public abstract class EquippableItem : Item {
  public abstract EquipmentSlot slot { get; }
  public event Action<Player> OnEquipped;
  public event Action<Player> OnUnequipped;

  public void Equip(Actor a) {
    var player = (Player) a;
    player.equipment.AddItem(this);
  }

  public void Unequip(Actor a) {
    var player = ((Player)a);
    player.inventory.AddItem(this);
  }

  public void TriggerEquipped(Player player) {
    OnEquipped?.Invoke(player);
  }

  public void TriggerUnequipped(Player player) {
    OnUnequipped?.Invoke(player);
  }

  public override List<MethodInfo> GetAvailableMethods(Player actor) {
    var methods = base.GetAvailableMethods(actor);
    if (actor.inventory.HasItem(this)) {
      methods.Add(GetType().GetMethod("Equip"));
    } else if (actor.equipment.HasItem(this)) {
      methods.Add(GetType().GetMethod("Unequip"));
    }
    return methods;
  }
}
