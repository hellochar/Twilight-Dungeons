using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Releases three Spore Bloats when any creature steps over it.", flavorText: "One man's dead brother is a fungi's feast.")]
public class Spores : Grass, IActorEnterHandler {
  public Spores(Vector2Int pos) : base(pos) {
  }

  public void HandleActorEnter(Actor actor) {
    if (!(actor is SporeBloat)) {
      Activate();
      Kill(actor);
    }
  }

  public void Activate() {
    var freeSpots = floor.GetAdjacentTiles(pos).Where(x => x.CanBeOccupied()).ToList();
    freeSpots.Shuffle();
    foreach (var tile in freeSpots.Take(3)) {
      floor.Put(new SporeBloat(tile.pos));
    }
  }
}

[System.Serializable]
[ObjectInfo(description: "Explodes, applying the Spored Status on to adjacent creatures.", flavorText: "Inflated and swollen and looking to spread its seed.")]
internal class SporeBloat : AIActor {
  public override float turnPriority => 25;
  public SporeBloat(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 1;
    faction = Faction.Neutral;
    SetTasks(
      new WaitTask(this, 1),
      new MoveRandomlyTask(this).OnlyCheckBefore(),
      new WaitTask(this, 1),
      new TelegraphedTask(this, 1, new GenericBaseAction(this, KillSelf))
    );
  }

  protected override ActorTask GetNextTask() {
    return new GenericTask(this, KillSelf);
  }

  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);
    foreach (var actor in floor.AdjacentActors(pos).Where(actor => !(actor is SporeBloat))) {
      if (!actor.statuses.Has<SporedStatus>()) {
        actor.statuses.Add(new SporedStatus(20));
      }
    }
  }
}

[System.Serializable]
[ObjectInfo("spored-status", "You inhaled the spore seeds! Your breathing is labored.")]
internal class SporedStatus : StackingStatus, IAttackDamageModifier, IActionCostModifier, IActorKilledHandler, IActionPerformedHandler {
  public override bool isDebuff => true;
  public override StackingMode stackingMode => StackingMode.Max;
  public SporedStatus(int stacks) : base(stacks) {}

  public override string Info() => $"Deal 2 less attack damage!\nMove slowly.\nMoving removes two stacks.\nWhen you die, Spores grow at your position.\n{stacks} turns remaining.";

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

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (final.Type == ActionType.MOVE) {
      stacks--;
    }
  }
}
