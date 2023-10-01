using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

[System.AttributeUsage(System.AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
sealed public class PlantConfigAttribute : System.Attribute {
  public PlantConfigAttribute() {}
  
  public int WaterCost { get; set; }
  public int FloorsToMature { get; set; }
}

[Serializable]
[PlantConfig(FloorsToMature = 4, WaterCost = 100)]
public abstract class Plant : Body, IHideInSidebar, IAnyDamageTakenModifier {
  public PlantConfigAttribute Config => GetType().GetCustomAttribute<PlantConfigAttribute>();

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

  public Plant(Vector2Int pos) : base(pos) {
    this.stage = new Seed(Config.FloorsToMature);
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
    Kill(GameModel.main.player);
  }

  internal void OnFloorCleared(Floor floor) {
    stage.xp++;
  }

  public int Modify(int input) {
    return 0;
  }
}
