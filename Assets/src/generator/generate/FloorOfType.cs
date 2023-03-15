using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class Generate {
  public static Floor FloorOfType(EncounterGroup EncounterGroup, EncounterGroupShared shared, int depth, FloorType type) {
    if (depth == 12) {
      return EndFloor(12);
    }
    int width = 11 + (depth - 1) / 3;
    int height = 8 + (depth - 1) / 3;
    switch(type) {
      case FloorType.Slime:
        return generateSingleRoomFloorSingleType(EncounterGroup, depth, width, height, 2 + depth, depth, false, null, Encounters.AddSlime);
      case FloorType.Processor:
        return generateSingleRoomFloorSingleType(EncounterGroup, depth, width, height, 2 + depth, depth, false, null, Encounters.AddProcessor);
      case FloorType.CraftingStation:
        return generateSingleRoomFloorSingleType(EncounterGroup, depth, width, height, 2 + depth, depth, false, null, Encounters.AddCrafting);
      case FloorType.Healing:
        return generateSingleRoomFloorSingleType(EncounterGroup, depth, width, height, 2 + depth, depth, false, null, Encounters.AddCampfire);
      case FloorType.Plant:
        return generateSingleRoomFloorSingleType(EncounterGroup, depth, width, height, 2 + depth, depth, false, null, shared.Plants.GetRandomAndDiscount(0.999f));
      case FloorType.Composter:
        return generateSingleRoomFloorSingleType(EncounterGroup, depth, width, height, 2 + depth, depth, false, null, Encounters.AddComposter);
      case FloorType.Mystery:
        throw new CannotPerformActionException("Cannot generate mystery floor type!");
      case FloorType.Empty:
        throw new CannotPerformActionException("Cannot generate empty floor type!");
      // case MistType.Trade:
      //   return generateEncounterFloor(depth, width, height, Encounters.RandomTrade);
      case FloorType.Combat:
      default:
        return generateSingleRoomFloorSingleType(EncounterGroup, depth, width, height, 2 + depth, depth);
    }
  }

  public static Floor generateSingleRoomFloorSingleType(EncounterGroup EncounterGroup, int depth, int width, int height, int numMobs, int numGrasses, bool reward = false, Encounter[] preMobEncounters = null, params Encounter[] extraEncounters) {
    Floor floor = tryGenerateSingleRoomFloor(EncounterGroup, depth, width, height, preMobEncounters == null);
    ensureConnectedness(floor);
    floor.PutAll(
      floor.EnumeratePerimeter().Where(pos => floor.tiles[pos] is Ground).Select(pos => new Wall(pos))
    );
    var room0 = floor.root;
    if (preMobEncounters != null) {
      foreach (var encounter in preMobEncounters) {
        encounter.Apply(floor, room0);
      }
    }

    var mobEncounter = EncounterGroup.Mobs.GetRandomAndDiscount();
    // X mobs
    for (var i = 0; i < numMobs; i++) {
      mobEncounter.Apply(floor, room0);
      // EncounterGroup.Mobs.GetRandomAndDiscount()(floor, room0);
    }

    var grassEncounter = EncounterGroup.Grasses.GetRandomAndDiscount();
    // Y grasses
    for (var i = 0; i < numGrasses; i++) {
      grassEncounter.Apply(floor, room0);
      // EncounterGroup.Grasses.GetRandomAndDiscount()(floor, room0);
    }

    foreach (var encounter in extraEncounters) {
      encounter.Apply(floor, room0);
    }

    // a reward (optional)
    if (reward) {
      Encounters.AddWater.Apply(floor, room0);
      EncounterGroup.Rewards.GetRandomAndDiscount().Apply(floor, room0);
    }

    EncounterGroup.Spice.GetRandom().Apply(floor, room0);
    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }
}