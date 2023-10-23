using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Releases three Spore Bloats when any creature steps over it.\n\nSpore Bloats explode, applying the Spored Status to nearby Creatures.\n\nCreatures with the Spored Status deal 0 damage.", flavorText: "One man's dead brother is a fungi's feast.")]
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
[ObjectInfo(description: "Pops after three turns, applying the Spored Status on to adjacent creatures.", flavorText: "Inflated and swollen and looking to spread its seed.")]
internal class SporeBloat : AIActor {
  public override float turnPriority => 25;
  public SporeBloat(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 1;
    faction = Faction.Neutral;
    SetTasks(
      new WaitTask(this, 1),
      new MoveRandomlyTask(this).OnlyCheckBefore(),
      new WaitTask(this, 1).OnlyCheckBefore(),
      ExplodeTask()
    );
  }

  ActorTask ExplodeTask() => new TelegraphedTask(this, 1, new GenericBaseAction(this, Explode));

  protected override ActorTask GetNextTask() {
    return ExplodeTask();
  }

  public void Explode() {
    var floor = this.floor;
    KillSelf();

    FloorController.current.PlayVFX("SporeBloat Explosion", pos);
    foreach (var actor in floor.AdjacentActors(pos).Where(actor => !(actor is SporeBloat))) {
      actor.statuses.Add(new SporedStatus());
    }
  }
}

[System.Serializable]
[ObjectInfo("spored-status", "You inhaled the spore seeds! Your breathing is labored.")]
internal class SporedStatus : Status, IAttackDamageModifier, IActorKilledHandler, IActionPerformedHandler {
  public override bool isDebuff => true;
  public SporedStatus() : base() {}

  public override string Info() => $"Deal 0 attack damage!\nMoving removes Spored Status.\nWhen you die, Spores grow at your position.";

  public void OnKilled(Actor a) {
    a.floor.Put(new Spores(a.pos));
  }

  public int Modify(int input) {
    return 0;
  }

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (final.Type == ActionType.MOVE) {
      Remove();
    }
  }

  public override bool Consume(Status other) {
    return true;
  }
}
