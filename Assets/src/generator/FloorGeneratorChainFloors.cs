
using System;
using System.Collections.Generic;
using UnityEngine;
using static Generators;

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
      () => generateChainFloor(EncounterGroup, 1 , 7, 6, 5, 1, 1, true),
      () => generateChainFloor(EncounterGroup, 2 , 7, 6, 5, 1, 1, true),
      () => generateChainFloor(EncounterGroup, 3 , 7, 6, 5, 2, 1, true),
      () => generateChainFloor(EncounterGroup, 4 , 7, 6, 6, 2, 1, true),
      () => generateChainFloor(EncounterGroup, 5 , 7, 6, 6, 2, 1, true),
      () => generateChainFloor(EncounterGroup, 6 , 7, 6, 6, 2, 1, true),
      () => generateChainFloor(EncounterGroup, 7 , 7, 7, 6, 3, 1, true),
      () => generateChainFloor(EncounterGroup, 8 , 7, 7, 6, 3, 1, true),
      () => generateChainFloor(EncounterGroup, 9 , 7, 7, 6, 3, 1, true),
      () => generateBlobBossFloor(10),

      // midgame
      () => generateChainFloor(EncounterGroup, 8 , 5, 7, 6, 1, 1, true),
      () => generateChainFloor(EncounterGroup, 9 , 5, 7, 6, 1, 1, true),
      () => generateChainFloor(EncounterGroup, 10 , 5, 7, 6, 2, 2, true),
      () => generateChainFloor(EncounterGroup, 11 , 5, 8, 7, 2, 2, true),
      () => generateChainFloor(EncounterGroup, 12, 5, 8, 7, 3, 3, true),
      // () => generateRewardFloor(13, shared.Plants.GetRandomAndDiscount(1f)),
      () => generateRewardFloor(13, Encounters.AddWater, Encounters.OneAstoria),
      () => generateFungalColonyBossFloor(14),

      // endgame
      () => generateChainFloor(EncounterGroup, 15, 5, 9, 7, 1, 1, true),
      () => generateChainFloor(EncounterGroup, 16, 5, 9, 7, 1, 1, true),
      () => generateChainFloor(EncounterGroup, 17, 5, 9, 7, 2, 2, true),
      () => generateChainFloor(EncounterGroup, 18, 5, 10, 8, 2, 2, true),
      () => generateChainFloor(EncounterGroup, 19, 5, 10, 8, 3, 3, true),
      // () => generateRewardFloor(20, shared.Plants.GetRandomAndDiscount(1f)),
      () => generateRewardFloor(20, Encounters.AddWater, Encounters.OneAstoria),
      () => generateEndBossFloor(21),
      () => generateEndFloor(22),
    };
  }
}
