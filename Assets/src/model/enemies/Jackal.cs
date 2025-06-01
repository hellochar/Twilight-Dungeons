using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
// run fast, fear other jackals nearby when they die
[System.Serializable]
[ObjectInfo(description: "Alternates moving 1 and 2 tiles.\nRuns away when another Jackal dies.\nChases you.")]
public class Jackal : AIActor {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.MOVE] = 0.67f,
  };

  protected override ActionCosts actionCosts => Jackal.StaticActionCosts;
  public Jackal(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 2;
  }

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;
    if (CanTargetPlayer()) {
      if (IsNextTo(player)) {
        return new AttackTask(this, player);
      } else {
        return new ChaseTargetTask(this, player);
      }
    } else {
      return new MoveRandomlyTask(this);
    }
  }

  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);
    var jackalsToAlert = floor.ActorsInCircle(pos, 7).Where((actor) => (actor is Jackal || actor is JackalBoss) && floor.TestVisibility(pos, actor.pos)).ToList();
    GameModel.main.EnqueueEvent(() => {
      foreach (var jackal in jackalsToAlert) {
        jackal.SetTasks(new RunAwayTask(jackal, pos, 6));
      }
    });
  }

  internal override (int, int) BaseAttackDamage() => (1, 2);
}

[Serializable]
[ObjectInfo(description: "Summons jackals if there are none on the map.")]
public class JackalBoss : Boss {
  // moves faster than jackals
  public override float turnPriority => base.turnPriority - 1;

  public JackalBoss(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 16;
    faction = Faction.Enemy;
    ClearTasks();
  }

  public float cooldown = 0;
  public override float Step() {
    var dt = base.Step();
    if (cooldown > 0) {
      cooldown -= dt;
    }
    return dt;
  }

  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);
    // kill all jackals on the map
    foreach (var j in AllJackals.ToArray()) {
      j.Kill(this);
    }
  }

  public IEnumerable<Jackal> AllJackals => floor.bodies.OfType<Jackal>();

  internal override (int, int) BaseAttackDamage() {
    return (2, 3);
  }

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;
    bool shouldCast = cooldown <= 0 && AllJackals.Count() < 1;
    if (shouldCast) {
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, SummonJackals));
    }
    if (CanTargetPlayer()) {
      if (IsNextTo(player)) {
        return new AttackTask(this, player);
      } else {
        var task = new ChaseTargetTask(this, player);
        // recompute GetNextTask for cooldown
        task.maxMoves = 1;
        return task;
      }
    } else {
      return new MoveRandomlyTask(this);
    }
  }

  int numToSummon = 2;

  public void SummonJackals() {
    cooldown = 9;
    var perimeter = floor.EnumeratePerimeter(1).ToList();
    perimeter.Shuffle();
    for (int i = 0; i < numToSummon; i++) {
      floor.Put(new Jackal(perimeter[i]));
    }
    numToSummon++;
  }
}

// [Serializable]
// [ObjectInfo("slime", description: "Deals 1 damage to any non-Blob that walks into it.\nRemoved when you walk into it, or the Blobmother dies.")]
// public class BlobSlime : Grass, IActorEnterHandler {
//   public BlobSlime(Vector2Int pos) : base(pos) {}

//   public void HandleActorEnter(Actor who) {
//     if (!(who is Blob || who is Blobmother || who is MiniBlob)) {
//       who.TakeDamage(1, this);
//       Kill(who);
//     }
//   }
// }