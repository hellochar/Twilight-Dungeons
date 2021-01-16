using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ObjectInfo(description: "Releases three Spore Bloats when any creature steps over it.", flavorText: "One man's dead brother is a fungi's feast.")]
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

[ObjectInfo(description: "Explodes, applying the Spored Status on to adjacent creatures.", flavorText: "Massive and swollen and looking to spread its seed...")]
internal class SporeBloat : AIActor {
  public SporeBloat(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 1;
    faction = Faction.Neutral;
    OnDeath += HandleDeath;
    ai = AI();
    ClearTasks();
  }

  private IEnumerator<ActorTask> AI() {
    yield return new WaitTask(this, 1);
    yield return new MoveRandomlyTask(this);
    GameModel.main.EnqueueEvent(() => statuses.Add(new SurprisedStatus()));
    yield return new WaitTask(this, 1); // gets consumed by the surprised
    yield return new GenericTask(this, (_) => {
      Kill();
    });
  }

  private void HandleDeath() {
    foreach (var actor in floor.AdjacentActors(pos).Where(actor => !(actor is SporeBloat))) {
      if (!actor.statuses.Has<SporedStatus>()) {
        actor.statuses.Add(new SporedStatus(20));
      }
    }
  }
}

[ObjectInfo("spored-status", "eww")]
internal class SporedStatus : StackingStatus, IAttackDamageModifier, IActionCostModifier, IActorKilledHandler {
  public override StackingMode stackingMode => StackingMode.Max;
  public SporedStatus(int stacks) : base(stacks) {}

  public override string Info() => $"Deal 2 less attack damage!\nMove twice as slow.\nWhen you die, Spores grow at your position.\n{stacks} turns remaining.";

  public override void Step() {
    stacks--;
  }

  public int Modify(int input) {
    return input - 2;
  }

  public ActionCosts Modify(ActionCosts input) {
    input[ActionType.MOVE] *= 2;
    return input;
  }

  public void OnKilled(Actor a) {
    a.floor.Put(new Spores(a.pos));
  }
}
