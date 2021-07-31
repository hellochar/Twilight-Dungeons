using static Encounters;

public static class EncounterGroupEarlyGame {
  public static EncounterGroup EarlyGame() {
    return new EncounterGroup() {
      Mobs = new EncounterBag {
        { 1, AFewBlobs },
        { 1, JackalPile },
        { 1, AFewSnails },
        { 0.4f, AddBats },
        { 0.35f, AddSpiders },
      },

      Grasses = new EncounterBag {
        { 1f, AddSoftGrass },

        { 0.75f, AddBladegrass },

        { 0.5f, AddGuardleaf },
        { 0.5f, ScatteredBoombugs },
        { 0.5f, AddEveningBells },
        { 0.5f, AddDeathbloom },

        { 0.4f, AddWebs },
        // { 0.4f, AddSpore },

        { 0.35f, AddAgave },
        { 0.35f, AddHangingVines },

        // { 0.2f, AddPoisonmoss },
        // { 0.2f, AddViolets },
        { 0.2f, FillWithFerns }
      },

      Spice = new EncounterBag {
        { 5f, Empty },

        { 0.5f, AFewBlobs },
        { 0.5f, JackalPile },
        { 0.5f, AFewSnails },
        { 0.5f, AddFruitingBodies },

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
        { 0.05f, FillWithFerns },

        { 0.01f, AddNecroroot },
      },
    };
  }
}