using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
[ObjectInfo(spriteName: "charmberry", flavorText: "It's sweet, sour, and tart! Loved by creatures of all sorts.")]
public class ItemCharmBerry : Item, IStackable, ITargetedAction<AIActor> {
  public int stacksMax => 12;

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

  internal override string GetStats() => "Makes a target loyal to you; they will follow you and attack nearby enemies, but cannot traverse floors. Enemies will not re-direct their focus to attack them.";

  private void Charm(AIActor actor) {
    actor.SetAI(new CharmAI(actor));
    actor.statuses.Add(new CharmedStatus());
    actor.faction = Faction.Ally;
    stacks--;
  }

  public string TargettedActionName => "Charm";

  public IEnumerable<AIActor> Targets(Player player) => player.GetVisibleActors(Faction.Enemy).Where((a) => a is AIActor && !(a is Boss)).Cast<AIActor>();

  public void PerformTargettedAction(Player player, Entity e) {
    var actor = (AIActor) e;
    player.SetTasks(
      new ChaseTargetTask(player, actor),
      new GenericPlayerTask(player, () => Charm(actor))
    );
  }
}

public interface ITargetedAction<out T> where T : Entity {
  string TargettedActionName { get; }
  IEnumerable<T> Targets(Player player);
  void PerformTargettedAction(Player player, Entity target);
}

[Serializable]
public abstract class AI {
  public abstract ActorTask GetNextTask();
}

[Serializable]
public class CharmAI : AI {
  public AIActor actor;

  public CharmAI(AIActor actor) {
    this.actor = actor;
  }

  public override ActorTask GetNextTask() {
    var player = GameModel.main.player;

    // player.OnEnterFloor += () => {
    //   GameModel.main.EnqueueEvent(() => {
    //     var freeTile = player.floor.GetAdjacentTiles(player.pos).Where((tile) => tile.CanBeOccupied()).First();
    //     GameModel.main.PutActorAt(actor, player.floor, freeTile.pos);
    //   });
    // };

    var target = TargetDecider();
    if (target == null) {
      return new MoveNextToTargetTask(actor, GameModel.main.player.pos);
    }
    if (actor.IsNextTo(target)) {
      return new AttackTask(actor, target);
    }
    return new ChaseDynamicTargetTask(actor, TargetDecider);
  }

  Actor TargetDecider() {
    var targets = GameModel.main.player.GetVisibleActors(Faction.Enemy).OrderBy(Util.DiamondDistanceToPlayer);
    if (targets.Any()) {
      var closestDistance = Util.DiamondDistanceToPlayer(targets.First());
      /// consider targets tied for closest distance
      var closestDistanceTargets = targets.TakeWhile((a) => Util.DiamondDistanceToPlayer(a) == closestDistance);
      /// out of those, pick the one closest to you
      var target = closestDistanceTargets.OrderBy(actor.DistanceTo).First();
      return target;
    }
    return null;
  }
}

[System.Serializable]
internal class CharmedStatus : Status {
  public CharmedStatus() {
  }

  public override string Info() => "On your team!";

  public override bool Consume(Status other) => true;
}