using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
// run fast, fear other jackals nearby when they die
public class Jackal : AIActor {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.MOVE] = 0.67f,
  };

  protected override ActionCosts actionCosts => Jackal.StaticActionCosts;
  public Jackal(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = hpMax = 2;
    ai = AIs.JackalAI(this).GetEnumerator();
    OnDeath += HandleDeath;
  }

  private void HandleDeath() {
    foreach (var jackal in floor.ActorsInCircle(pos, 7).Where((actor) => actor is Jackal)) {
      jackal.SetTasks(new RunAwayTask(jackal, pos, 6));
    }
  }

  internal override int BaseAttackDamage() {
    return UnityEngine.Random.Range(1, 3);
  }
}