using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
// run fast, fear other jackals nearby when they die
public class Jackal : AIActor {
  public static new IDictionary<ActionType, float> ActionCosts = new Dictionary<ActionType, float>(Actor.ActionCosts) {
    [ActionType.MOVE] = 0.67f,
  };

  public override IDictionary<ActionType, float> actionCosts => Jackal.ActionCosts;
  public Jackal(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = hpMax = 3;
    ai = AIs.JackalAI(this).GetEnumerator();
    OnDeath += HandleDeath;
  }

  private void HandleDeath() {
    foreach (var jackal in floor.ActorsInCircle(pos, 7).Where((actor) => actor is Jackal)) {
      jackal.SetTasks(new RunAwayTask(jackal, pos, 6));
    }
  }

  internal override int GetAttackDamage() {
    return UnityEngine.Random.Range(1, 3);
  }
}
