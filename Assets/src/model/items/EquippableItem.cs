using System;
using System.Collections.Generic;
using System.Reflection;

[Serializable]
public abstract class EquippableItem : Item {
  public abstract EquipmentSlot slot { get; }
  protected Player player => GameModel.main.player;
  public bool IsEquipped => inventory != null;

  public void Equip(Actor a) {
    var player = (Player) a;
    player.equipment.AddItem(this);
  }

  public void Unequip(Actor a) {
    var player = ((Player)a);
    player.inventory.AddItem(this);
  }

  public virtual void OnEquipped(Player player) {}

  public virtual void OnUnequipped(Player player) {}
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
