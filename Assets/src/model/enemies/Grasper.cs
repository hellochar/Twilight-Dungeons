using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Shoots out a long, snaking Tendril that chases and surrounds you.\nIf you are surrounded on all sides by Tendrils, Grasper deals 2 attack damage per turn.")]
public class Grasper : AIActor, IBaseActionModifier {
  public readonly List<Tendril> tendrils = new List<Tendril>();

  public Grasper(Vector2Int pos) : base(pos) {
    faction = Faction.Neutral;
    hp = baseMaxHp = 10;
  }

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;
    var tendrilsSurroundingPlayer = floor.AdjacentActors(player.pos).Where((a) => a is Tendril || a is Grasper);
    var isPlayerSurrounded = tendrilsSurroundingPlayer.Count() >= 8;
    if (isPlayerSurrounded) {
      foreach (var t in tendrilsSurroundingPlayer.Where(t => t is Tendril).Cast<Tendril>()) {
        t.OnPulse();
      }
      return new GenericTask(this, DamagePlayer);
    }
    return new GenericTask(this, SpawnTendril);
  }

  private void DamagePlayer() {
    var player = GameModel.main.player;
    player.TakeAttackDamage(2, this);
  }

  private void SpawnTendril() {
    var lastNodePos = tendrils.LastOrDefault()?.pos ?? pos;
    var target = GameModel.main.player.pos;
    // var path = floor.FindPath(lastNodePos, target, true);
    // if (path == null || path.Count == 0) {
    //   // find a new path
    //   SetNewTarget();
    //   return;
    // }
    var nextTile = floor
      .GetCardinalNeighbors(lastNodePos)
      .Where((t) => t.CanBeOccupied())
      .OrderBy(t => t.DistanceTo(target))
      .FirstOrDefault();
    if (nextTile != null) {
      var tendril = new Tendril(nextTile.pos, this);
      tendrils.Add(tendril);
      floor.Put(tendril);
      tendril.ClearTasks();
    }
  }

  internal void TendrilDied(Tendril tendril, Entity source) {
    if (source == this) {
      return;
    }

    statuses.Add(new SurprisedStatus());

    // find the tendril along the length
    var index = tendrils.IndexOf(tendril);
    if (index == -1) {
      Debug.LogError("couldn't find tendril");
    }
    // kill all tendrils past this one
    foreach (var nextTendril in tendrils.Skip(index)) {
      nextTendril.Kill(this);
    }
    tendrils.RemoveRange(index, tendrils.Count - index);
  }

  public BaseAction Modify(BaseAction input) {
    if (input.Type == ActionType.ATTACK || input.Type == ActionType.MOVE) {
      // disallow attack/move
      return new WaitBaseAction(input.actor);
    }
    return input;
  }
}

[Serializable]
[ObjectInfo(description: "If you are surrounded on all sides by Tendrils, take 2 damage a turn.\nKilling one will also kill all descendant Tendrils.")]
public class Tendril : Actor, IDeathHandler, IBaseActionModifier, IIgnoreStagger {
  [field:NonSerialized] /// controller only
  public Action OnPulse = delegate {};
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.GENERIC] = 4,
  };
  protected override ActionCosts actionCosts => StaticActionCosts;

  private Actor target;
  public readonly Grasper owner;

  public Tendril(Vector2Int pos, Grasper owner) : base(pos) {
    this.owner = owner;
    faction = Faction.Neutral;
    hp = baseMaxHp = 5;
    timeNextAction += 999999;
  }

  public override float Step() {
    return 999999;
  }

  public void HandleDeath(Entity source) {
    owner.TendrilDied(this, source);
  }

  public BaseAction Modify(BaseAction input) {
    if (input.Type == ActionType.ATTACK || input.Type == ActionType.MOVE) {
      // disallow attack/move
      return new WaitBaseAction(input.actor);
    }
    return input;
  }
}

/// <summary>Don't stagger the turn manager for this steppable.
interface IIgnoreStagger : ISteppable {}