using System;
using System.Linq;
using UnityEngine;

[Serializable]
public abstract class Plant : Body, IHideInSidebar {
  [field:NonSerialized] /// controller only
  public event Action OnHarvested;

  public float percentGrown {
    get {
      if (stage.NextStage == null) {
        return 1;
      } else {
        return stage.percentGrown;
      }
    }
  }

  private PlantStage _stage;
  public PlantStage stage {
    get => _stage;
    set {
      _stage = value;
      _stage.BindTo(this);
    }
  }
  public override string displayName => $"{base.displayName}{ (stage.NextStage == null ? "" : " (" + stage.name + ")") }";


  public Plant(Vector2Int pos, PlantStage stage) : base(pos) {
    this.stage = stage;
    this.hp = this.baseMaxHp = 1;
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

  internal void OnFloorCleared(Floor floor) {
    stage.xp++;
  }
}
