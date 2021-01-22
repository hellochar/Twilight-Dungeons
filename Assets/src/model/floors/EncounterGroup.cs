using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using static Encounters;

public class EncounterGroup {
  public WeightedRandomBag<Encounter> Mobs;
  public WeightedRandomBag<Encounter> Walls;
  public WeightedRandomBag<Encounter> Grasses;
  public WeightedRandomBag<Encounter> DeadEnds;
  public WeightedRandomBag<Encounter> Rewards;
  public WeightedRandomBag<Encounter> Plants = PlantsStatic;

  public static WeightedRandomBag<Encounter> PlantsStatic = new WeightedRandomBag<Encounter> {
    { 1f, MatureBerryBush },
    { 1f, MatureThornleaf },
    { 1f, MatureWildWood },
    { 1f, MatureWeirdwood },
    { 1f, MatureKingshroom }
  };

  public static EncounterGroup Everything() {
    return new EncounterGroup() {
      Mobs = new WeightedRandomBag<Encounter> {
        { 1, AFewBlobs },
        { 1, JackalPile },
        { 1, AFewSnails },
        { 0.4f, AddBats },
        { 0.35f, AddSpiders },
        { 0.2f, AddScorpions },
        { 0.2f, AddParasite },
        { 0.2f, AddGolems },
        { 0.1f, AddHydra },
      },
      Walls = new WeightedRandomBag<Encounter> {
    { 3f, Empty },
    { 0.5f, WallPillars },
    { 0.5f, ChunkInMiddle },
    { 0.5f, LineWithOpening },
  },
      Grasses = new WeightedRandomBag<Encounter> {
    { 1f, AddSoftGrass },

    { 0.75f, AddBladegrass },

    { 0.5f, AddAgave },
    // { 0.5f, AddCoralmoss },
    { 0.5f, AddHangingVines },

    { 0.4f, AddEveningBells },
    { 0.4f, AddGuardleaf },
    { 0.4f, AddSpore },
    { 0.4f, AddWebs },
    { 0.4f, ScatteredBoombugs },

    { 0.2f, AddPoisonmoss },
    { 0.2f, AddDeathbloom },
  },

      DeadEnds = new WeightedRandomBag<Encounter> {
    /// just to make it interesting, always give dead ends *something*
    { 5f, Empty },

    { 0.5f, AFewBlobs },
    { 0.5f, JackalPile },
    { 0.5f, AFewSnails },

    { 0.25f, AddSoftGrass },
    { 0.25f, AddBladegrass },

    { 0.2f, AddWater },
    { 0.2f, ScatteredBoombugs },

    // { 0.1f, AddCoralmoss },
    { 0.1f, AddDeathbloom },
    { 0.1f, AddSpiders },
    { 0.1f, AddGuardleaf },
    { 0.1f, AddSpore },

    { 0.05f, AddEveningBells },
    { 0.05f, AddPoisonmoss },

    { 0.02f, AddScorpions },
    { 0.02f, AddParasite },
    { 0.02f, AddGolems },

    { 0.01f, AddHydra },
  },

      Rewards = new WeightedRandomBag<Encounter> {
    // { 2f, AddWater },
    { 1f, AddMushroom },
    { 1f, AddPumpkin },
    { 1f, OneAstoria },
    { 0.5f, AddJackalHide },
    { 0.5f, AddGloopShoes },
    { 0.5f, OneButterfly },
    { 0.5f, ThreeAstoriasInCorner },
  },
    };
  }
}