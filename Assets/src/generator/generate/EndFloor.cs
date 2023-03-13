using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class Generate {
  public static Floor EndFloor(int depth) {
    // use bossfloor to get rid of the sound
    Floor floor = new BossFloor(depth, 14, 42);

    FloorUtils.SurroundWithWalls(floor);

    var room0 = new Room(floor);
    floor.rooms = new List<Room> { room0 };
    floor.root = room0;

    var treePos = room0.center + Vector2Int.up * 10;

    // carve out chasms/ground
    for (int y = 0; y < floor.height; y++) {
      var angle = Util.MapLinear(y, 0, floor.height / 2, 0, Mathf.PI / 2 * 1.7f);
      var width = Mathf.Max(0, Mathf.Cos(angle)) * (floor.width - 2f) + 1;
      var xMin = (floor.width / 2f - width / 2) - 1;
      var xMax = (xMin + width);
      for (int x = 0; x < floor.width; x++) {
        var pos = new Vector2Int(x, y);
        if (x < xMin || x > xMax || y > treePos.y) {
          floor.Put(new Chasm(pos));
        } else if (floor.tiles[pos] == null) {
          floor.Put(new Ground(pos));
        }
      }
    }

    floor.SetStartPos(new Vector2Int(floor.width / 2 - 1, 1), false);

    Room roomBot = new Room(Vector2Int.one, new Vector2Int(floor.width - 2, 11));
    for (var i = 0; i < 1; i++) {
      Encounters.AddWater(floor, roomBot);
    }
    Encounters.TwelveRandomAstoria(floor, roomBot);
    for (var i = 0; i < 12; i++) {
      Encounters.AddGuardleaf(floor, roomBot);
    }

    foreach (var t in floor.EnumerateRoom(roomBot).Where(p => floor.tiles[p] is Ground && floor.grasses[p] == null)) {
      floor.Put(new SoftGrass(t));
    }

    foreach (var pos in floor.EnumerateCircle(treePos + Vector2Int.down, 2.5f).Where(p => p.y <= treePos.y)) {
      var grass = floor.grasses[pos];
      if (grass != null) {
        floor.Remove(grass);
      }
      floor.Put(new FancyGround(pos));
    }

    /// add Tree of Life right in the middle
    floor.Put(new TreeOfLife(treePos));
    floor.Put(new Ezra(treePos + Vector2Int.down * 2));

    return floor;
  }
}