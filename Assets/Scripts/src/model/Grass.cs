using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Grass : SteppableEntity {
  private Vector2Int _pos;
  public override Vector2Int pos {
    get => _pos;
    /// do not allow moving grass
    set { }
  }

  public Grass(Vector2Int pos) : base() {
    this._pos = pos;
  }
}

public class SoftGrass : Grass {
  public SoftGrass(Vector2Int pos) : base(pos) {
    OnEnterFloor += HandleEnterFloor;
    /// TODO make SteppableEntity an Interface
    timeNextAction = this.timeCreated + 99999;
  }

  void HandleEnterFloor() {
    /// TODO make this declarative instead of manually registering events
    tile.OnActorEnter += HandleActorEnter;
    tile.OnActorLeave += HandleActorLeave;
  }

  void HandleActorEnter(Actor who) {
    if (who is Player player) {
      player.statuses.Add(new SoftGrassStatus());
    }
  }

  void HandleActorLeave(Actor who) {
    who.statuses.RemoveOfType<SoftGrassStatus>();
  }


  protected override float Step() {
    return 99999;
  }

  public override void Kill() {
    tile.OnActorEnter -= HandleActorEnter;
    if (tile.actor != null) {
      HandleActorLeave(tile.actor);
    }
    tile.OnActorLeave -= HandleActorLeave;
    OnEnterFloor -= HandleEnterFloor;
    base.Kill();
  }
}

public class Redvines : Grass {
  public Redvines(Vector2Int pos) : base(pos) {
    OnEnterFloor += HandleEnterFloor;
    timeNextAction = this.timeCreated + 99999;
  }

  protected override float Step() {
    return 99999;
  }

  void HandleEnterFloor() {
    tile.OnActorEnter += HandleActorEnter;
  }

  private void HandleActorEnter(Actor who) {
    // stun them for a few turns
    GrappledTask action = new GrappledTask(who, 3, this);
    action.OnDone += HandleActionDone;
    who.InsertTasks(action);
  }

  private void HandleActionDone() {
    if (!IsDead) {
      Kill();
    }
  }

  public override void Kill() {
    /// TODO oh shit you have to manually unregister all your events
    tile.OnActorEnter -= HandleActorEnter;
    base.Kill();
  }
}

public class GrappledTask : ActorTask {
  private readonly int turns;
  public Entity grappler { get; }
  public GrappledTask(Actor actor, int turns, Entity grappler) : base(actor) {
    this.turns = turns;
    this.grappler = grappler;
  }

  public override IEnumerator<BaseAction> Enumerator() {
    for (var i = 0; i < turns; i++) {
      if (grappler.IsDead) {
        yield break;
      }
      yield return new WaitBaseAction(actor);
    }
  }
}

public class Mushroom : Grass {
  public Mushroom(Vector2Int pos) : base(pos) {
    timeNextAction = this.timeCreated + GetRandomDuplicateTime();
    OnEnterFloor += HandleEnterFloor;
  }

  void HandleEnterFloor() {
    tile.OnActorEnter += HandleActorEnter;
  }

  private void HandleActorEnter(Actor actor) {
    if (actor == GameModel.main.player) {
      GameModel.main.player.inventory.AddItem(new ItemMushroom(1));
      Kill();
    }
  }

  private float GetRandomDuplicateTime() {
    return UnityEngine.Random.Range(50, 100);
  }

  public override void Kill() {
    base.Kill();
  }

  protected override float Step() {
    // find an adjacent square without mushrooms and grow into it
    var noGrassTiles = floor.GetAdjacentTiles(pos).Where((tile) => tile is Ground && tile.grass == null);
    if (noGrassTiles.Any()) {
      var toGrowInto = Util.RandomPick(noGrassTiles);
      var newMushrom = new Mushroom(toGrowInto.pos);
      floor.Add(newMushrom);
    }
    return GetRandomDuplicateTime();
  }
}