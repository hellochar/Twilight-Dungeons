using System;
using System.Linq;
using UnityEngine;

public abstract class Plant : Actor {
  private int m_water = 2;
  public int water {
    get => m_water;
    set {
      m_water = Mathf.Clamp(value, 0, maxWater);
      if (water > 0) {
        timeNextAction = GameModel.main.time + stage.StepTime;
      }
    }
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
    OnDeath += HandleDeath;
    // AddTimedEvent(5, StepWater);
  }

  // private void StepWater() {
  //   if (water <= 0) {
  //     Kill();
  //   } else {
  //     water--;
  //   }
  //   AddTimedEvent(5, StepWater);
  // }

  protected override float Step() {
    if (water <= 0) {
      return 99999;
    }
    water--;
    var stageBefore = stage;
    stage.Step();
    return stage.StepTime;
  }

  public void GoNextStage() {
    if (stage.NextStage != null) {
      stage = stage.NextStage;
      timeNextAction = age + stage.StepTime;
    }
  }

  internal string GetUIText() {
    if (water > 0) {
      return stage.getUIText();
    } else {
      return "This plant needs water to keep growing!";
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
    Kill();
  }

  private void HandleDeath() {
    CullRewards()?.DropRandomlyOntoFloorAround(floor, base.pos);
    Harvest();
  }

  public abstract Inventory HarvestRewards();

  public abstract Inventory CullRewards();
}
