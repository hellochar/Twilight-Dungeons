using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Deathbloom : Grass {
  public bool isBloomed = false;
  public event Action OnBloomed;

  public Deathbloom(Vector2Int pos) : base(pos) {
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
  }

  private void HandleEnterFloor() {
    floor.OnEntityRemoved += HandleEntityRemoved;
    tile.OnActorEnter += HandleActorEnter;
  }

  private void HandleLeaveFloor() {
    floor.OnEntityRemoved -= HandleEntityRemoved;
    tile.OnActorEnter -= HandleActorEnter;
  }

  private void HandleEntityRemoved(Entity entity) {
    if (entity is Actor a && a.IsDead && a.IsNextTo(this)) {
      isBloomed = true;
      OnBloomed?.Invoke();
    }
  }

  private void HandleActorEnter(Actor actor) {
    if (isBloomed) {
      if (actor is Player p) {
        p.inventory.AddItem(new ItemDeathbloomFlower(1), this);
      }
      var noGrassTiles = floor.GetAdjacentTiles(pos).Where((tile) => tile is Ground && tile.grass == null).ToList();
      noGrassTiles.Shuffle();
      foreach (var tile in noGrassTiles.Take(1)) {
        var newDeathbloom = new Deathbloom(tile.pos);
        floor.Put(newDeathbloom);
      }
      Kill();
    }
  }
}

internal class ItemDeathbloomFlower : Item, IStackable, IEdible {
  public ItemDeathbloomFlower(int stacks) {
    this.stacks = stacks;
  }
  public int stacksMax => 5;

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
    a.statuses.Add(new FrenziedStatus(10));
    stacks--;
  }
}

internal class FrenziedStatus : StackingStatus, IAttackDamageModifier {
  public FrenziedStatus(int turnsLeft) {
    this.stacks = turnsLeft;
  }

  public override void Step() {
    stacks--;
  }

  public override string Info() => $"You deal +2 damage.\n{this.stacks} turns remaining.";

  public int Modify(int input) {
    return input + 2;
  }
}