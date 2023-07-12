using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class TutorialFloor : Floor {
  [field:NonSerialized]
  public static event Action OnTutorialEnded;

  public string name;
  public TutorialFloor(int depth, int width, int height) : base(depth, width, height) {}


  public static Floor CreateFromPrebuilt(Prebuilt pb) {
    var bounds = pb.GetEntityBounds();

    var floor = new TutorialFloor(-1, bounds.x, bounds.y);
    floor.root = new Room(floor);
    floor.PutAll(pb.entitiesWithoutPlayer);
    floor.name = pb.name;

    if (floor.name == "T_Healing") {
      floor.Put(new ItemOnGround(new Vector2Int(7, 2), new ItemRedberry(2)));
    }

    return floor;
  }

  internal override void PlayerGoDownstairs() {
    bool tutorialEnded = GameModel.main.TutorialPlayerWentDownstairs(this);
    if (tutorialEnded) {
      PlayerPrefs.SetInt("hasSeenPrologue", 1);
      // Causes some weird error with TutorialController#HandleTutorialEnded's StartCoroutine call
      // having a Unity error
      OnTutorialEnded?.Invoke();

      // GameModel.main.turnManager.OnPlayersChoice += () => OnTutorialEnded?.Invoke();
    }
  }
}
