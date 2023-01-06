using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Any non-player Creature walking into the Evening Bells falls into Deep Sleep for 3 turns. This consumes the Evening Bells.")]
public class EveningBells : Grass, IActorEnterHandler {
  public static Item HomeItem => new ItemEveningPowder();
  public readonly float angle;

  public static bool CanOccupy(Tile tile) => tile is Ground && tile.CanBeOccupied();
  public EveningBells(Vector2Int pos, float angle) : base(pos) {
    this.angle = angle;
  }
  public EveningBells(Vector2Int pos) : this(pos, 0) {}

  public void HandleActorEnter(Actor actor) {
    // if (actor != GameModel.main.player) {
      actor.SetTasks(new SleepTask(actor, 3, true));
      GameModel.main.EnqueueEvent(() => Kill(actor));
    // }
  }
}

[Serializable]
[ObjectInfo("evening-powder", description: "Use to put target adjacent Creature to Deep Sleep for three turns.")]
internal class ItemEveningPowder : Item, ITargetedAction<Actor> {
  public override int stacksMax => int.MaxValue;

  public string TargettedActionName => "Apply";
  public string TargettedActionDescription => "Choose target.";

  public void PerformTargettedAction(Player player, Entity target) {
    var actor = target as Actor;
    actor.SetTasks(new SleepTask(actor, 3, true));
    stacks--;
  }

  public IEnumerable<Actor> Targets(Player player) {
    return player.floor.AdjacentActors(player.pos).Where(a => !(a is Player));
  }
}