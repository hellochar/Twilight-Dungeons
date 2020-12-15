using System;
using System.Linq;
using UnityEngine;

public abstract class Plant : Actor {
  private PlantStage _stage;
  public PlantStage stage {
    get => _stage;
    set {
      _stage = value;
      _stage.BindTo(this);
    }
  }
  /// put earlier than the player so they can act early
  internal override float turnPriority => 0;

  public string displayName => $"{Util.WithSpaces(GetType().Name)} ({stage.name})";

  public Plant(Vector2Int pos, PlantStage stage) : base(pos) {
    faction = Faction.Ally;
    this.stage = stage;
    this.timeNextAction = this.timeCreated + stage.StepTime;
  }

  protected override float Step() {
    var stageBefore = stage;
    var timeDelta = stage.Step();
    if (stage != stageBefore) {
      return stage.StepTime;
    }
    return timeDelta;
  }

  public void GoNextStage() {
    if (stage.NextStage != null) {
      stage = stage.NextStage;
    }
  }

  public override void CatchUpStep(float lastStepTime, float time) {
    // don't catchup; plants always run.
    return;
  }

  public virtual void Harvest() {
    var items = HarvestRewards();
    if (items != null) {
      // it pops out randomly adjacent
      foreach (var item in items) {
        var itemTile = Util.RandomPick(floor.GetAdjacentTiles(pos).Where((tile) => tile.item == null && tile.actor == null));
        var itemOnGround = new ItemOnGround(itemTile.pos, item);
        floor.Put(itemOnGround);
      }
    }
  }

  public virtual void Cull() {
    var items = CullRewards();
    if (items != null) {
      // it pops out randomly adjacent
      foreach (var item in items) {
        var itemTile = Util.RandomPick(floor.GetAdjacentTiles(pos).Where((tile) => tile.item == null && tile.actor == null));
        var itemOnGround = new ItemOnGround(itemTile.pos, item);
        floor.Put(itemOnGround);
      }
    }
    Harvest();
    Kill();
  }

  public abstract Item[] HarvestRewards();

  public abstract Item[] CullRewards();
}
