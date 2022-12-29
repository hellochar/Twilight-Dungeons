using System.Collections.Generic;
using System.Linq;

class CaptureAction : ITargetedAction<Entity> {
  public string TargettedActionName => "Keep";
  public string TargettedActionDescription => "Keep a Grass or Creature!";

  public void PerformTargettedAction(Player player, Entity target) {
    Item item = null;
    if (target is Grass grass) {
      item = new ItemGrass(grass.GetType());
    } else if (target is AIActor actor) {
      item = new ItemPlaceableEntity(target);
    }

    if (item != null) {
      // place it at home
      var dropPos = GameModel.main.home.soils.First().pos;
      GameModel.main.home.Put(new ItemOnGround(dropPos, item));

      // var success = player.inventory.AddItem(item, target);
      // if (!success) {
      //   player.floor.Put(new ItemOnGround(target.pos, item));
      // }
      // they're *not* killed because we don't want to trigger actions on them
      target.floor.Remove(target);
    }
  }

  public IEnumerable<Entity> Targets(Player player) {
    var floor = player.floor;
    return floor
      .grasses
      .Cast<Entity>()
      .Concat(player.floor.bodies.OfType<AIActor>())
      .Where(e => e.room == player.room)
      .Where(e => e.GetHomeItem() != null);
  }
}
