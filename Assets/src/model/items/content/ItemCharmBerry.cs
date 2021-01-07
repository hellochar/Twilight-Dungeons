using System;
using System.Collections.Generic;
using System.Linq;

[ObjectInfo(spriteName: "charmberry", flavorText: "It's tart, sour and sweet! Loved by creatures of all sorts.")]
public class ItemCharmBerry : Item, IStackable {
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

  public ItemCharmBerry(int stacks) {
    this.stacks = stacks;
  }

  public void Charm(AIActor actor) {
    actor.SetAI(CharmAI(actor).GetEnumerator());
    actor.statuses.Add(new CharmedStatus());
    actor.faction = Faction.Ally;
    stacks--;
  }

private IEnumerable<ActorTask> CharmAI(AIActor actor) {
    var player = GameModel.main.player;

    player.OnEnterFloor += () => {
      GameModel.main.EnqueueEvent(() => {
        var freeTile = player.floor.GetAdjacentTiles(player.pos).Where((tile) => tile.CanBeOccupied()).First();
        GameModel.main.PutActorAt(actor, player.floor, freeTile.pos);
      });
    };

    // diagonals only count as distance 1
    int DiamondDistance(Actor a) => Math.Min(Math.Abs(a.pos.x - player.pos.x), Math.Abs(a.pos.y - player.pos.y));

    Actor TargetDecider() {
      var targets = player.ActorsInSight(Faction.Enemy).OrderBy(DiamondDistance);
      if (targets.Any()) {
        var closestDistance = DiamondDistance(targets.First());
        /// consider targets tied for closest distance
        var closestDistanceTargets = targets.TakeWhile((a) => DiamondDistance(a) == closestDistance);
        /// out of those, pick the one closest to you
        var target = closestDistanceTargets.OrderBy(actor.DistanceTo).First();
        return target;
      }
      return null;
    }

    while(true) {
      if (TargetDecider() != null) {
        var task = new ChaseDynamicTargetTask(actor, TargetDecider);
        yield return task;
        var target = task.GetTargetActor();
        if (target != null && actor.IsNextTo(target)) {
          yield return new AttackTask(actor, target);
        }
      } else {
        yield return new MoveNextToTargetTask(actor, GameModel.main.player.pos);
      }
    }
  }
}

internal class CharmedStatus : Status {
  public CharmedStatus() {
  }

  public override string Info() => "On your team!";

  public override bool Consume(Status other) => true;
}