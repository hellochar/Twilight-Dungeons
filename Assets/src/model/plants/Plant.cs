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

  protected virtual bool isFreeHarvest => floor.depth > 0;
  internal void Harvest(int choiceIndex) {
    var player = GameModel.main.player;
#if experimental_actionpoints
    if (!isFreeHarvest) {
      player.UseActionPointOrThrow();
    }
#endif
    var floor = this.floor;
    var harvest = stage.harvestOptions[choiceIndex];
    OnHarvested?.Invoke();
    Kill(player);
    // autoplant seed
    var itemSeed = harvest.ItemsNonNull().OfType<ItemSeed>().FirstOrDefault();
    if (itemSeed != null) {
      var constructor = GetType().GetConstructor(new Type[1] { typeof(Vector2Int) });
      var plant = (Plant) constructor.Invoke(new object[] { pos });
      floor.Put(plant);
      harvest.TryDropAllItems(floor, pos);
      itemSeed.stacks--;
    } else {
      harvest.TryDropAllItems(floor, pos);
    }
  }
}
