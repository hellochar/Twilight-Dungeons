
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
      () => generateChainFloor(2 , 5, 6, 5, 2, 2, true),
      () => generateChainFloor(3 , 5, 7, 6, 2, 2, true),
      () => generateChainFloor(4 , 5, 7, 6, 3, 3, true),
      () => generateRewardFloor(5, shared.Plants.GetRandomAndDiscount(1f)),
      () => generateBlobBossFloor(6),

      // midgame
      () => generateChainFloor(7 , 5, 8, 7, 1, 1, true),
      () => generateChainFloor(8 , 5, 8, 7, 2, 2, true),
      () => generateChainFloor(9 , 5, 9, 7, 2, 2, true),
      () => generateChainFloor(10, 5, 9, 7, 3, 3, true),
      () => generateRewardFloor(11, shared.Plants.GetRandomAndDiscount(1f)),
      () => generateFungalColonyBossFloor(12),

      // endgame
      () => generateChainFloor(13, 5, 10, 8, 1, 1, true),
      () => generateChainFloor(14, 5, 10, 8, 2, 2, true),
      () => generateChainFloor(15, 5, 11, 8, 2, 2, true),
      () => generateChainFloor(16, 5, 11, 8, 3, 3, true),
      () => generateRewardFloor(17, shared.Plants.GetRandomAndDiscount(1f)),
      () => generateEndBossFloor(18),
      () => generateEndFloor(19),
    };
  }
}
