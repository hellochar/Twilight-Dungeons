using static Encounters;

public static class EncounterGroupEarlyGame {
  public static EncounterGroup EarlyGame() {
    return new EncounterGroup() {
      Mobs = new EncounterBag {
        { 1.0f, AFewBlobs },
        { 1.0f, JackalPile },
        { 1.0f, AddSkullys },
        { 1.0f, AddOctopus },

        { 0.9f, AFewSnails },
        { 0.4f, AddBats },
        { 0.35f, AddSpiders },
      },

      Grasses = new EncounterBag {
        { 1f, AddSoftGrass },

        { 0.6f, AddLlaora },
        { 0.6f, AddGuardleaf },
        { 0.6f, AddBladegrass },

        { 0.5f, ScatteredBoombugs },
        { 0.5f, AddEveningBells },

        { 0.4f, AddDeathbloom },
        { 0.4f, AddWebs },
        { 0.4f, AddAgave },

        { 0.35f, AddHangingVines },

        { 0.2f, AddViolets },
        { 0.2f, FillWithFerns }
      },

      Spice = new EncounterBag {
        { 5f, Empty },

        { 0.5f, AddStalk },
        { 0.5f, AFewBlobs },
        { 0.5f, AFewSnails },
        { 0.5f, AddFruitingBodies },

        { 0.3f, JackalPile },

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