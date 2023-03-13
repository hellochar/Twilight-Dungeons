using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class Generators {

  public static Floor generateBlobBossFloor(int depth) {
    Floor floor = new BossFloor(depth, 12, 9);
    // fill with wall
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Ground(p));
    }
    foreach(var p in floor.EnumeratePerimeter()) {
      floor.Put(new Wall(p));
    }
    floor.Put(new Wall(new Vector2Int(1, 1)));
    floor.Put(new Wall(new Vector2Int(floor.width - 1, 1)));
    floor.Put(new Wall(new Vector2Int(floor.width - 1, floor.height - 1)));
    floor.Put(new Wall(new Vector2Int(1, floor.height - 1)));

    Room room0 = new Room(floor);
    floor.Put(new Wall(room0.center + new Vector2Int(2, 2)));
    floor.Put(new Wall(room0.center + new Vector2Int(2, -2)));
    floor.Put(new Wall(room0.center + new Vector2Int(-2, -2)));
    floor.Put(new Wall(room0.center + new Vector2Int(-2, 2)));

    floor.SetStartPos(new Vector2Int(1, floor.height / 2));
    floor.PlaceDownstairs(new Vector2Int(floor.width - 2, floor.height / 2));

    floor.root = room0;
    floor.rooms = new List<Room>();
    // floor.startRoom = room0;
    floor.downstairsRoom = room0;

    // add boss
    floor.Put(new Blobmother(floor.downstairs.pos));

#if experimental_grassesonbossfloor
    EncounterGroup.Grasses.GetRandomAndDiscount()(floor, room0);
#endif

    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }

}