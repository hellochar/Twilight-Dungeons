using System;
using System.Linq;
using UnityEngine;

[Serializable]
[PlantConfig(FloorsToMature = 2, WaterCost = 50)]
public class Faeleaf : Plant {
  [Serializable]
  class Mature : MaturePlantStage {
    public override void BindTo(Plant plant) {
      base.BindTo(plant);
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Faeleaf), 2)
      ));
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Faeleaf), 1),
        new ItemFaegrass(3)
      ));
      harvestOptions.Add(new Inventory(
        new ItemSeed(typeof(Faeleaf), 1),
        new ItemBirdWings(5)
      ));
      harvestOptions.Add(new Inventory(
        new ItemWallflowerTendril()
      ));
    }
  }
  public Faeleaf(Vector2Int pos) : base(pos) {
    stage.NextStage = new Mature();
  }
}

[Serializable]
public class ItemFaegrass : Item, IStackable, IUsable {
  public ItemFaegrass(int stacks) {
    this.stacks = stacks;
  }
  public int stacksMax => 100;

  internal override string GetStats() => "Use to disperse Faegrass randomly across the Level.\n\nFaegrass teleports creatures that are attacked while standing over it.";

  private int _stacks;
  public int stacks {
    get => _stacks;
    set {
      if (value < 0) {
        throw new System.ArgumentException("Setting negative stack!" + this + " to " + value);
      }
      _stacks = value;
      if (_stacks == 0) {
        Destroy();
      }
    }
  }

  public void Use(Actor a) {
    if (a.floor.isCleared) {
      throw new CannotPerformActionException("Use when enemies are nearby!");
    }
    Encounters.AddFaegrassImpl(a.floor, 12);
    stacks--;
  }
}

[Serializable]
[ObjectInfo("faegrass", description: "When a creature standing the Faegrass takes damage, it will get teleported to a random location and consume the Faegrass.")]
class Faegrass : Grass, ITakeAnyDamageHandler {
  public static bool CanOccupy(Tile t) => t.CanBeOccupied() && t is Ground;
  public Faegrass(Vector2Int pos) : base(pos) {
    this.BodyModifier = this;
  }

  public void HandleTakeAnyDamage(int damage) {
    if (body is Actor a) {
      var randomPos = Util.RandomPick(floor.tiles.Where(t => t.CanBeOccupied())).pos;
      a.pos = randomPos;
      FloorController.current.PlayVFX("FaeTeleport", a);
      Kill(a);
      if (a is AIActor) {
        a.statuses.Add(new SurprisedStatus());
      }
    }
  }
}