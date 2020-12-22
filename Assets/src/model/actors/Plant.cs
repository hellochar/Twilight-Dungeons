using System;
using System.Linq;
using UnityEngine;

public abstract class Plant : Actor {
  private int m_water = 2;
  public int water {
    get => m_water;
    set => m_water = Mathf.Clamp(value, 0, maxWater);
  }

  public abstract int maxWater { get; }
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
    AddTimedEvent(500, StepWater);
  }

  private void StepWater() {
    water--;
    AddTimedEvent(500, StepWater);
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
    HarvestRewards()?.DropRandomlyOntoFloorAround(floor, base.pos);
  }

  public virtual void Cull() {
    CullRewards()?.DropRandomlyOntoFloorAround(floor, base.pos);
    Harvest();
    Kill();
  }

  public abstract Inventory HarvestRewards();

  public abstract Inventory CullRewards();
}
