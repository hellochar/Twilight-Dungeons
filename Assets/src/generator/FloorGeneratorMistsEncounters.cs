
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class FloorGeneratorMistsEncounters : FloorGenerator {
  // EncounterBag floorTypes;
  public FloorGeneratorMistsEncounters(List<int> floorSeeds) : base(floorSeeds) { 
    // floorTypes = new EncounterBag {
    //   { 1, generateCalmFloor }
    // }
  }

  protected override EncounterGroup GetEncounterGroup(int depth) {
    if (depth <= 6) {
      return earlyGame;
    } else if (depth <= 12) {
      return everything;
    }
    return midGame;
  }

  public override Floor generateCaveFloor(int depth) {
    floorSeeds[depth] = new System.Random().Next();
    return base.generateCaveFloor(depth);
  }

  protected override void InitFloorGenerators() {
    floorGenerators = new List<Func<Floor>>() {
      // early game
      () => generateHomeFloor(),
      () => generateMistRoomFloor(1, 9, 7, 3, 1),
      () => generateMistRoomFloor(2, 9, 7, 3, 1),
      () => generateMistRoomFloor(3, 9, 7, 4, 1),
      () => generateMistRoomFloor(4, 9, 7, 5, 1),
      () => generateMistRoomFloor(5, 9, 7, 6, 1),
      () => generateMistRoomFloor(6, 9, 7, 7, 1),
      () => generateMistRoomFloor(7, 9, 7, 8, 1),
      () => generateRewardFloor(8, shared.Plants.GetRandomAndDiscount(1f), Encounters.OneAstoria),
      () => generateSingleRoomFloor(9, 10, 7, 6, 3),
      () => generateSingleRoomFloor(10, 10, 7, 7, 3),
      () => generateSingleRoomFloor(11, 10, 7, 7, 3, true, null, Encounters.AddDownstairsInRoomCenter),
      () => generateBlobBossFloor(12),

      // midgame
      () => generateSingleRoomFloor(13, 11, 8, 2, 1),
      () => generateSingleRoomFloor(14, 11, 8, 2, 1),
      () => generateSingleRoomFloor(15, 11, 8, 2, 1),
      () => generateRewardFloor(16, shared.Plants.GetRandomAndDiscount(1f), Encounters.OneAstoria),
      () => generateSingleRoomFloor(17, 12, 8, 3, 2),
      () => generateSingleRoomFloor(18, 12, 8, 3, 2),
      () => generateSingleRoomFloor(19, 12, 8, 4, 3, true),
      () => generateSingleRoomFloor(20, 12, 8, 5, 2),
      () => generateSingleRoomFloor(21, 12, 8, 6, 3),
      () => generateSingleRoomFloor(22, 12, 8, 7, 4, true, null, Encounters.AddDownstairsInRoomCenter, Encounters.FungalColonyAnticipation),
      () => generateFungalColonyBossFloor(23),
      () => generateRewardFloor(24, shared.Plants.GetRandomAndDiscount(1f), Encounters.AddWater),

      // endgame
      () => generateSingleRoomFloor(25, 12, 8, 2, 2),
      () => generateSingleRoomFloor(26, 12, 8, 2, 2),
      () => generateSingleRoomFloor(27, 13, 9, 3, 3, false, null, Encounters.AddWater),
      () => generateSingleRoomFloor(28, 13, 9, 4, 3, false, new Encounter[] { Encounters.LineWithOpening, Encounters.ChasmsAwayFromWalls2 }),
      () => generateSingleRoomFloor(29, 13, 9, 5, 3),
      () => generateSingleRoomFloor(30, 13, 9, 6, 3),
      () => generateSingleRoomFloor(31, 14, 9, 7, 4),
      () => generateRewardFloor(32, shared.Plants.GetRandomAndDiscount(1f), Encounters.AddWater, Encounters.ThreeAstoriasInCorner),
      () => generateSingleRoomFloor(33, 14, 9, 8, 5),
      () => generateSingleRoomFloor(34, 14, 9, 9, 6),
      () => generateSingleRoomFloor(35, 14, 9, 10, 7),
      () => generateEndBossFloor(36),
      () => generateEndFloor(37),
    };
  }

  public Floor generateFloorOfType(int depth, FloorType type) {
    int width = 9 + (depth - 1) / 3;
    int height = 7 + (depth - 1) / 3;
    switch(type) {
      case FloorType.Slime:
        return generateEncounterFloor(depth, width, height, Encounters.AddSlime);
      case FloorType.Processor:
        return generateEncounterFloor(depth, width, height, Encounters.AddProcessor);
      case FloorType.CraftingStation:
        return generateEncounterFloor(depth, width, height, Encounters.AddCrafting);
      case FloorType.Healing:
        return generateEncounterFloor(depth, width, height, Encounters.AddCampfire);
      case FloorType.Plant:
        return generateEncounterFloor(depth, width, height, shared.Plants.GetRandomAndDiscount(0.999f));
      case FloorType.Mystery:
        throw new CannotPerformActionException("Cannot generate mystery floor type!");
      case FloorType.Empty:
        throw new CannotPerformActionException("Cannot generate empty floor type!");
      // case MistType.Trade:
      //   return generateEncounterFloor(depth, width, height, Encounters.RandomTrade);
      case FloorType.Combat:
      default:
        return generateSingleRoomFloor(depth, width, height, 2 + depth, depth);
    }
  }

  private Floor generateEncounterFloor(int depth, int width, int height, params Encounter[] encounters) {
    return generateSingleRoomFloor(depth, width - 1, height - 1, 1, 1, false, null, encounters);
  }

  public Floor generateMistRoomFloor(int depth, int width, int height, int numMobs, int numGrasses, bool reward = false, Encounter[] preMobEncounters = null, params Encounter[] extraEncounters) {
    // if (MyRandom.value < 0.5f) {
      return generateSingleRoomFloor(depth, width, height, numMobs, numGrasses, false, null, new Encounter[] {
        Encounters.AddSlime
      });
    // } else if (MyRandom.value < 0.5f) {
    //   return generateGrassFloor(depth, width - 1, height - 1, numGrasses, reward);
    // } else {
    //   return generateSlimeFloor(depth, width - 1, height - 1);
    // }
  }

  public Floor generateSlimeFloor(int depth, int width, int height) {
    Floor floor = tryGenerateSingleRoomFloor(depth, width, height);
    ensureConnectedness(floor);
    floor.PutAll(
      floor.EnumeratePerimeter().Where(pos => floor.tiles[pos] is Ground).Select(pos => new Wall(pos))
    );
    for(int i = 0; i < depth + 3; i++) {
      Encounters.AddSlime(floor, floor.root);
    }
    return floor;
  }

  public Floor generateGrassFloor(int depth, int width, int height, int numGrasses, bool reward = false) {
    Floor floor = tryGenerateSingleRoomFloor(depth, width, height);
    ensureConnectedness(floor);
    floor.PutAll(
      floor.EnumeratePerimeter().Where(pos => floor.tiles[pos] is Ground).Select(pos => new Wall(pos))
    );
    var room0 = floor.root;
    // Y grasses
    for (var i = 0; i < numGrasses; i++) {
      var grassEncounter = EncounterGroup.Grasses.GetRandomAndDiscount();
      for (int j = 0; j < 3; j++) {
        grassEncounter(floor, room0);
      }
    }

    EncounterGroup.Spice.GetRandom()(floor, room0);
    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }
}
