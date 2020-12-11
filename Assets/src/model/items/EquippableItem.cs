using System.Collections.Generic;

public abstract class EquippableItem : Item {
  public abstract EquipmentSlot slot { get; }

  public void Equip(Actor a) {
    ((Player)a).equipment.AddItem(this);
  }

  public void Unequip(Actor a) {
    ((Player)a).inventory.AddItem(this);
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
