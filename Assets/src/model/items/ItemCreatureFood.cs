using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
[ObjectInfo("creature-food", description: "Feed to a Creature at 1 HP to tame them!")]
public class ItemCreatureFood : Item, ITargetedAction<AIActor> {
  public ItemCreatureFood() {
  }

  string ITargetedAction<AIActor>.TargettedActionName => "Feed";
  string ITargetedAction<AIActor>.TargettedActionDescription => "Choose a Creature to Feed.";
  IEnumerable<AIActor> ITargetedAction<AIActor>.Targets(Player player) {
    return player.floor.Enemies().Where(e => e.IsNextTo(player));
  }

  void ITargetedAction<AIActor>.PerformTargettedAction(Player player, Entity entity) {
    var target = entity as AIActor;
    if (target.hp <= 1) {
      // you caught it!
      var slot = inventory.GetSlotFor(this);
      Destroy();
      target.SetAI(new WaitAI(target));
      target.statuses.Add(new CharmedStatus());
      target.faction = Faction.Ally;
      target.floor.CheckTeleporter();

      // we're *not* killing the entity
      // target.floor.Remove(target);
      // var item = new ItemPlaceableEntity(target).RequireSpace();
      // var success = player.inventory.AddItem(item, slot, entity);
      // if (!success) {
      //   player.floor.Put(new ItemOnGround(target.pos, item));
      // }
    } else {
      throw new CannotPerformActionException("Target has too much HP!");
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