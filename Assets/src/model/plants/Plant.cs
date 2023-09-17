using System;
using System.Linq;
using UnityEngine;

[Serializable]
public abstract class Plant : Body, IHideInSidebar, IAnyDamageTakenModifier {
  [field:NonSerialized] /// controller only
  public event Action OnHarvested;

  public float percentGrown {
    get {
      if (IsMature) {
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
  public override string displayName => $"{base.displayName}";

  public bool IsMature => stage.NextStage == null;

  public Plant(Vector2Int pos, PlantStage stage) : base(pos) {
    this.stage = stage;
    this.hp = this.baseMaxHp = 999;
  }

  public void GoNextStage() {
    if (!IsMature) {
      stage = stage.NextStage;
    }
  }

  internal void Harvest(int choiceIndex) {
    var floor = this.floor;
    var harvest = stage.harvestOptions[choiceIndex];
    Kill(GameModel.main.player);
    // // autoplant seed
    // var itemSeed = harvest.ItemsNonNull().OfType<ItemSeed>().FirstOrDefault();
    // if (itemSeed != null) {
    //   var constructor = GetType().GetConstructor(new Type[1] { typeof(Vector2Int) });
    //   var plant = (Plant) constructor.Invoke(new object[] { pos });
    //   floor.Put(plant);
    //   itemSeed.stacks--;
    //   if (itemSeed.stacks == 0) {
    //     harvest.RemoveItem(itemSeed);
    //   }
    // }
    harvest.TryDropAllItems(floor, pos);
    OnHarvested?.Invoke();
  }

  internal void OnFloorCleared(Floor floor) {
    stage.xp++;
  }

  public int Modify(int input) {
    return 0;
  }
}
