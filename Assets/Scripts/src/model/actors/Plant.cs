using System;
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
  }

  protected override float Step() {
    return this.stage.Step();
  }

  public abstract void Harvest(); 

  public abstract void Cull();
}
