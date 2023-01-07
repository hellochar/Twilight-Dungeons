using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ExpandingHomeFloor : HomeFloor {
  public static ExpandingHomeFloor generate(int numFloors) {
    var startSize = new Vector2Int(6, 6);
    var finalSize = startSize + 2 * Vector2Int.one * numFloors;
    ExpandingHomeFloor floor = new ExpandingHomeFloor(finalSize.x, finalSize.y);
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Chasm(p));
    }

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
    // put a pit at the center
    floor.Put(new Pit(room0.center));
    Encounters.AddWater(floor, room0);

    // show chasm edges
    room0.max += Vector2Int.one;
    room0.min -= Vector2Int.one;

    return floor;
  }

  public ExpandingHomeFloor(int width, int height) : base(width, height) { }

  public void Expand() {
    foreach(var pos in this.EnumerateRoomPerimeter(root)) {
      Put(new HomeGround(pos));
    }
    root.max += Vector2Int.one;
    root.min -= Vector2Int.one;
  }

  public override void Put(Entity entity) {
    base.Put(entity);
    if (entity is HomeGround) {
      root.ExtendToEncompass(new Room(entity.pos - Vector2Int.one, entity.pos + Vector2Int.one));
      RecomputeVisibility();
      // f.RecomputeVisibility();
    }
  }
}
