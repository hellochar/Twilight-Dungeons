using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class Generate {
  
  public static Floor FungalColonyBossFloor(int depth) {
    Floor floor = new BossFloor(depth, 13, 9);
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Wall(p));
    }

    Room room0 = new Room(floor);

    void CutOutCircle(Vector2Int center, float radius) {
      FloorUtils.CarveGround(floor, floor.EnumerateCircle(center, radius));
    }

    // start and end paths
    CutOutCircle(new Vector2Int(3, floor.height / 2), 2f);
    CutOutCircle(new Vector2Int(floor.width - 4, floor.height / 2), 2f);
    CutOutCircle(room0.center, 4.5f);
    floor.Put(new FungalColony(room0.center));

    // turn some of the walls and empty space into fungal walls
    foreach (var pos in floor.EnumerateFloor()) {
      var t = floor.tiles[pos];
      if (t is Wall && floor.GetAdjacentTiles(t.pos).Any(t2 => t2.CanBeOccupied())) {
        floor.Put(new FungalWall(pos));
      } else if (t is Ground && t.CanBeOccupied()) {
        if (MyRandom.value < 0.25f) {
          floor.Put(new FungalWall(pos));
        }
      }
    }

    // re-apply normal Wall to outer perimeter
    floor.PutAll(floor.EnumeratePerimeter().Select(pos => new Wall(pos)));

    // block entrance
    // floor.PutAll(new FungalWall(new Vector2Int(7, 5)), new FungalWall(new Vector2Int(7, 6)), new FungalWall(new Vector2Int(7, 7)));
    floor.SetStartPos(new Vector2Int(0, floor.height / 2));
    floor.PlaceDownstairs(new Vector2Int(floor.width - 1, floor.height / 2));

    floor.root = room0;
    floor.rooms = new List<Room>();
    // floor.startRoom = room0;
    floor.downstairsRoom = room0;

#if experimental_grassesonbossfloor
    EncounterGroup.Grasses.GetRandomAndDiscount()(floor, room0);
#endif

    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }


}