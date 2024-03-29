using static Encounters;

public static class EncounterGroupEverything {
  public static EncounterGroup Everything() {
    return new EncounterGroup() {
      Mobs = new EncounterBag {
        { 1f, AddFungalBreeder },
        { 1f, AddFungalSentinel },
        { 1f, AddBats },
        { 1f, AddSpiders },
        { 1f, AddScorpions },
        { 1f, AddThistlebog },
        { 1f, AddGolems },
        { 0.5f, AddClumpshroom },
        { 0.5f, AddGrasper },
        { 0.5f, AddParasite },
        { 0.2f, AddHydra },
      },

      Grasses = new EncounterBag {
        { 1f, AddVibrantIvy },
        { 1f, AddSpore },
        { 1f, AddBloodwort },

        { 0.75f, AddBladegrass },
        // { 0.75f, AddCheshireWeeds },

        { 0.5f, AddTunnelroot },
        { 0.5f, AddBrambles },
        { 0.5f, AddSoftGrass },
        { 0.5f, ScatteredBoombugs },
        { 0.5f, AddDeathbloom },
        { 0.5f, AddGuardleaf },

        { 0.4f, AddPoisonmoss },
        { 0.4f, AddWebs },
        { 0.4f, AddViolets },
        { 0.4f, AddHangingVines },

        { 0.35f, AddEveningBells },


        { 0.2f, AddAgave },

        { 0.05f, FillWithFerns }
      },

      Spice = new EncounterBag {
        { 5f, Empty },

        { 0.25f, AFewBlobs },
        { 0.25f, JackalPile },
        { 0.25f, AFewSnails },

        { 0.25f, AddScuttlers },

        { 0.25f, AddFruitingBodies },
        { 0.25f, AddBloodstone },

        // { 0.1f, AddCoralmoss },
        { 0.1f, AddWater },

        { 0.05f, AddEveningBells },
        { 0.05f, AddPoisonmoss },
        { 0.05f, AddTunnelroot },

        { 0.02f, AddFaegrass },
        { 0.02f, AddNecroroot },

        { 0.01f, AddHydra },
      },
    };
  }
}