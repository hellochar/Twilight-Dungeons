
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
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 1, 9, 7, 3, 2)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 2, 9, 7, 3, 2, extraEncounters: Encounters.OneAstoria)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 3, 9, 7, 3, 3)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 4, 9, 7, 5, 3, true, extraEncounters: Encounters.OneAstoria)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 5, 9, 7, 5, 3)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 6, 9, 7, 5, 3)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 7, 9, 7, 6, 3)),
      () => Generate.RewardFloor(new RewardFloorParams(8, shared.Plants.GetRandomAndDiscount(1f), Encounters.OneAstoria)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 9, 10, 7, 6, 3)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 10, 10, 7, 7, 3)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 11, 10, 7, 7, 3, true, null, Encounters.AddDownstairsInRoomCenter)),
      () => Generate.BlobBossFloor(12),

      // midgame
      // CAREFUL - EncounterGroup has mutated here!
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 13, 11, 8, 2, 1)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 14, 11, 8, 2, 1)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 15, 11, 8, 2, 1)),
      () => Generate.RewardFloor(new RewardFloorParams(16, shared.Plants.GetRandomAndDiscount(1f), Encounters.OneAstoria)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 17, 12, 8, 3, 2)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 18, 12, 8, 3, 2)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 19, 12, 8, 4, 3, true)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 20, 12, 8, 5, 2)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 21, 12, 8, 6, 3)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 22, 12, 8, 7, 4, true, null, Encounters.AddDownstairsInRoomCenter, Encounters.FungalColonyAnticipation)),
      () => Generate.FungalColonyBossFloor(23),
      () => Generate.RewardFloor(new RewardFloorParams(24, shared.Plants.GetRandomAndDiscount(1f), Encounters.AddWater)),

      // endgame
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 25, 12, 8, 2, 2)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 26, 12, 8, 2, 2)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 27, 13, 9, 3, 3, false, null, Encounters.AddWater)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 28, 13, 9, 4, 3, false, new Encounter[] { Encounters.LineWithOpening, Encounters.ChasmsAwayFromWalls2 })),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 29, 13, 9, 5, 3)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 30, 13, 9, 6, 3)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 31, 14, 9, 7, 4)),
      () => Generate.RewardFloor(new RewardFloorParams(32, shared.Plants.GetRandomAndDiscount(1f), Encounters.AddWater, Encounters.ThreeAstoriasInCorner)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 33, 14, 9, 8, 5)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 34, 14, 9, 9, 6)),
      () => Generate.SingleRoomFloor(new SingleRoomFloorParams(EncounterGroup, 35, 14, 9, 10, 7)),
      () => Generate.EndBossFloor(36),
      () => Generate.EndFloor(37),
    };
  }
}
