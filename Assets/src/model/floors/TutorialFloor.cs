using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct TutorialFloorInfo {
  public string name;

  public readonly int index {
    get {
      for(int i = 0; i < TutorialFloor.TUTORIAL_FLOORS.Length; i++)
      {
        if (TutorialFloor.TUTORIAL_FLOORS[i].name == name) {
          return i;
        }
      }
      return -1;
    }
  }

  // what new HUD elements this floor introduces
  public Func<GameObject[]> newHUDObjects;
}

[Serializable]
public class TutorialFloor : Floor {
  public static GameObject[] STARTING_HUD => new GameObject[] { HUDController.main.depth, HUDController.main.waitButton };

  public static TutorialFloorInfo[] TUTORIAL_FLOORS = {
    new TutorialFloorInfo {
      name = "T_Room1",
      newHUDObjects = () => new GameObject[] {},
    },
    new TutorialFloorInfo {
      name = "T_TwoBlobs",
      newHUDObjects = () => new GameObject[] {},
    },
    new TutorialFloorInfo {
      name = "T_Healing",
      newHUDObjects = () => new GameObject[] { HUDController.main.hpBar, HUDController.main.damageFlash, HUDController.main.inventoryToggle, HUDController.main.inventoryContainer },
    },
    new TutorialFloorInfo {
      name = "T_Jackals",
      newHUDObjects = () => new GameObject[] {},
    },
    new TutorialFloorInfo {
      name = "T_Guardleaf",
      newHUDObjects = () => new GameObject[] { HUDController.main.statuses },
    },
    new TutorialFloorInfo {
      name = "T_Battle",
      newHUDObjects = () => new GameObject[] {},
    },
  };

  [field:NonSerialized]
  public static event Action OnTutorialEnded;

  public string name;
  public TutorialFloor(int depth, int width, int height) : base(depth, width, height) {}

  public TutorialFloorInfo GetInfo() => TUTORIAL_FLOORS.First(f => f.name == this.name);

  public static Floor CreateFromPrebuilt(Prebuilt pb) {
    var bounds = pb.GetEntityBounds();

    var floor = new TutorialFloor(-1, bounds.x, bounds.y);
    floor.root = new Room(floor);
    // SO hacky
    foreach(var entity in pb.entitiesWithoutPlayer) {
      entity.ForceSetTimeCreated(GameModel.main.time);
      if (entity is Actor a) {
        a.timeNextAction = entity.timeCreated;
      }
      floor.Put(entity);
    }
    floor.name = pb.name;
    foreach(var pos in floor.EnumerateFloor()) {
      if (floor.tiles[pos] == null) {
        Debug.LogError($"{floor.name}: no Tile for {pos}, using Ground!");
        floor.Put(new Ground(pos));
      }
    }

    if (floor.name == "T_Healing") {
      floor.Put(new ItemOnGround(new Vector2Int(7, 2), new ItemStick()));
    } else if (floor.name == "T_Battle") {
      floor.Put(new ItemOnGround(new Vector2Int(1, 3), new ItemStick()));
      floor.Put(new ItemOnGround(new Vector2Int(2, 2), new ItemBarkShield()));
    }

    return floor;
  }

    public override void ClearFloor() {
      // don't add upstairs
      // AddUpstairs();
      AddDownstairs();
      GameModel.main.FloorCleared(this);
    }

    internal override void PlayerGoDownstairs() {
    var floorIndex = GetInfo().index;

    if (floorIndex == TUTORIAL_FLOORS.Length - 1 || floorIndex == -1) {
      PlayerPrefs.SetInt("hasSeenPrologue", 1);
      OnTutorialEnded?.Invoke();
    } else {
      // go onto next floor
      var nextFloorName = TUTORIAL_FLOORS[floorIndex + 1].name;
      Prebuilt pb = Prebuilt.LoadBaked(nextFloorName);

      Serializer.SaveMainToCheckpoint();
      GameModel.main.PutPlayerAt(CreateFromPrebuilt(pb), pb.player?.pos);
    }
  }
}
