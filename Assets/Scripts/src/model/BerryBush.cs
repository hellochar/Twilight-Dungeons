using UnityEngine;

public class BerryBush : Actor {
  class Seed : PlantStage<BerryBush> {
    public Seed(BerryBush plant) : base(plant) { }
    public override void Step() {
      if (this.age >= 15) {
        plant.currentStage = new Young(plant);
      }
    }

    public override string getUIText() => $"Grows in {15 - this.age} turns.";
  }

  class Young : PlantStage<BerryBush> {
    public Young(BerryBush plant) : base(plant) { }
    public override void Step() {
      if (this.age >= 15) {
        plant.currentStage = new Mature(plant);
      }
    }

    public override string getUIText() => $"Grows in {15 - this.age} turns.";
  }

  class Mature : PlantStage<BerryBush> {
    public int numBerries = 0;
    public Mature(BerryBush plant) : base(plant) { }

    public override void Step() {
      int turnsPerBerry = 15;
      /// TODO this only works because Step() gets called for every single consecutive age.
      if (this.age >= turnsPerBerry && this.age % turnsPerBerry == 0) {
        numBerries++;
      }
    }

    public override string getUIText() => $"Contains {numBerries} berries.";
  }

  public PlantStage<BerryBush> currentStage;

  internal override float queueOrderOffset => 0.4f;

  public string displayName => $"Berry Bush ({currentStage.name})";

  public BerryBush(Vector2Int pos) : base(pos) {
    currentStage = new Seed(this);
    Debug.Log("Creating " + this);
  }

  public override void Step() {
    this.currentStage.Step();
    this.timeNextAction += baseActionCost;
  }

  public override void CatchUpStep(int time) {
    Debug.Log("catching up " + this + " from " + this.timeNextAction + " to " + time);
    while (this.timeNextAction < time) {
      this.Step();
    }
  }

  internal void Harvest() {
    floor.RemoveActor(this);
  }
}

public class PlantStage<T> where T : Actor {
  public T plant;
  public int ageEntered { get; }
  /// how long the plant has been in this stage specifically
  public int age { get => plant.age - ageEntered; }
  public string name { get => GetType().Name; }

  public PlantStage(T plant) {
    this.plant = plant;
    this.ageEntered = plant.age;
  }

  public virtual void Step() { }

  public virtual string getUIText() { return ""; }
}