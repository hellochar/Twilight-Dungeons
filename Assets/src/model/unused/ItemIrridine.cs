using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class PushPlayerGrass : Grass, ISteppable {
  public PushPlayerGrass(Vector2Int pos) : base(pos) {
  }

  public float timeNextAction { get; set; }

  public float turnPriority => 11;

  public float Step() {
    var walls = floor.GetCardinalNeighbors(pos).Where(t => t is Wall);
    foreach (var w in walls) {
      var offset = (w.pos - pos) * -1;
      var body1 = floor.bodies[pos + offset];
      if (body1 is AIActor a) {
        a.pos += offset;
        a.statuses.Add(new SurprisedStatus());
        OnNoteworthyAction();
      }
      var body2 = floor.bodies[pos];
      if (body2 is AIActor a2) {
        a2.pos += offset;
        a2.statuses.Add(new SurprisedStatus());
        OnNoteworthyAction();
      }
    }
    return 1;
  }
}

[Serializable]
[ObjectInfo("irridine")]
public class ItemIrridine : Item, ITargetedAction<Tile> {
  public override int stacksMax => 999;

  public ItemIrridine(int stacks) : base() {
    this.stacks = stacks;
  }

  internal override string GetStats() => "Plant along the Walls in your home base.";

  public void Transplant(Tile tile) {
    // if (tile.floor.depth != 0) {
    //   throw new CannotPerformActionException("Plant at home!");
    // }
    if (tile.grass != null) {
      throw new CannotPerformActionException("Plant on empty tile!");
    }
    foreach (var t in tile.floor.BreadthFirstSearch(tile.pos, VibrantIvy.CanOccupy, mooreNeighborhood: true).Take(stacks).ToList()) {
      tile.floor.Put(new VibrantIvy(t.pos));
      stacks--;
    }
  }

  string ITargetedAction<Tile>.TargettedActionName => "Transplant";
  IEnumerable<Tile> ITargetedAction<Tile>.Targets(Player player) =>
    // player.floor.depth != 0 ?
    // Enumerable.Empty<Ground>() :
    player.floor.tiles.Where(VibrantIvy.CanOccupy);//.Cast<Ground>();

  void ITargetedAction<Tile>.PerformTargettedAction(Player player, Entity target) => Transplant(target as Tile);
}
