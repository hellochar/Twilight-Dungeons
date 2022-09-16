using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Once Parasite deals attack damage, it applies the Parasite Status and dies.\nAttacks anything near it.\nMoves quickly but randomly.", flavorText: "Blind but fast, these bloodthirsty ticks will latch onto anything they can feel out.")]
public class Parasite : AIActor, IDealAttackDamageHandler {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.MOVE] = 0.5f,
  };

  protected override ActionCosts actionCosts => StaticActionCosts;
  public Parasite(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 1;
  }

  public void HandleDealAttackDamage(int dmg, Body target) {
    if (dmg > 0 && target is Actor actor && !(actor is Parasite)) {
      actor.statuses.Add(new ParasiteStatus(100));
      KillSelf();
    }
  }

  internal override (int, int) BaseAttackDamage() => (1, 1);

  protected override ActorTask GetNextTask() {
    var target = SelectTarget();
    if (target is Actor actor) {
      return new AttackTask(this, target);
    } else {
      return new MoveRandomlyTask(this);
    }
  }

  Actor SelectTarget() {
    var potentialTargets = new HashSet<Actor>(floor
      .AdjacentActors(pos)
      .Where((t) => !(t is Parasite)));
    if (!CanTargetPlayer()) {
      potentialTargets.Remove(GameModel.main.player);
    }
    return Util.RandomPick(potentialTargets);
  }
}

[System.Serializable]
[ObjectInfo(description: "Hatches into two Parasites after 5 turns.")]
public class ParasiteEgg : Body {
  public ParasiteEgg(Vector2Int pos) : base(pos) {
    AddTimedEvent(2, Hatch);
    hp = baseMaxHp = 3;
  }

  void Hatch() {
    var floor = this.floor;
    KillSelf();
    for(var i = 0; i < 2; i++) {
      var p = new Parasite(pos);
      p.ClearTasks();
      p.statuses.Add(new SurprisedStatus());
      floor.Put(p);
    }
  }
}

[System.Serializable]
[ObjectInfo("parasite", flavorText: "You can feel something crawling under your skin.")]
public class ParasiteStatus : StackingStatus, IDeathHandler, IHealHandler {
  public override StackingMode stackingMode => StackingMode.Independent;
  public override bool isDebuff => true;
  [field:NonSerialized] /// controller only
  public event System.Action OnAttack;

  public ParasiteStatus(int stacks) : base(stacks) {
  }

  // remove immediately
  public void HandleHeal(int amount) {
    Remove();
  }

  public void HandleDeath(Entity source) {
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