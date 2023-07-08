using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class TutorialFloor : Floor {
  [field:NonSerialized]
  public event Action OnTutorialEnded;

  public string name;
  public TutorialFloor(int depth, int width, int height) : base(depth, width, height) {}


  public static Floor CreateFromPrebuilt(Prebuilt pb) {
    var bounds = pb.GetEntityBounds();

    var floor = new TutorialFloor(-1, bounds.x, bounds.y);
    floor.root = new Room(floor);
    floor.PutAll(pb.entitiesWithoutPlayer);
    floor.name = pb.name;

    return floor;
  }

  internal override void PlayerGoDownstairs() {
    bool tutorialEnded = GameModel.main.TutorialPlayerWentDownstairs(this);
    if (tutorialEnded) {
      PlayerPrefs.SetInt("hasSeenPrologue", 1);
      // OnTutorialEnded?.Invoke();
      GameModel.main.turnManager.OnPlayersChoice += () => OnTutorialEnded?.Invoke();
    }
  }
}
