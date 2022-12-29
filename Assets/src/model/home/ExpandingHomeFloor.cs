using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ExpandingHomeFloor : HomeFloor {
  public static ExpandingHomeFloor generate(Vector2Int startSize, int numFloors) {
    var finalSize = startSize + 2 * Vector2Int.one * numFloors;
    ExpandingHomeFloor floor = new ExpandingHomeFloor(finalSize.x, finalSize.y);
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Chasm(p));
    }

    // put a pit at the center
    var center = floor.center;
    var min = floor.center - startSize / 2;
    Room room0 = new Room(
      min,
      min + startSize - Vector2Int.one
    );
    floor.rooms = new List<Room>() { room0 };
    floor.root = room0;
    foreach (var p in floor.EnumerateRoom(room0)) {
      floor.Put(new HomeGround(p));
    }

    floor.startPos = new Vector2Int(room0.min.x, room0.center.y);
    floor.Put(new Pit(room0.center));
    Encounters.AddWater(floor, room0);

    // show chasm edges
    room0.max += Vector2Int.one;
    room0.min -= Vector2Int.one;

    return floor;
  }

  public ExpandingHomeFloor(int width, int height) : base(width, height) { }
}
