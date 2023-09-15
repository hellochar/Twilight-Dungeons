using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Chases and attacks its target until it dies, then burrows back into the ground.")]
public class Scuttler : AIActor {
  public override float turnPriority => 21;
  public Scuttler(Vector2Int pos) : base(pos) {
    faction = Faction.Neutral;
    hp = baseMaxHp = 1;
  }

  public static bool CanOccupy(Tile t) => t.CanBeOccupied() && t is Ground && t.floor.GetCardinalNeighbors(t.pos).Any(n => n is Wall);

  void BecomeGrass() {
    floor.Put(new ScuttlerUnderground(pos));
    // don't KILL, just remove
    floor.Remove(this);
  }

  protected override ActorTask GetNextTask() {
    if (target == null || target.IsDead) {
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, BecomeGrass));
    }
    var player = GameModel.main.player;
    if (target == player && !CanTargetPlayer()) {
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, BecomeGrass));
    }

    if (IsNextTo(target)) {
      return new AttackTask(this, target);
    } else {
      return new ChaseTargetTask(this, target);
    }
  }

  internal override (int, int) BaseAttackDamage() => (1, 1);

  public Actor target;
  internal Entity Targetting(Actor who) {
    target = who;
    return this;
  }
}

[Serializable]
[ObjectInfo("scuttler-underground", description: "Something lies in wait here. Anything that walks over it will become targeted.")]
public class ScuttlerUnderground : Grass, IActorEnterHandler {
    public override string displayName => "???";
    internal static bool CanOccupy(Tile tile) => tile.CanBeOccupied() && tile is Ground;

  public ScuttlerUnderground(Vector2Int pos) : base(pos) {
  }

  // public void HandleActorLeave(Actor who) {
  //   if (!(who is Scuttler)) {
  //     floor.Put(new Scuttler(pos).Targetting(who));
  //     Kill(who);
  //   }
  // }

  public void HandleActorEnter(Actor who) {
    if (!(who is Scuttler)) {
      floor.Put(new Scuttler(pos).Targetting(who));
      Kill(who);
    }
  }
}

[Serializable]
[ObjectInfo("scuttler")]
internal class ItemScuttler : Item, ITargetedAction<Tile> {
  string ITargetedAction<Tile>.TargettedActionName => "Place";
  string ITargetedAction<Tile>.TargettedActionDescription => "Choose where to place the Scuttler.";
  void ITargetedAction<Tile>.PerformTargettedAction(Player player, Entity target) {
    player.floor.Put(new ScuttlerUnderground(target.pos));
    Destroy();
  }

  IEnumerable<Tile> ITargetedAction<Tile>.Targets(Player player) {
    return player.floor.GetAdjacentTiles(player.pos).Where(ScuttlerUnderground.CanOccupy);
  }
}