using System;
using System.Linq;
using UnityEngine;

[Serializable]
public abstract class Plant : Body, ISteppable {
  public float timeNextAction { get; set; }
  /// put earlier than the player so they can act early
  public float turnPriority => 0;

  public float percentGrown {
    get {
      if (stage.NextStage == null) {
        return 1;
      } else {
        return stage.age / stage.StepTime;
      }
    }
  }

  private PlantStage _stage;
  public PlantStage stage {
    get => _stage;
    set {
      _stage = value;
      _stage.BindTo(this);
      timeNextAction = GameModel.main.time + _stage.StepTime;
    }
  }
  public override string displayName => $"{base.displayName}{ (stage.NextStage == null ? "" : " (" + stage.name + ")") }";


  public Plant(Vector2Int pos, PlantStage stage) : base(pos) {
    this.stage = stage;
    this.hp = this.baseMaxHp = 1;
    this.timeNextAction = this.timeCreated + stage.StepTime;
  }

  public float Step() {
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
    return uiText;
  }

  internal void Harvest(int choiceIndex) {
    stage.harvestOptions[choiceIndex].TryDropAllItems(floor, pos);
    Kill(GameModel.main.player);
  }
}
