using System;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Chases and attacks its target until it dies.")]
public class Chiller : AIActor {
  public override float turnPriority => 21;
  public Chiller(Vector2Int pos) : base(pos) {
    faction = Faction.Neutral;
    hp = baseMaxHp = 1;
  }

  public static bool CanOccupy(Tile t) => t.CanBeOccupied() && t is Ground && t.floor.GetCardinalNeighbors(t.pos).Any(n => n is Wall);

  void BecomeGrass() {
    floor.Put(new ChillerGrass(pos));
    KillSelf();
  }

  protected override ActorTask GetNextTask() {
    if (target == null || target.IsDead) {
      BecomeGrass();
    }
    var player = GameModel.main.player;
    if (target == player && !CanTargetPlayer()) {
      BecomeGrass();
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
[ObjectInfo("Ground1_0", description: "A creature lies in wait here. Anything that walks over it will become targeted.")]
public class ChillerGrass : Grass, IActorLeaveHandler {
  public ChillerGrass(Vector2Int pos) : base(pos) {
  }

  public void HandleActorLeave(Actor who) {
    if (!(who is Chiller)) {
      floor.Put(new Chiller(pos).Targetting(who));
      Kill(who);
    }
  }
}