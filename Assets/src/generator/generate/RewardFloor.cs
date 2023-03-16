using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class RewardFloorParams {
  public readonly int depth;
  public readonly Encounter[] extraEncounters;
  public RewardFloorParams(int depth, params Encounter[] extraEncounters) {
    this.depth = depth;
    this.extraEncounters = extraEncounters;
  }
}

public static partial class Generate {
  public static Floor RewardFloor(RewardFloorParams obj) {
    Floor floor = new Floor(obj.depth, 12, 8);
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
    Encounters.AddOneWater.Apply(floor, room0);
    foreach (var encounter in obj.extraEncounters) {
      encounter.Apply(floor, room0);
    }

    FloorUtils.TidyUpAroundStairs(floor);
    floor.root = room0;

    return floor;
  }
}