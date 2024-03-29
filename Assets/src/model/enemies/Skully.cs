using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Chases you.\nWhen Skully dies, it turns into Muck.\nMuck regenerates into a new Skully after three turns.\nStep on the Muck to remove it.")]
public class Skully : AIActor, IActorKilledHandler {
  public Skully(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 1;
  }

  public void OnKilled(Actor a) {
    var floor = this.floor;
    GameModel.main.EnqueueEvent(() => {
      var muckSpot = floor.BreadthFirstSearch(pos, tile => tile is Ground).Where(t => !(t.grass is Muck)).FirstOrDefault();
      if (muckSpot != null) {
        floor.Put(new Muck(muckSpot.pos));
      }
    });
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

  internal override (int, int) BaseAttackDamage() => (1, 1);
}

public interface IEnemyEntity {}

[System.Serializable]
[ObjectInfo(description: "Regenerates into a Skully after three turns.\nStep on the Muck to remove it.")]
public class Muck : Grass, ISteppable, IActorEnterHandler, IEnemyEntity {
  public Muck(Vector2Int pos) : base(pos) {
    timeNextAction = timeCreated + 1;
  }

  public float timeNextAction { get; set; }

  public float turnPriority => 40;

  public void HandleActorEnter(Actor who) {
    if (who is Player p) {
      Kill(p);
    }
  }

  public float Step() {
    OnNoteworthyAction();
    if (timeNextAction - timeCreated >= 3) {
      var s = new Skully(pos);
      s.ClearTasks();
      s.timeNextAction += 1;
      floor.Put(s);
      KillSelf();
    }
    return 1;
  }
}
