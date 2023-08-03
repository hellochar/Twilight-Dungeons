using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

// usage: Bind PlantStage to a Plant, then set its XP. Once it gets its needed XP the PlantStage will 
// call GoNextStage() on the plant.
[Serializable]
public abstract class PlantStage {
  public Plant plant;

  public int xp {
    get => _xp;
    set {
      if (xpNeeded > 0) {
        _xp = value;
        if (_xp >= xpNeeded) {
          GoNextStage();
        }
      }
    }
  }
  private int _xp;
  public int xpNeeded { get; private set; }

  public float percentGrown => (float) xp / xpNeeded;
  public string name => GetType().Name;
  public List<Inventory> harvestOptions = new List<Inventory>();

  protected PlantStage(int xpNeeded) {
    this.xpNeeded = xpNeeded;
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

  private void GoNextStage() => plant?.GoNextStage();
}

[Serializable]
class Seed : PlantStage {
  public Seed(int xpNeeded = 3) : base(xpNeeded) {}
}

[Serializable]
class MaturePlantStage : PlantStage {
  public MaturePlantStage() : base(-1) {}
}