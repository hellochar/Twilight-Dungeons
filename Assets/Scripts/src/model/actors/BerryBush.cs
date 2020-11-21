using UnityEngine;

public class BerryBush : Actor {
  class Seed : PlantStage<BerryBush> {
    public Seed(BerryBush plant) : base(plant) { }
    public override void Step() {
      if (this.age >= 500) {
        plant.currentStage = new Young(plant);
      }
    }

    public override string getUIText() => $"Grows in {15 - this.age} turns.";
  }

  class Young : PlantStage<BerryBush> {
    public Young(BerryBush plant) : base(plant) { }
    public override void Step() {
      if (this.age >= 500) {
        plant.currentStage = new Mature(plant);
      }
    }

    public override string getUIText() => $"Grows in {15 - this.age} turns.";
  }

  class Mature : PlantStage<BerryBush> {
    public int numBerries = 0;
    public Mature(BerryBush plant) : base(plant) { }

    public override void Step() {
      int turnsPerBerry = 300;
      /// TODO this only works because Step() gets called for every single consecutive age.
      if (this.age >= turnsPerBerry && this.age % turnsPerBerry == 0) {
        numBerries++;
      }
    }

    public override string getUIText() => $"Contains {numBerries} berries.";
  }

  public PlantStage<BerryBush> currentStage;
  internal override float turnPriority => 40;
  public string displayName => $"Berry Bush ({currentStage.name})";

  public BerryBush(Vector2Int pos) : base(pos) {
    faction = Faction.Ally;
    currentStage = new Seed(this);
  }

  public override void Step() {
    this.currentStage.Step();
    this.timeNextAction += baseActionCost;
  }

  internal void Harvest() {
    Player player = GameModel.main.player;
    var stacks = currentStage is Mature ? ((Mature) currentStage).numBerries : 0;
    if (stacks > 0) {
      var item = new ItemBerries(stacks);
      player.inventory.AddItem(item);
      ((Mature) currentStage).numBerries = 0;
    }
  }

  internal void Cull() {
    Harvest();
    Player player = GameModel.main.player;
    player.inventory.AddItem(new ItemSeed());
    player.inventory.AddItem(new ItemSeed());
    Kill();
  }
}

public class PlantStage<T> where T : Actor {
  public T plant;
  public float ageEntered { get; }
  /// how long the plant has been in this stage specifically
  public float age => plant.age - ageEntered;
  public string name => GetType().Name;

  public PlantStage(T plant) {
    this.plant = plant;
    this.ageEntered = plant.age;
  }

  public virtual void Step() { }

  public virtual string getUIText() { return ""; }
}