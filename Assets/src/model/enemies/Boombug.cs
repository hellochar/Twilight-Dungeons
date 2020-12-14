using System.Collections.Generic;
using UnityEngine;

public class Boombug : AIActor {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.WAIT] = 2f,
    [ActionType.MOVE] = 2f,
  };
  protected override ActionCosts actionCosts => Boombug.StaticActionCosts;
  public Boombug(Vector2Int pos) : base(pos) {
    hp = hpMax = 1;
    faction = Faction.Neutral;
    ai = AIs.BugAI(this).GetEnumerator();
    OnDeath += HandleDeath;
  }

  private void HandleDeath() {
    var floor = this.floor;
    // leave a corpse on death
    // explode and hurt everything nearby
    GameModel.main.EnqueueEvent(() => floor.Put(new BoombugCorpse(pos)));
  }
}

public class BoombugCorpse : Actor {
  public BoombugCorpse(Vector2Int pos) : base(pos) {
    hp = hpMax = 1;
    OnDeath += HandleDeath;
    SetTasks(new ExplodeTask(this));
  }

  private void HandleDeath() {
    var floor = this.floor;
    GameModel.main.EnqueueEvent(() => {
      foreach (var tile in floor.GetAdjacentTiles(pos)) {
        if (tile.actor != null && tile.actor != this) {
          this.Attack(tile.actor);
        }
        if (tile.grass != null) {
          tile.grass.Kill();
        }
      }
    });
  }

  protected override float Step() {
    Kill();
    return baseActionCost;
  }

  internal override int BaseAttackDamage() {
    return 2;
  }
}

public class ExplodeTask : ActorTask {
  public ExplodeTask(Actor actor) : base(actor) {
  }

  public override IEnumerator<BaseAction> Enumerator() {
    yield return new WaitBaseAction(actor);
  }
}