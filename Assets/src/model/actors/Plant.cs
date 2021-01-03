using System;
using System.Linq;
using UnityEngine;

public abstract class Plant : Actor {
  public Inventory inventory = new Inventory(12);
  private int m_water = 2;
  public int water {
    get => m_water;
    set {
      m_water = Mathf.Clamp(value, 0, maxWater);
      if (water > 0) {
        timeNextAction = Mathf.Min(timeNextAction, GameModel.main.time + stage.StepTime);
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
      timeNextAction = GameModel.main.time + _stage.StepTime;
    }
  }
  /// put earlier than the player so they can act early
  internal override float turnPriority => 0;

  public string displayName => $"{Util.WithSpaces(GetType().Name)} ({stage.name})";

  public Plant(Vector2Int pos, PlantStage stage) : base(pos) {
    faction = Faction.Ally;
    inventory.AddItem(new ItemSeed(this.GetType()));
    this.stage = stage;
    this.timeNextAction = this.timeCreated + stage.StepTime;
    OnDeath += HandleDeath;
  }

  protected override float Step() {
    if (water <= 0) {
      return 99999;
    }
    water--;
    var stageBefore = stage;
    stage.Step();
    if (stageBefore == stage) {
      return stage.StepTime;
    } else {
      // stage has changed; timeNextAction was already set by setting the stage.
      return 0;
    }
  }

  public void GoNextStage() {
    if (stage.NextStage != null) {
      stage = stage.NextStage;
    }
  }

  internal string GetUIText() {
    var uiText = stage.getUIText();
    if (water > 0) {
      return uiText;
    } else {
      return uiText + "\nNeeds water to keep growing!";
    }
  }

  public override void CatchUpStep(float lastStepTime, float time) {
    // don't catchup; plants always run.
    return;
  }

  private void HandleDeath() {
    inventory.TryDropAllItems(floor, pos);
  }

  internal void Harvest() {
    // just a pseudonym
    Kill();
  }
}
