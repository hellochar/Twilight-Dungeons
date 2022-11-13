using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SingleItemPlant {

  [Serializable]
  public class SingleItemPlant : Plant {
    public static void AddSingleItemPlantSeedToRoom(Floor floor, Room room, Type itemType) {
      Tile tile = Util.RandomPick(floor.EnumerateRoomTiles(room).Where(t => t is Soil && t.CanBeOccupied()));
      if (tile == null) {
        tile = FloorUtils.TilesFromCenter(floor, room).Where(t => t.CanBeOccupied()).FirstOrDefault();
      }
      if (tile != null) {
        var plant = new SingleItemPlant(tile.pos, itemType);
        plant.GoNextStage();
        floor.Put(plant);
        // var item = new ItemSingleItemPlantSeed(itemType);
        // floor.Put(new ItemOnGround(tile.pos, item));
      }
    }

    [Serializable]
    class Mature : PlantStage {
      public override void BindTo(Plant plant) {
        SingleItemPlant ip = (SingleItemPlant)plant;
        System.Type itemType = ip.ItemType;
        var constructor = itemType.GetConstructor(new Type[] { });
        Item item;
        if (constructor != null) {
          item = (Item)constructor.Invoke(new object[0]);
          harvestOptions.Add(new Inventory(item));
          harvestOptions.Add(new Inventory(new ItemSingleItemPlantSeed(itemType), new ItemSingleItemPlantSeed(itemType)));
        }
        base.BindTo(plant);
      }
    }

    public System.Type ItemType;
    public SingleItemPlant(Vector2Int pos, System.Type ItemType) : base(pos, new Seed()) {
      this.ItemType = ItemType;
      stage.NextStage = new Mature();
    }
  }

  [Serializable]
  [ObjectInfo("roguelikeSheet_transparent_532")]
  public class ItemSingleItemPlantSeed : Item, ITargetedAction<Soil> {
    public System.Type itemType;

    public ItemSingleItemPlantSeed(Type itemType) {
      this.itemType = itemType;
    }

    internal override string GetStats() {
      var itemName = Util.WithSpaces(itemType.Name.Substring(4));
      return $"Plant on a soil (100 water) to grow.\nTurns into a {itemName}.";
    }

    public override string displayName => $"{Util.WithSpaces(itemType.Name.Substring(4))} Seed";

    public string TargettedActionName => "Plant";
    public string TargettedActionDescription => "Plant";
    public IEnumerable<Soil> Targets(Player player) => player.floor.tiles.Where(tile => tile is Soil && tile.isExplored && tile.CanBeOccupied()).Cast<Soil>();
    public void PerformTargettedAction(Player player, Entity target) => MoveAndPlant((Soil)target);

    public void MoveAndPlant(Soil soil) {
      var model = GameModel.main;
      Player player = model.player;
      if (model.currentFloor.depth != 0) {
        throw new CannotPerformActionException("Plant on the home floor.");
      }
      player.SetTasks(
      new MoveNextToTargetTask(player, soil.pos),
      new GenericPlayerTask(player, () => {
        if (player.IsNextTo(soil)) {
          Plant(soil);
        }
      })
      );
    }

    int waterCost = 45;

    private void Plant(Soil soil) {
      var player = GameModel.main.player;
      if (player.water >= waterCost) {
        player.water -= waterCost;
        var plant = new SingleItemPlant(soil.pos, itemType);
        soil.floor.Put(plant);
        GameModel.main.stats.plantsPlanted++;
        Destroy();
      } else {
        throw new CannotPerformActionException($"Need <color=lightblue>{waterCost}</color> water!");
      }
    }
  }

  [Serializable]
  public class CustomEncounterGroupShared : EncounterGroupShared {
    public CustomEncounterGroupShared() : base() {
      var allItemTypes = AppDomain.CurrentDomain.GetAssemblies()
      .SelectMany(a => a.GetTypes())
      .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Item)) && t.GetConstructor(new Type[0]) != null);
      Plants.Clear();
      // Rewards.Clear();

      foreach (var type in allItemTypes) {
        Plants.Add(1, (Floor floor, Room room) => {
          SingleItemPlant.AddSingleItemPlantSeedToRoom(floor, room, type);
        });
        Rewards.Add(1, (Floor floor, Room room) => {
          SingleItemPlant.AddSingleItemPlantSeedToRoom(floor, room, type);
        });
        Debug.Log(type.Name);
      }
    }
  }
}
