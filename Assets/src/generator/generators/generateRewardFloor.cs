using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class Generators {
  public static Floor generateRewardFloor(int depth, params Encounter[] extraEncounters) {
    Floor floor = new Floor(depth, 12, 8);
    FloorUtils.CarveGround(floor);
    FloorUtils.SurroundWithWalls(floor);
    FloorUtils.NaturalizeEdges(floor);

    var room0 = new Room(floor);

    floor.SetStartPos(new Vector2Int(room0.min.x, room0.max.y));
    floor.PlaceDownstairs(new Vector2Int(room0.max.x, room0.min.y));

    // Encounters.PlaceFancyGround(floor, room0);
    // Encounters.CavesRewards.GetRandomAndDiscount()(floor, room0);
    // EncounterGroup.Plants.GetRandomAndDiscount(0.9f)(floor, room0);
    // Encounters.AddTeleportStone(floor, room0);
    Encounters.AddOneWater(floor, room0);
    foreach (var encounter in extraEncounters) {
      encounter(floor, room0);
    }

    FloorUtils.TidyUpAroundStairs(floor);
    floor.root = room0;

    return floor;
  }
}