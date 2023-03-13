using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class Generators {
  public static Floor generateEndBossFloor(int depth) {
    Floor floor = new BossFloor(depth, 15, 11);
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Ground(p));
    }
    foreach(var p in floor.EnumeratePerimeter()) {
      floor.Put(new Wall(p));
    }
    FloorUtils.NaturalizeEdges(floor);
    foreach(var p in floor.EnumerateFloor()) {
      if (floor.tiles[p] is Wall) {
        floor.Put(new Chasm(p));
      }
    }

    Room room0 = new Room(floor);

    floor.Put(new CorruptedEzra(room0.center));
    floor.SetStartPos(new Vector2Int(0, floor.height / 2));
    floor.PlaceDownstairs(new Vector2Int(floor.width - 1, floor.height / 2));

    floor.root = room0;
    floor.rooms = new List<Room>();
    // floor.startRoom = room0;
    floor.downstairsRoom = room0;

    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }
}