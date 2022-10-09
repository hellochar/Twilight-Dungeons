using System;
using System.Collections.Generic;

[Serializable]
[ObjectInfo("creature-food", description: "Your item has been overtaken by mold!")]
public class ItemCreatureFood : Item, ITargetedAction<AIActor> {
  public ItemCreatureFood() {
  }

  string ITargetedAction<AIActor>.TargettedActionName => "Feed";
  IEnumerable<AIActor> ITargetedAction<AIActor>.Targets(Player player) {
    return player.floor.Enemies();
  }

  void ITargetedAction<AIActor>.PerformTargettedAction(Player player, Entity entity) {
    var target = entity as AIActor;
    if (target.hp <= 1) {
      // you caught it!
      var slot = inventory.GetSlotFor(this);
      Destroy();
      target.SetAI(new WaitAI(target));
      // target.statuses.Add(new CharmedStatus());
      target.faction = Faction.Ally;
      // we're *not* killing the entity
      target.floor.Remove(target);
      var item = new ItemPlaceableEntity(target).RequireSpace();
      var success = player.inventory.AddItem(item, slot, entity);
      if (!success) {
        player.floor.Put(new ItemOnGround(target.pos, item));
      }
    }
  }
}

[Serializable]
internal class WaitAI : AI {
  public AIActor actor;

  public WaitAI(AIActor actor) {
    this.actor = actor;
  }

  public override ActorTask GetNextTask() {
    return new WaitTask(actor, 1);
  }
}