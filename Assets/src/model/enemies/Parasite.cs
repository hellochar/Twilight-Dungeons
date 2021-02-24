using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "When Parasite deals damage, it applies the Parasite Status and dies.\nAttacks anything near it.\nMoves quickly but randomly.", flavorText: "Blind but fast, these bloodthirsty ticks will latch onto anything they can feel out.")]
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
    var potentialTargets = floor
      .AdjacentActors(pos)
      .Where((t) => !(t is Parasite));
    return Util.RandomPick(potentialTargets);
  }
}

[System.Serializable]
[ObjectInfo(description: "Hatches into two Parasites after 5 turns.")]
public class ParasiteEgg : Body {
  public ParasiteEgg(Vector2Int pos) : base(pos) {
    AddTimedEvent(5, Hatch);
    hp = baseMaxHp = 5;
  }

  void Hatch() {
    var tiles = floor.GetCardinalNeighbors(pos).Where((t) => t.CanBeOccupied()).ToList();
    tiles.Shuffle();
    foreach (var tile in tiles.Take(2)) {
      var p = new Parasite(tile.pos);
      p.timeNextAction += 1;
      p.ClearTasks();
      floor.Put(p);
    }
    KillSelf();
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