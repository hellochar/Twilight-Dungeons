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
        p.inventory.AddItem(new ItemDeathbloomFlower());
      }
      var noGrassTiles = floor.GetAdjacentTiles(pos).Where((tile) => tile is Ground && tile.grass == null).ToList();
      noGrassTiles.Shuffle();
      foreach (var tile in noGrassTiles.Take(2)) {
        var newDeathbloom = new Deathbloom(tile.pos);
        floor.Add(newDeathbloom);
      }
      Kill();
    }
  }
}

internal class ItemDeathbloomFlower : Item {

  [PlayerAction]
  public void Eat(Actor a) {
    a.statuses.Add(new FrenziedStatus(a, 7));
  }
}

internal class FrenziedStatus : Status, IAttackDamageModifier {
  private Actor actor;
  private int turnsLeft;

  public FrenziedStatus(Actor actor, int turnsLeft) {
    this.actor = actor;
    this.turnsLeft = turnsLeft;
  }

  public override void Step() {
    turnsLeft--;
    if (turnsLeft <= 0) {
      GameModel.main.EnqueueEvent(() => actor.statuses.Remove(this));
    }
  }

  public override string Info() => $"You deal +2 damage.\n{this.turnsLeft} turns remaining.";

  public int Modify(int input) {
    return input + 2;
  }
}