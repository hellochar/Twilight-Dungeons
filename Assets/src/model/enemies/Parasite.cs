using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Parasite : AIActor {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.MOVE] = 0.5f,
  };

  protected override ActionCosts actionCosts => StaticActionCosts;
  public Parasite(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 1;
    ai = AI().GetEnumerator();
    OnDealAttackDamage += HandleDealAttackDamage;
  }

  private void HandleDealAttackDamage(int dmg, Actor target) {
    if (dmg > 0) {
      GameModel.main.EnqueueEvent(() => target.statuses.Add(new ParasiteStatus(16)));
      Kill();
    }
  }

  internal override int BaseAttackDamage() {
    return 1;
  }

  private IEnumerable<ActorTask> AI() {
    while (true) {
      var target = SelectTarget();
      if (target == null) {
        yield return new MoveRandomlyTask(this);
        continue;
      }
      if (IsNextTo(target)) {
        yield return new AttackTask(this, target);
        continue;
      }
      // chase until you are next to any target
      yield return new ChaseDynamicTargetTask(this, SelectTarget);
    }
  }

  Actor SelectTarget() {
    var potentialTargets = floor
      .ActorsInCircle(pos, 7)
      .Where((t) => floor.TestVisibility(pos, t.pos) && !(t is Parasite));
    if (potentialTargets.Any()) {
      return potentialTargets.Aggregate((t1, t2) => DistanceTo(t1) < DistanceTo(t2) ? t1 : t2);
    }
    return null;
  }
}

[ObjectInfo("colored_transparent_packed_270", "oh noooooooo")]
public class ParasiteStatus : StackingStatus {
  public override bool isDebuff => base.isDebuff;

  public ParasiteStatus(int stacks) : base(stacks) {
  }

  public override void Start() {
    actor.OnDeath += HandleActorDeath;
  }

  public override void End() {
    actor.OnDeath -= HandleActorDeath;
  }

  private void HandleActorDeath() {
    var floor = actor.floor;
    var pos = actor.pos;
    GameModel.main.EnqueueEvent(() => {
      var tiles = floor.GetAdjacentTiles(pos).Where((t) => t.CanBeOccupied()).ToList();
      tiles.Shuffle();
      foreach (var tile in tiles.Take(2)) {
        floor.Put(new Parasite(tile.pos));
      }
    });
  }


  public override void Step() {
    if (stacks % 3 == 0) {
      actor.TakeAttackDamage(1, actor);
    }
    stacks--;
  }

  public override string Info() => "A parasite is inside you! Take 1 attack damage per 3 turns.\nIf you die, two parasites spawn around your position.";
}