
using System;
using System.Collections.Generic;
using static Generators;

[Serializable]
public class FloorGenerator200Start : FloorGenerator {
  public FloorGenerator200Start(List<int> floorSeeds) : base(floorSeeds) {
  }

  protected override EncounterGroup GetEncounterGroup(int depth) {
    /// configure the EncounterGroup
    if (depth <= 12) {
      return earlyGame;
    } else if (depth <= 24) {
      return everything;
    } else {
      return midGame;
    }
  }

  protected override void InitFloorGenerators() {
    floorGenerators = new List<Func<Floor>>() {
      // early game
      () => generateHomeFloor(),
      () => generateSingleRoomFloor(EncounterGroup, 1, 9, 7, 3, 2),
      () => generateSingleRoomFloor(EncounterGroup, 2, 9, 7, 3, 2, extraEncounters: Encounters.OneAstoria),
      () => generateSingleRoomFloor(EncounterGroup, 3, 9, 7, 3, 3),
      () => generateSingleRoomFloor(EncounterGroup, 4, 9, 7, 5, 3, true, extraEncounters: Encounters.OneAstoria),
      () => generateSingleRoomFloor(EncounterGroup, 5, 9, 7, 5, 3),
      () => generateSingleRoomFloor(EncounterGroup, 6, 9, 7, 5, 3),
      () => generateSingleRoomFloor(EncounterGroup, 7, 9, 7, 6, 3),
      () => generateRewardFloor(8, shared.Plants.GetRandomAndDiscount(1f), Encounters.OneAstoria),
      () => generateSingleRoomFloor(EncounterGroup, 9, 10, 7, 6, 3),
      () => generateSingleRoomFloor(EncounterGroup, 10, 10, 7, 7, 3),
      () => generateSingleRoomFloor(EncounterGroup, 11, 10, 7, 7, 3, true, null, Encounters.AddDownstairsInRoomCenter),
      () => generateBlobBossFloor(12),

      // midgame
      () => generateSingleRoomFloor(EncounterGroup, 13, 11, 8, 2, 1),
      () => generateSingleRoomFloor(EncounterGroup, 14, 11, 8, 2, 1),
      () => generateSingleRoomFloor(EncounterGroup, 15, 11, 8, 2, 1),
      () => generateRewardFloor(16, shared.Plants.GetRandomAndDiscount(1f), Encounters.OneAstoria),
      () => generateSingleRoomFloor(EncounterGroup, 17, 12, 8, 3, 2),
      () => generateSingleRoomFloor(EncounterGroup, 18, 12, 8, 3, 2),
      () => generateSingleRoomFloor(EncounterGroup, 19, 12, 8, 4, 3, true),
      () => generateSingleRoomFloor(EncounterGroup, 20, 12, 8, 5, 2),
      () => generateSingleRoomFloor(EncounterGroup, 21, 12, 8, 6, 3),
      () => generateSingleRoomFloor(EncounterGroup, 22, 12, 8, 7, 4, true, null, Encounters.AddDownstairsInRoomCenter, Encounters.FungalColonyAnticipation),
      () => generateFungalColonyBossFloor(23),
      () => generateRewardFloor(24, shared.Plants.GetRandomAndDiscount(1f), Encounters.AddWater),

      // endgame
      () => generateSingleRoomFloor(EncounterGroup, 25, 12, 8, 2, 2),
      () => generateSingleRoomFloor(EncounterGroup, 26, 12, 8, 2, 2),
      () => generateSingleRoomFloor(EncounterGroup, 27, 13, 9, 3, 3, false, null, Encounters.AddWater),
      () => generateSingleRoomFloor(EncounterGroup, 28, 13, 9, 4, 3, false, new Encounter[] { Encounters.LineWithOpening, Encounters.ChasmsAwayFromWalls2 }),
      () => generateSingleRoomFloor(EncounterGroup, 29, 13, 9, 5, 3),
      () => generateSingleRoomFloor(EncounterGroup, 30, 13, 9, 6, 3),
      () => generateSingleRoomFloor(EncounterGroup, 31, 14, 9, 7, 4),
      () => generateRewardFloor(32, shared.Plants.GetRandomAndDiscount(1f), Encounters.AddWater, Encounters.ThreeAstoriasInCorner),
      () => generateSingleRoomFloor(EncounterGroup, 33, 14, 9, 8, 5),
      () => generateSingleRoomFloor(EncounterGroup, 34, 14, 9, 9, 6),
      () => generateSingleRoomFloor(EncounterGroup, 35, 14, 9, 10, 7),
      () => generateEndBossFloor(36),
      () => generateEndFloor(37),
    };
  }
}
