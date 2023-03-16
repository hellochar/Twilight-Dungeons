
using System;
using System.Collections.Generic;

[Serializable]
public class FloorGeneratorChainFloors : FloorGenerator {
  public FloorGeneratorChainFloors(List<int> floorSeeds) : base(floorSeeds) {
  }

  protected override EncounterGroup GetEncounterGroup(int depth) {
    /// configure the EncounterGroup
    if (depth <= 10) {
      return earlyGame;
    } else if (depth <= 12) {
      return everything;
    } else {
      return midGame;
    }
  }

  protected override void InitFloorGenerators() {
    floorGenerators = new List<Func<Floor>>() {
      // early game
      () => generateHomeFloor(),
      () => Generate.ChainFloor(EncounterGroup, 1 , 7, 6, 5, 1, 1, true),
      () => Generate.ChainFloor(EncounterGroup, 2 , 7, 6, 5, 1, 1, true),
      () => Generate.ChainFloor(EncounterGroup, 3 , 7, 6, 5, 2, 1, true),
      () => Generate.ChainFloor(EncounterGroup, 4 , 7, 6, 6, 2, 1, true),
      () => Generate.ChainFloor(EncounterGroup, 5 , 7, 6, 6, 2, 1, true),
      () => Generate.ChainFloor(EncounterGroup, 6 , 7, 6, 6, 2, 1, true),
      () => Generate.ChainFloor(EncounterGroup, 7 , 7, 7, 6, 3, 1, true),
      () => Generate.ChainFloor(EncounterGroup, 8 , 7, 7, 6, 3, 1, true),
      () => Generate.ChainFloor(EncounterGroup, 9 , 7, 7, 6, 3, 1, true),
      () => Generate.BlobBossFloor(10),

      // midgame
      () => Generate.ChainFloor(EncounterGroup, 8 , 5, 7, 6, 1, 1, true),
      () => Generate.ChainFloor(EncounterGroup, 9 , 5, 7, 6, 1, 1, true),
      () => Generate.ChainFloor(EncounterGroup, 10 , 5, 7, 6, 2, 2, true),
      () => Generate.ChainFloor(EncounterGroup, 11 , 5, 8, 7, 2, 2, true),
      () => Generate.ChainFloor(EncounterGroup, 12, 5, 8, 7, 3, 3, true),
      // () => generateRewardFloor(13, shared.Plants.GetRandomAndDiscount(1f)),
      () => Generate.RewardFloor(new RewardFloorParams(13, Encounters.AddWater, Encounters.OneAstoria)),
      () => Generate.FungalColonyBossFloor(14),

      // endgame
      () => Generate.ChainFloor(EncounterGroup, 15, 5, 9, 7, 1, 1, true),
      () => Generate.ChainFloor(EncounterGroup, 16, 5, 9, 7, 1, 1, true),
      () => Generate.ChainFloor(EncounterGroup, 17, 5, 9, 7, 2, 2, true),
      () => Generate.ChainFloor(EncounterGroup, 18, 5, 10, 8, 2, 2, true),
      () => Generate.ChainFloor(EncounterGroup, 19, 5, 10, 8, 3, 3, true),
      // () => generateRewardFloor(20, shared.Plants.GetRandomAndDiscount(1f)),
      () => Generate.RewardFloor(new RewardFloorParams(20, Encounters.AddWater, Encounters.OneAstoria)),
      () => Generate.EndBossFloor(21),
      () => Generate.EndFloor(22),
    };
  }
}
