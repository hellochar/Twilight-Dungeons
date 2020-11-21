using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

/// An actor whose actions are controlled by some sort of AI.
/// This AI decides what actions the actor takes.
/// TODO we should use composition for this instead, eventually
public class AIActor : Actor {
  protected IEnumerator<ActorAction> ai;
  public AIActor(Vector2Int pos) : base(pos) {
    OnPreStep += HandlePreStep;
  }

  void HandlePreStep() {
    if (action == null) {
      do {
        ai.MoveNext();
      } while (ai.Current.IsDone());
      SetActions(ai.Current);
    }
  }
}

public class Bat : AIActor {
  public Bat(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    ai = AIs.BatAI(this).GetEnumerator();
  }
}

// run fast, fear other jackals nearby when they die
public class Jackal : AIActor {
  public static new IDictionary<Type, float> ActionCosts = new ReadOnlyDictionary<Type, float>(
    new Dictionary<Type, float>(Actor.ActionCosts) {
      {typeof(FollowPathAction), 0.67f},
    }
  );

  public override IDictionary<Type, float> actionCosts => Jackal.ActionCosts;
  public Jackal(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = hpMax = 3;
    ai = AIs.JackalAI(this).GetEnumerator();
    OnDeath += HandleDeath;
  }

  private void HandleDeath() {
    foreach (var jackal in floor.ActorsInCircle(pos, 4).Where((actor) => actor is Jackal)) {
      jackal.SetActions(new RunAwayAction(jackal, pos, 5));
    }
  }
}

public class RunAwayAction : ActorAction {
  private Vector2Int fearPoint;
  public int turns;

  public RunAwayAction(Actor a, Vector2Int fearPoint, int turns) : base(a) {
    this.fearPoint = fearPoint;
    this.turns = turns;
  }

  public override void Perform() {
    turns--;
    var adjacentTiles = actor.floor.GetNeighborhoodTiles(actor.pos).Where((tile) => tile.CanBeOccupied());
    var furthestTile = adjacentTiles.Aggregate((t1, t2) =>
      Vector2Int.Distance(fearPoint, t1.pos) > Vector2Int.Distance(fearPoint, t2.pos) ? t1 : t2);
    actor.pos = furthestTile.pos;
  }

  public override bool IsDone() {
    return turns <= 0;
  }
}

/// Don't do anything until the Player's in view
class SleepAction : ActorAction {
  public SleepAction(Actor actor) : base(actor) {
  }

  public override void Perform() {
    bool canSeePlayer = actor.currentTile.visibility == TileVisiblity.Visible;
    if (!canSeePlayer) {
      return;
    } else {
      base.Perform();
    }
  }
}

public static class AIs {
  public static IEnumerable<ActorAction> BatAI(Actor actor) {
    while (true) {
      bool canSeePlayer = actor.currentTile.visibility == TileVisiblity.Visible;
      // hack - start attacking you once the player has vision
      if (canSeePlayer) {
        if (actor.IsNextTo(GameModel.main.player)) {
          yield return new AttackGroundAction(actor, GameModel.main.player.pos, 1);
        } else {
          yield return new ChaseTargetAction(actor, GameModel.main.player);
        }
      } else {
        yield return new MoveRandomlyAction(actor);
      }
    }
  }

  public static IEnumerable<ActorAction> JackalAI(Actor actor) {
    yield return new SleepAction(actor);
    while (true) {
      bool canSeePlayer = actor.currentTile.visibility == TileVisiblity.Visible;
      if (canSeePlayer) {
        yield return new ChaseTargetAction(actor, GameModel.main.player);
        yield return new AttackAction(actor, GameModel.main.player);
      } else {
        yield return new MoveRandomlyAction(actor);
      }
    }
  }
}