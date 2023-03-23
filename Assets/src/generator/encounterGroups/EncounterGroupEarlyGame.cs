using static Encounters;

public static class EncounterGroupEarlyGame {
  public static EncounterGroup EarlyGame() {
    return new EncounterGroup() {
      Mobs = new EncounterBag {
        { 1.0f, AFewBlobs },
        { 1.0f, JackalPile },
        { 1.0f, AFewSnails },
        { 0.3f, AddBird },
        { 0.3f, AddSnake },
        { 0.2f, AddWallflowers },
        { 0.2f, AddSkullys },
        { 0.2f, AddOctopus },
        { 0.2f, AddSpiders },
      },

      Grasses = new EncounterBag {
        { 1f, AddSoftGrass },
        { 1f, AddGuardleaf },
        { 1f, AddBladegrass },

        // { 1f, AddNubs },
        // { 1f, AddRedleaf },

        // { 1f, AddSoftMoss },
        // { 1f, AddPlatelets },
        { 0.2f, AddBloodwort },
        { 0.2f, AddLlaora },
        { 0.2f, AddMushroom },

        // { 0.5f, ScatteredBoombugs },
        { 0.3f, AddEveningBells },

        { 0.3f, AddDeathbloom },
        // { 0.4f, AddWebs },
        { 0.3f, AddAgave },

        { 0.3f, AddHangingVines },

        // { 0.02f, AddViolets },
        { 0.02f, FillWithFerns }
      },

      Spice = new EncounterBag {
        { 10f, Empty },

        { 1f, AddShielders },
        { 1f, AddFruitingBodies },
        { 1f, AddSpore },
        { 1f, AddChillers },

        { 0.25f, AddSoftGrass },
        { 0.25f, AddBladegrass },

        { 0.2f, AFewBlobs },
        { 0.2f, AFewSnails },
        { 0.2f, JackalPile },
        { 0.2f, AddWater },
        { 0.2f, ScatteredBoombugs },

        { 0.1f, AddCoralmoss },
        { 0.1f, AddDeathbloom },
        { 0.1f, AddSpiders },
        { 0.1f, AddGuardleaf },

        { 0.05f, AddEveningBells },
        { 0.05f, AddPoisonmoss },
        { 0.05f, FillWithFerns },

        { 0.01f, AddNecroroot },
      },
    };
  }
}