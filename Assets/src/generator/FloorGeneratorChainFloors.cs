
using System;
using System.Collections.Generic;

[Serializable]
public class FloorGeneratorChainFloors : FloorGenerator {
  public FloorGeneratorChainFloors(List<int> floorSeeds) : base(floorSeeds) {
  }

  protected override EncounterGroup GetEncounterGroup(int depth) {
    /// configure the EncounterGroup
    if (depth <= 6) {
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
      () => generateChainFloor(1 , 5, 6, 5, 1, 1, true),
      () => generateChainFloor(2 , 5, 6, 5, 1, 1, true),
      () => generateChainFloor(3 , 5, 6, 5, 2, 2, true),
      () => generateChainFloor(4 , 5, 7, 6, 2, 2, true),
      () => generateChainFloor(5 , 5, 7, 6, 3, 3, true),
      // () => generateRewardFloor(6, shared.Plants.GetRandomAndDiscount(1f)),
      () => generateRewardFloor(6, Encounters.AddWater, Encounters.OneAstoria),
      () => generateBlobBossFloor(7),

      // midgame
      () => generateChainFloor(8 , 5, 7, 6, 1, 1, true),
      () => generateChainFloor(9 , 5, 7, 6, 1, 1, true),
      () => generateChainFloor(10 , 5, 7, 6, 2, 2, true),
      () => generateChainFloor(11 , 5, 8, 7, 2, 2, true),
      () => generateChainFloor(12, 5, 8, 7, 3, 3, true),
      // () => generateRewardFloor(13, shared.Plants.GetRandomAndDiscount(1f)),
      () => generateRewardFloor(13, Encounters.AddWater, Encounters.OneAstoria),
      () => generateFungalColonyBossFloor(14),

      // endgame
      () => generateChainFloor(15, 5, 9, 7, 1, 1, true),
      () => generateChainFloor(16, 5, 9, 7, 1, 1, true),
      () => generateChainFloor(17, 5, 9, 7, 2, 2, true),
      () => generateChainFloor(18, 5, 10, 8, 2, 2, true),
      () => generateChainFloor(19, 5, 10, 8, 3, 3, true),
      // () => generateRewardFloor(20, shared.Plants.GetRandomAndDiscount(1f)),
      () => generateRewardFloor(20, Encounters.AddWater, Encounters.OneAstoria),
      () => generateEndBossFloor(21),
      () => generateEndFloor(22),
    };
  }
}
