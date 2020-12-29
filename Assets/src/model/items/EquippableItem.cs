using System;
using System.Collections.Generic;

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

  public override List<ActorTask> GetAvailableTasks(Player actor) {
    var actions = base.GetAvailableTasks(actor);
    if (actor.inventory.HasItem(this)) {
      actions.Add(new GenericTask(actor, Equip));
    } else if (actor.equipment.HasItem(this)) {
      actions.Add(new GenericTask(actor, Unequip));
    }
    return actions;
  }
}
