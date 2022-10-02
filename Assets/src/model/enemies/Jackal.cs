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
    var jackalsToAlert = floor.ActorsInCircle(pos, 7).Where((actor) => actor is Jackal && floor.TestVisibility(pos, actor.pos) == TileVisiblity.Visible).ToList();
    GameModel.main.EnqueueEvent(() => {
      foreach (var jackal in jackalsToAlert) {
        jackal.SetTasks(new RunAwayTask(jackal, pos, 6));
      }
    });
  }

  internal override (int, int) BaseAttackDamage() => (1, 2);
}
