using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Shoots out a long, snaking Tendril that surrounds you.\nIf you are next to three or more Tendrils, Grasper deals 3 attack damage.")]
public class Grasper : AIActor, IBaseActionModifier {
  public readonly List<Tendril> tendrils = new List<Tendril>();

  public Grasper(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 6;
  }

#nullable enable
  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;
    var tendrilsSurroundingPlayer = GetContiguousTendrilsSurroundingPlayer(3);
    if (tendrilsSurroundingPlayer != null) {
      foreach (var t in tendrilsSurroundingPlayer) {
        t.OnPulse();
      }
      return new GenericTask(this, DamagePlayer);
    }
    if (GetNextTendrilTile() == null) {
      return new WaitTask(this, 1);
    } else {
      return new GenericTask(this, SpawnTendril);
    }
  }

  private List<Tendril>? GetContiguousTendrilsSurroundingPlayer(int minLength) {
    for (int i = 0; i < tendrils.Count; i++) {
      var run = FindContiguousRunSurroundingPlayer(i);
      if (run.Count >= minLength) {
        return run;
      }
    }
    return null;
  }

  private List<Tendril> FindContiguousRunSurroundingPlayer(int startIndex) {
    List<Tendril> list = new List<Tendril>();

    int index = startIndex;
    while (index < tendrils.Count && tendrils[index].IsNextTo(GameModel.main.player)) {
      list.Add(tendrils[index]);
      index++;
    }
    return list;
  }

  private void DamagePlayer() {
    var player = GameModel.main.player;
    player.TakeAttackDamage(3, this);
  }

  Tile GetNextTendrilTile() {
    var lastNodePos = tendrils.LastOrDefault()?.pos ?? pos;
    var target = GameModel.main.player.pos;

    return floor.GetCardinalNeighbors(lastNodePos)
      .Where((t) => t.CanBeOccupied())
      .OrderBy(t => t.DistanceTo(target))
      .FirstOrDefault();
  }

  private void SpawnTendril() {
    if (GetNextTendrilTile() != null) {
      var tendril = new Tendril(GetNextTendrilTile().pos, this);
      tendrils.Add(tendril);
      floor.Put(tendril);
      tendril.ClearTasks();
    }
  }

  // take great care - this can be called after Grasper is dead
  internal void TendrilDied(Tendril tendril, Entity source) {
    if (source == this) {
      return;
    }

    if (!IsDead) {
      statuses.Add(new SurprisedStatus());
    }

    // find the tendril along the length
    var index = tendrils.IndexOf(tendril);
    if (index == -1) {
      Debug.LogError("couldn't find tendril");
    }
    // kill all tendrils past this one
    var nextTendrils = tendrils.Skip(index).ToList();
    foreach (var nextTendril in nextTendrils) {
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
[ObjectInfo(description: "If you next to 3 or more Tendrils, the Grasper deals 3 attack damage a turn.\nKilling a Tendril kills descendant Tendrils.")]
public class Tendril : Actor, IDeathHandler, IBaseActionModifier, INoTurnDelay {
  [field:NonSerialized] /// controller only
  public Action OnPulse = delegate {};
  private Actor target;
  public readonly Grasper owner;

  public Tendril(Vector2Int pos, Grasper owner) : base(pos) {
    this.owner = owner;
    faction = Faction.Neutral;
    hp = baseMaxHp = 3;
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
interface INoTurnDelay : ISteppable {}