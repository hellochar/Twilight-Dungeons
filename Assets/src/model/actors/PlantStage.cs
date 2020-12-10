using System;
using UnityEngine;

public class PlantStage {
  public Plant plant;

  /// set when the plant actually enters into this stage
  public float ageEntered { get; private set; }
  /// how long the plant has been in this stage specifically
  public float age => plant.age - ageEntered;
  public string name => GetType().Name;

  public PlantStage NextStage { get; set; }

  public void BindTo(Plant plant) {
    if (this.plant != null) {
      throw new Exception("Trying to bind an already bount Plant Stage!");
    }
    if (plant.stage != this) {
      throw new Exception("Plant's stage isn't this stage!");
    }
    this.plant = plant;
    ageEntered = plant.age;
  }

  public virtual float Step() {
    return 1;
  }

  public virtual string getUIText() { return ""; }
}

class Seed : PlantStage {
  public override float Step() {
    if (this.age >= 500) {
      plant.stage = NextStage;
    }
    return 1;
  }

  public override string getUIText() => $"Grows in {500 - this.age} turns.";
}

class Young : PlantStage {
  public override float Step() {
    if (this.age >= 500) {
      plant.stage = NextStage;
    }
    return 1;
  }

  public override string getUIText() => $"Grows in {500 - this.age} turns.";
}

class Sapling : PlantStage {
  public override float Step() {
    if (this.age >= 500) {
      plant.stage = NextStage;
    }
    return 1;
  }

  public override string getUIText() => $"Grows in {500 - this.age} turns.";
}