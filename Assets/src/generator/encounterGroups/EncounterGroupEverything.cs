using static Encounters;

public static class EncounterGroupEverything {
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
        { 0.2f, AddThistlebog },
        { 0.2f, AddGolems },
        { 0.1f, AddHydra },
        { 0.1f, AddGrasper },
      },

      Grasses = new WeightedRandomBag<Encounter> {
        { 1f, AddBladegrass },

        { 0.75f, AddSoftGrass },

        { 0.5f, AddHangingVines },
        { 0.5f, AddPoisonmoss },
        { 0.5f, ScatteredBoombugs },

        { 0.4f, AddGuardleaf },
        { 0.4f, AddSpore },
        { 0.4f, AddWebs },
        { 0.4f, AddViolets },

        { 0.35f, AddEveningBells },

        { 0.2f, AddTunnelroot },
        { 0.2f, AddBrambles },
        { 0.2f, AddAgave },
        { 0.2f, AddDeathbloom },
      },

      Spice = new WeightedRandomBag<Encounter> {
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
        { 0.1f, AddFruitingBodies },

        { 0.05f, AddEveningBells },
        { 0.05f, AddPoisonmoss },
        { 0.05f, AddTunnelroot },
        { 0.05f, AddViolets },
        { 0.05f, AddBrambles },

        { 0.02f, AddNecroroot },
        { 0.02f, AddScorpions },
        { 0.02f, AddParasite },
        { 0.02f, AddGolems },

        { 0.01f, AddHydra },
        { 0.01f, AddGrasper },
      },
    };
  }
}