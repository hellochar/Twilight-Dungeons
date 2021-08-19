using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Blooms when an adjacent creature dies.\nOnce bloomed, walk over it to obtain a Deathbloom Flower and spawn a new Deathbloom.")]
public class Deathbloom : Grass, IActorEnterHandler, IDeathHandler {
  public bool isBloomed = false;
  [field:NonSerialized] /// controller only
  public event Action OnBloomed;

  public Deathbloom(Vector2Int pos) : base(pos) {}

  [OnDeserialized]
  protected override void HandleEnterFloor() {
    floor.OnEntityRemoved += HandleEntityRemoved;
  }

  protected override void HandleLeaveFloor() {
    floor.OnEntityRemoved -= HandleEntityRemoved;
  }

  private void HandleEntityRemoved(Entity entity) {
    if (entity is Actor a && a.IsDead && a.IsNextTo(this) && !(a is BoombugCorpse)) {
      isBloomed = true;
      OnBloomed?.Invoke();
    }
  }

  public void HandleActorEnter(Actor actor) {
    if (isBloomed) {
      if (actor is Player p) {
        var noGrassTiles = floor.GetAdjacentTiles(pos).Where((tile) => tile is Ground && tile.grass == null).ToList();
        noGrassTiles.Shuffle();
        foreach (var tile in noGrassTiles.Take(1)) {
          var newDeathbloom = new Deathbloom(tile.pos);
          floor.Put(newDeathbloom);
        }
        Kill(p);
      }
    }
  }

  public void HandleDeath(Entity source) {
    if (isBloomed) {
      var player = GameModel.main.player;
      if (player.pos == pos) {
        // player is over the deathbloom; try putting it into player inventory
        var item = new ItemDeathbloomFlower(1);
        if (!player.inventory.AddItem(item, this)) {
          floor.Put(new ItemOnGround(pos, item, pos));
        }
      }
    }
  }
}

[Serializable]
internal class ItemDeathbloomFlower : Item, IStackable, IEdible {
  public ItemDeathbloomFlower(int stacks) {
    this.stacks = stacks;
  }
  public int stacksMax => 10;

  private int _stacks;
  public int stacks {
    get => _stacks;
    set {
      if (value < 0) {
        throw new ArgumentException("Setting negative stack!" + this + " to " + value);
      }
      _stacks = value;
      if (_stacks == 0) {
        Destroy();
      }
    }
  }

  public void Eat(Actor a) {
    a.statuses.RemoveOfType<WeaknessStatus>();
    a.statuses.Add(new FrenziedStatus(3));
    stacks--;
  }

  internal override string GetStats() => "Eat to become Frenzied, providing +2 attack damage for 3 turns. Afterwards, gain 3 stacks of Weakness.\nEating Deathbloom also removes Weakness.";
}

[System.Serializable]
[ObjectInfo(spriteName: "3Red", flavorText: "You're engulfed in a rage!")]
internal class FrenziedStatus : StackingStatus, IAttackDamageModifier {
  public override StackingMode stackingMode => StackingMode.Add;
  public FrenziedStatus(int turnsLeft) {
    this.stacks = turnsLeft;
  }

  public override void End() {
    actor.statuses.Add(new WeaknessStatus(3));
  }

  public override void Step() {
    stacks--;
  }

  public override string Info() => $"Deal +2 attack damage for {this.stacks} more turns.\nWhen Frenzied ends, gain Weakness, dealing -1 damage on your next three attacks.";

  public int Modify(int input) {
    return input + 2;
  }
}

[System.Serializable]
[ObjectInfo(spriteName: "weakness", flavorText: "Your muscles are failing you!")]
internal class WeaknessStatus : StackingStatus, IAttackDamageModifier {
  public override bool isDebuff => true;
  public WeaknessStatus(int stacks) : base(stacks) {}

  public override string Info() => $"Your next {stacks} attacks deal -1 damage!\nCan be Removed by eating a Deathbloom Flower.";

  public int Modify(int input) {
    stacks--;
    return input - 1;
  }
}