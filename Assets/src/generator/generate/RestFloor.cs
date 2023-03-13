using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class Generate {
  // A little dose of coziness, a half-reward, something nice, a breather
  // maybe a pool of water to wash off on, a place to rest and recover
  // maybe healing
  // 
  public static Floor RestFloor(int depth, EncounterGroup shared) {
    Floor floor = new Floor(depth, 8 + 2, 6 + 2);
    FloorUtils.CarveGround(floor);
    FloorUtils.SurroundWithWalls(floor);
    FloorUtils.NaturalizeEdges(floor);

    var room0 = new Room(floor);

    floor.SetStartPos(new Vector2Int(room0.min.x, room0.max.y));
    floor.PlaceDownstairs(new Vector2Int(room0.max.x, room0.min.y));

    Encounters.AddOneWater(floor, room0);
    shared.Rests.GetRandomAndDiscount(0.5f)(floor, room0);

    FloorUtils.TidyUpAroundStairs(floor);
    floor.root = room0;
    return floor;
  }
}