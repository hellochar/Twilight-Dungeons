using System;
using UnityEngine;

public class Actor : Entity {
  private Vector2Int _pos;
  public virtual Vector2Int pos {
    get => _pos;
    set {
      Floor floor = this.floor;
      if (floor == null || floor.tiles[value.x, value.y].CanBeOccupied()) {
        _pos = value;
      }
    }
  }
  public int baseActionCost { get => 1; }
  public int timeCreated { get; }
  /// how many turns this Entity has been alive for
  /// this has a bug with CatchUpStep - age will jump
  public int age { get => GameModel.main.time - timeCreated; }
  public int timeNextAction;
  public virtual ActorAction action { get; set; }
  public int visibilityRange = 7;
  public Floor floor;
  public Tile currentTile => floor.tiles[pos.x, pos.y];
  public bool visible => currentTile.visiblity == TileVisiblity.Visible;

  /// This number allows tweaking of Actor order when they would otherwise be scheduled
  /// at the same time. This offset gets added to the timeNextAction, so higher numbers
  /// will come after lower numbers. This does *NOT* actually modify "time" which the
  /// actor takes the action. Player has offset 0 (aka always goes first). This number should
  /// be < 1.
  internal virtual float queueOrderOffset { get => 0.5f; }

  public Actor(Vector2Int pos) {
    this.timeCreated = GameModel.main.time;
    this.timeNextAction = this.timeCreated;
    this.pos = pos;
  }

  public virtual void Step() {
    if (action == null) {
      this.timeNextAction += baseActionCost;
    } else {
      if (action.IsDone()) {
        action.Finish();
        this.action = null;
        this.timeNextAction += baseActionCost;
      } else {
        int timeCost = action.Perform();
        this.timeNextAction += timeCost;
        if (action.IsDone()) {
          action.Finish();
          this.action = null;
        }
      }
    }
  }

  public bool IsNextTo(Entity other) {
    return Math.Abs(pos.x - other.pos.x) <= 1 && Math.Abs(pos.y - other.pos.y) <= 1;
  }

  public virtual void CatchUpStep(int newTime) {
    // by default actors don't do anything; they just act as if they were paused
    this.timeNextAction = newTime;
  }
}
