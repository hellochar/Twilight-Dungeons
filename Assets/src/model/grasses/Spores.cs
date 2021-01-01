using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Spores : Grass {
  public Spores(Vector2Int pos) : base(pos) {
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
    OnDeath += HandleDeath;
  }

  private void HandleEnterFloor() {
    tile.OnActorEnter += HandleActorEnter;
  }

  private void HandleLeaveFloor() {
    tile.OnActorEnter -= HandleActorEnter;
  }

  private void HandleActorEnter(Actor actor) {
    if (!(actor is SporeBloat)) {
      Kill();
    }
  }

  private void HandleDeath() {
    var freeSpots = floor.GetAdjacentTiles(pos).Where(x => x.CanBeOccupied()).ToList();
    freeSpots.Shuffle();
    foreach (var tile in freeSpots.Take(3)) {
      floor.Put(new SporeBloat(tile.pos));
    }
  }
}

internal class SporeBloat : AIActor {
  private bool isMature => age >= 20;
  public SporeBloat(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 1;
    faction = Faction.Neutral;
    OnDeath += HandleDeath;
    OnActionPerformed += HandleActionPerformed;
    ai = AI();
  }

  private void HandleActionPerformed(BaseAction arg1, BaseAction arg2) {
    GameModel.main.EnqueueEvent(() => {
      if (floor != null) {
        foreach (var actor in floor.AdjacentActors(pos).Where(actor => !(actor is SporeBloat))) {
          if (!actor.statuses.Has<SporedStatus>()) {
            actor.statuses.Add(new SporedStatus(1));
          }
        }
      }
    });
  }

  private IEnumerator<ActorTask> AI() {
    while(true) {
      if (isMature) {
        yield return new GenericTask(this, (_) => {
          Kill();
        });
      } else {
        yield return new WaitTask(this, 1);
        yield return new MoveRandomlyTask(this);
      }
    }
  }

  private void HandleDeath() {
    if (isMature) {
      floor.Put(new Spores(pos));
    }
  }
}

[ObjectInfo("slimed", "eww")]
internal class SporedStatus : StackingStatus, IAttackDamageModifier, IActionCostModifier {
  public SporedStatus(int stacks) : base(stacks) {
  }

  public override string Info() => $"You do {stacks} less damage!\nYou move twice as slow.\nStacks are equal to number of adjacent Spore Bloats.";

  public override void Step() {
    GameModel.main.EnqueueEvent(() => {
      if (actor != null) {
        var numNearbyBloats = actor.floor.AdjacentActors(actor.pos).Where((a) => a is SporeBloat).Count();
        stacks = numNearbyBloats;
      }
    });
  }

  public int Modify(int input) {
    return input - stacks;
  }

  public ActionCosts Modify(ActionCosts input) {
    input[ActionType.MOVE] *= 2;
    return input;
  }
}
