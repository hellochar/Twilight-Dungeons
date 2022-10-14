
using System;
using System.Collections.Generic;

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
      () => generateSingleRoomFloor(1, 9, 7, 1, 1),
      () => generateSingleRoomFloor(2, 9, 7, 1, 1, extraEncounters: Encounters.OneAstoria),
      () => generateSingleRoomFloor(3, 9, 7, 1, 1),
      () => generateSingleRoomFloor(4, 9, 7, 2, 1, true, extraEncounters: Encounters.OneAstoria),
      () => generateSingleRoomFloor(5, 9, 7, 2, 1),
      () => generateSingleRoomFloor(6, 9, 7, 2, 1),
      () => generateSingleRoomFloor(7, 9, 7, 2, 1),
      () => generateRewardFloor(8, shared.Plants.GetRandomAndDiscount(1f), Encounters.OneAstoria),
      () => generateSingleRoomFloor(9, 10, 7, 3, 2),
      () => generateSingleRoomFloor(10, 10, 7, 3, 2),
      () => generateSingleRoomFloor(11, 10, 7, 3, 2, true, null, Encounters.AddDownstairsInRoomCenter),
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
}
