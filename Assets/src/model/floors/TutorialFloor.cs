using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class TutorialFloor : Floor {
  public static string[] PREBUILT_NAMES = {
    "T_Room1",
    "T_TwoBlobs",
    "T_Healing",
    "T_Jackals",
    "T_Guardleaf"
  };

  [field:NonSerialized]
  public static event Action OnTutorialEnded;

  public string name;
  public TutorialFloor(int depth, int width, int height) : base(depth, width, height) {}


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

    if (floor.name == "T_Healing") {
      floor.Put(new ItemOnGround(new Vector2Int(7, 2), new ItemRedberry(2)));
    }

    return floor;
  }

  internal override void PlayerGoDownstairs() {
    var floorIndex = Array.IndexOf(PREBUILT_NAMES, name);

    if (floorIndex == PREBUILT_NAMES.Length - 1 || floorIndex == -1) {
      PlayerPrefs.SetInt("hasSeenPrologue", 1);
      OnTutorialEnded?.Invoke();
    } else {
      // go onto next floor
      var nextFloorName = PREBUILT_NAMES[floorIndex + 1];
      Prebuilt pb = Prebuilt.LoadBaked(nextFloorName);

      Serializer.SaveMainToCheckpoint();
      GameModel.main.PutPlayerAt(CreateFromPrebuilt(pb), pb.player?.pos);
    }
  }
}
