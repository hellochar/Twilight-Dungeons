using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorDebug : MonoBehaviour {
  private Floor floor;
  private Sprite sprite;
  void Start() {
    sprite = Resources.Load<Sprite>("Square");
    floor = GameModel.main.currentFloor;
    MakeGameObjectForRoom(floor.root, "R");
  }

  void MakeGameObjectForRoom(Room room, string name) {
    if (room.isTerminal) {
      name += "*";
    } else {
      name += room.split.Value.direction == SplitDirection.Horizontal ? "h" : "v";
    }
    var go = new GameObject(name);
    var s = go.AddComponent<SpriteRenderer>();
    s.sortingLayerName = "UI";
    s.sprite = sprite;
    var c = s.color;
    c.a = 0.05f;
    s.color = c;

    go.transform.position = room.centerFloat;
    go.transform.localScale = new Vector3(room.width, room.height, 1);
    go.transform.parent = transform;

    if (room.split.HasValue) {
      var split = room.split.Value;
      MakeGameObjectForRoom(split.a, name + " A");
      MakeGameObjectForRoom(split.b, name + " B");
    }
  }
}
