using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
public abstract class PlantStage {
  public Plant plant;

  public int daysToGrow { get; private set; }
  public int daysGrown { get; private set; }
  public float percentGrown => (float)daysGrown / daysToGrow;
  public string name => GetType().Name;
  public List<Inventory> harvestOptions = new List<Inventory>();

  protected PlantStage(int daysToGrow = 2) {
    this.daysToGrow = daysToGrow;
  }

  public PlantStage NextStage { get; set; }

  public virtual void BindTo(Plant plant) {
    if (this.plant != null) {
      throw new Exception("Trying to bind an already bount Plant Stage!");
    }
    if (plant.stage != this) {
      throw new Exception("Plant's stage isn't this stage!");
    }
    this.plant = plant;
  }

  internal void StepDay() {
    daysGrown++;
    if (daysGrown >= daysToGrow) {
      GoNextStage();
    }
  }

  internal void GoNextStage() => plant.GoNextStage();
}

[Serializable]
class Seed : PlantStage {
  public Seed(int daysToGrow = 2) : base(daysToGrow) {
  }
}

[Serializable]
class Young : PlantStage {
}

[Serializable]
class Sapling : PlantStage {
}