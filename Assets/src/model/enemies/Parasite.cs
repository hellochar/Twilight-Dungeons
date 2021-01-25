using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ObjectInfo(description: "Moves twice per turn. When Parasite deals damage, it applies the Infested Status and dies.", flavorText: "")]
public class Parasite : AIActor {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.MOVE] = 0.5f,
    [ActionType.ATTACK] = 1f,
  };
  public override float turnPriority => task is AttackGroundTask ? 90 : base.turnPriority;

  protected override ActionCosts actionCosts => StaticActionCosts;
  public Parasite(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 1;
    ai = AI().GetEnumerator();
    OnDealAttackDamage += HandleDealAttackDamage;
  }

  private void HandleDealAttackDamage(int dmg, Body target) {
    if (target is Actor actor && !(actor is Parasite)) {
      actor.statuses.Add(new ParasiteStatus(100));
      Kill();
    }
  }

  internal override (int, int) BaseAttackDamage() => (1, 1);

  private IEnumerable<ActorTask> AI() {
    while (true) {
      var target = SelectTarget();
      if (target == null) {
        yield return new MoveRandomlyTask(this);
        continue;
      }
      yield return new AttackGroundTask(this, target.pos, 1);
    }
  }

  Actor SelectTarget() {
    var potentialTargets = floor
      .AdjacentActors(pos)
      .Where((t) => !(t is Parasite));
    return Util.RandomPick(potentialTargets);
  }
}

public class ParasiteEgg : Body {
  public ParasiteEgg(Vector2Int pos) : base(pos) {
    AddTimedEvent(10, Hatch);
    hp = baseMaxHp = 5;
  }

  void Hatch() {
    var tiles = floor.GetCardinalNeighbors(pos).Where((t) => t.CanBeOccupied()).ToList();
    tiles.Shuffle();
    foreach (var tile in tiles.Take(2)) {
      var p = new Parasite(tile.pos);
      var sleepTask = p.task as SleepTask;
      sleepTask.wakeUpNextTurn = true;
      floor.Put(p);
    }
    Kill();
  }
}

[ObjectInfo("colored_transparent_packed_270", "oh noooooooo")]
public class ParasiteStatus : StackingStatus {
  public override StackingMode stackingMode => StackingMode.Independent;
  public override bool isDebuff => base.isDebuff;
  public event System.Action OnAttack;

  public ParasiteStatus(int stacks) : base(stacks) {
  }

  public override void Start() {
    actor.OnDeath += HandleActorDeath;
    actor.OnHeal += HandleHeal;
  }

  public override void End() {
    actor.OnDeath -= HandleActorDeath;
    actor.OnHeal -= HandleHeal;
  }

  // remove immediately
  private void HandleHeal(int arg1, int arg2) {
    Remove();
  }


  private void HandleActorDeath() {
    var floor = actor.floor;
    var pos = actor.pos;
    GameModel.main.EnqueueEvent(() => {
      floor.Put(new ParasiteEgg(pos));
    });
  }

  public override void Step() {
    if (stacks % 10 == 0 && stacks != 100) {
      GameModel.main.EnqueueEvent(() => {
        actor?.TakeAttackDamage(1, actor);
      });
      OnAttack?.Invoke();
    }
    stacks--;
  }

  public override string Info() => "A parasite is inside you! Take 1 attack damage per 10 turns.\nHealing cures immediately.\nIf you die, a Parasite Egg spawns over your corpse.";
}