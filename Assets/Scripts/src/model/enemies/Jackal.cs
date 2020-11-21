using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
// run fast, fear other jackals nearby when they die
public class Jackal : AIActor {
  public static new IDictionary<Type, float> ActionCosts = new ReadOnlyDictionary<Type, float>(
    new Dictionary<Type, float>(Actor.ActionCosts) {
      {typeof(FollowPathAction), 0.67f},
    }
  );

  public override IDictionary<Type, float> actionCosts => Jackal.ActionCosts;
  public Jackal(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = hpMax = 3;
    ai = AIs.JackalAI(this).GetEnumerator();
    OnDeath += HandleDeath;
  }

  private void HandleDeath() {
    foreach (var jackal in floor.ActorsInCircle(pos, 7).Where((actor) => actor is Jackal)) {
      jackal.SetActions(new RunAwayAction(jackal, pos, 6));
    }
  }
}
