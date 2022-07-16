using static Encounters;

public static class EncounterGroupEverything {
  public static EncounterGroup Everything() {
    return new EncounterGroup() {
      Mobs = new EncounterBag {
        { 1, AFewBlobs },
        { 1, AddClumpshroom },
        { 0.8f, AFewSnails },
        { 0.4f, AddBats },
        { 0.35f, AddSpiders },
        { 0.2f, AddScorpions },
        { 0.2f, AddParasite },
        { 0.2f, AddThistlebog },
        { 0.2f, AddGolems },
        { 0.1f, AddHydra },
        { 0.1f, AddGrasper },
      },

      Grasses = new EncounterBag {
        { 1f, AddVibrantIvy },

        { 0.75f, AddBladegrass },

        { 0.6f, AddSpore },

        { 0.5f, AddSoftGrass },
        { 0.5f, ScatteredBoombugs },

        { 0.4f, AddPoisonmoss },
        { 0.4f, AddWebs },
        { 0.4f, AddViolets },
        { 0.4f, AddHangingVines },

        { 0.35f, AddEveningBells },

        { 0.3f, AddGuardleaf },

        { 0.2f, AddTunnelroot },
        { 0.2f, AddBrambles },
        { 0.2f, AddAgave },
        { 0.2f, AddDeathbloom },

        { 0.05f, FillWithFerns }
      },

      Spice = new EncounterBag {
        { 5f, Empty },

        { 0.5f, AFewBlobs },
        { 0.5f, JackalPile },
        { 0.5f, AFewSnails },

        { 0.25f, AddSoftGrass },
        { 0.25f, AddBladegrass },
        { 0.25f, AddFruitingBodies },
        { 0.25f, AddBloodstone },

        { 0.2f, ScatteredBoombugs },

        // { 0.1f, AddCoralmoss },
        { 0.1f, AddDeathbloom },
        { 0.1f, AddSpiders },
        { 0.1f, AddGuardleaf },
        { 0.1f, AddSpore },
        { 0.1f, AddWater },

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