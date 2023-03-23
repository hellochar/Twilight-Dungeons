using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class Generate {
  public static HomeFloor GardeningActionPointsFloor0() {
    HomeFloor floor = new HomeFloor(31, 25);
    FloorUtils.CarveGround(floor);
    // FloorUtils.SurroundWithWalls(floor);
    foreach (var p in floor.EnumeratePerimeter()) {
      if (p.x == floor.width - 1) {
        floor.Put(new Wall(p));
      } else {
        floor.Put(new Ground(p));
      }
    }
    // FloorUtils.NaturalizeEdges(floor);

    // fits 4x2 structures, and +1 on each edge to see the walls
    // var rootWidth = (3 + 2 + 2 + 2) + 2;
    // var rootHeight = (3 + 2) + 2;
    var rootWidth = 7 + 2;
    var rootHeight = 5 + 2;
    var middleRight = new Vector2Int(floor.width - 2, floor.height / 2);
    var rootMin = new Vector2Int(middleRight.x - (rootWidth - 2), middleRight.y - (rootHeight - 1) / 2);
    var root = new Room(
      rootMin,
      rootMin + new Vector2Int(rootWidth - 1, rootHeight - 1)
    );
    floor.rooms = new List<Room> { root };
    floor.root = root;

    List<Room> farAwayRooms = new List<Room>();
    // add rewards and plants randomly
    var SECTIONS = 5;
    for(int i = 0; i < floor.width; i += floor.width / SECTIONS + 1) {
      for(int j = 0; j < floor.height; j += floor.height / SECTIONS + 1) {
        var room = new Room(new Vector2Int(i, j), new Vector2Int(i + floor.width / SECTIONS, j + floor.height / SECTIONS));
        if (floor.InBounds(room.max)) {
          if (!room.rect.Overlaps(root.rect)) {
            farAwayRooms.Add(room);
          }
        }
      }
    }

    // shared.Plants.GetRandomAndDiscount(1f)(floor, root);

    // pick three of them for plants, the rest for rewards
    var room1 = Util.RandomPick(farAwayRooms);
    farAwayRooms.Remove(room1);
    var room2 = Util.RandomPick(farAwayRooms);
    farAwayRooms.Remove(room2);
    var room3 = Util.RandomPick(farAwayRooms);
    farAwayRooms.Remove(room3);

    // shared.Plants.GetRandomAndDiscount(1)(floor, room1);
    // shared.Plants.GetRandomAndDiscount(1)(floor, room2);
    // shared.Plants.GetRandomAndDiscount(1)(floor, room3);

    foreach(var r in farAwayRooms) {
      // Encounters.PutSlime(floor, r);
      Encounters.AddOneWater.Apply(floor, r);
      // shared.Rewards.GetRandom()(floor, r);
    }

    // FloorUtils.AddWallsOutside(floor, floor.root);
    FloorUtils.AddThickBrushOutside(floor, floor.root);

    floor.startPos = new Vector2Int(root.min.x + 1, root.center.y);
    floor.PlaceDownstairs(new Vector2Int(root.max.x, root.center.y));
    Encounters.AddWater.Apply(floor, root);
    // Encounters.AddOrganicMatter(floor, root);
    // Encounters.AddOrganicMatter(floor, root);
    // Encounters.AddOrganicMatter(floor, root);
    // Encounters.OneAstoria(floor, root);
    FloorUtils.TidyUpAroundStairs(floor);

    // var tiles = FloorUtils.EmptyTilesInRoom(floor, room0).ToList();
    // tiles.Shuffle();
    // floor.PutAll(tiles.Take(10).Select(t => new HardGround(t.pos)).ToList());

    return floor;
  }
}