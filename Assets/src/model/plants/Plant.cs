using System;
using System.Linq;
using UnityEngine;

[Serializable]
public abstract class Plant : Body, ISteppable, IHideInSidebar {
  public float timeNextAction { get; set; }
  /// put earlier than the player so they can act early
  public float turnPriority => 0;
  [field:NonSerialized] /// controller only
  public event Action OnHarvested;

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
  public ItemFertilizer fertilizer;

  internal bool isMatured => percentGrown >= 1;

  public PlantStage stage {
    get => _stage;
    set {
      _stage = value;
      _stage.BindTo(this);
      /// hack - apply fertilizer here
      if (fertilizer != null) {
        foreach (var inventory in _stage.harvestOptions) {
          foreach (var item in inventory.ItemsNonNull()) {
            if (item is IWeapon w) {
              fertilizer.Imbue(w);
            }
          }
        }
      }
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

  internal void Harvest(int choiceIndex) {
    stage.harvestOptions[choiceIndex].TryDropAllItems(floor, pos);
    OnHarvested?.Invoke();
    Kill(GameModel.main.player);
  }
}
