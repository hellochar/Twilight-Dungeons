using System;
using UnityEngine;

public abstract class PlantStage {
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

  public abstract float StepTime { get; }
  public abstract float Step();

  public virtual string getUIText() { return ""; }
}

class Seed : PlantStage {
  public override float StepTime => 500;
  public override float Step() {
    plant.stage = NextStage;
    return StepTime;
  }

  public override string getUIText() => $"Grows in {StepTime - this.age} turns.";
}

class Young : PlantStage {
  public override float StepTime => 500;
  public override float Step() {
    plant.stage = NextStage;
    return StepTime;
  }

  public override string getUIText() => $"Grows in {StepTime - this.age} turns.";
}

class Sapling : PlantStage {
  public override float StepTime => 500;
  public override float Step() {
    plant.stage = NextStage;
    return StepTime;
  }

  public override string getUIText() => $"Grows in {StepTime - this.age} turns.";
}