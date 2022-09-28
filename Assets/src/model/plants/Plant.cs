using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public abstract class Plant : Body, IHideInSidebar, IDaySteppable {
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

  public void StepDay() {
    if (stage.NextStage != null) {
      stage.StepDay();
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
      /// hack - apply soil improvements here
      if (IsSurroundedByGrass()) {
        var existingOptions = new List<Inventory>(_stage.harvestOptions);
        _stage.harvestOptions.Clear();
        _stage.plant = null;
        _stage.BindTo(this);
        for (int i = 0; i < _stage.harvestOptions.Count; i++) {
          foreach (var item in existingOptions[i]) {
            _stage.harvestOptions[i].AddItem(item, null, true);
          }
        }
      }

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
    }
  }

  public override string displayName => $"{base.displayName}{ (stage.NextStage == null ? "" : " (" + stage.name + ")") }{ (IsSurroundedByGrass() ? " 2x" : "") }";


  public Plant(Vector2Int pos, PlantStage stage) : base(pos) {
    this.stage = stage;
    this.hp = this.baseMaxHp = 1;
  }

  public bool IsSurroundedByGrass() {
    return floor == null ? false : floor.GetAdjacentTiles(pos).Where(t => t.grass != null).Count() >= 8;
  }

  public void GoNextStage() {
    if (stage.NextStage != null) {
      stage = stage.NextStage;
    }
  }

  internal void Harvest(int choiceIndex) {
    var player = GameModel.main.player;
#if experimental_actionpoints
    var isFreeHarvest = floor.depth > 0;
    if (!isFreeHarvest) {
      player.UseActionPointOrThrow();
    }
#endif
    stage.harvestOptions[choiceIndex].TryDropAllItems(floor, pos);
    OnHarvested?.Invoke();
    Kill(player);
  }
}
