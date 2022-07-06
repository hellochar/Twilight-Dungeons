using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "After five turns, dies and spreads Pollen to the four adjacent Tiles.")]
public class Puffer : AIActor, IBodyTakeAttackDamageHandler, INoTurnDelay {
  public Puffer(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 1;
    SetTasks(new WaitTask(this, 5));
  }

  protected override ActorTask GetNextTask() {
    return new TelegraphedTask(this, 1, new GenericBaseAction(this, CreatePollen));
  }

  public void CreatePollen() {
    floor.PutAll(
      floor
        .GetCardinalNeighbors(pos)
        .Where(Pollen.CanOccupy)
        .Select(t => new Pollen(t.pos))
    );
    KillSelf();
  }

  public void HandleTakeAttackDamage(int damage, int hp, Actor source) {
    var sniffly = source.statuses.FindOfType<SnifflyStatus>();
    if (!(source is Puffer) && sniffly != null) {
      source.TakeAttackDamage(1, source);
      sniffly.stacks--;
    }
  }
}

[System.Serializable]
[ObjectInfo(description: "Any Creature walking over it takes 1 attack damage and clears the Pollen.\nAfter five turns, turns into a Puffer.")]
public class Pollen : Grass, ISteppable, IActorEnterHandler {
  public float timeNextAction { get; set; }
  public float turnPriority => 90;
  public static bool CanOccupy(Tile tile) => tile is Ground && !(tile.grass is Pollen) && tile.CanBeOccupied();

  public Pollen(Vector2Int pos) : base(pos) {
    timeNextAction = timeCreated + 5;
  }

  public void HandleActorEnter(Actor who) {
    if (!(who is Puffer)) {
      who.TakeAttackDamage(1, who);
      Kill(who);
    }
  }

  public float Step() {
    floor.Put(new Puffer(pos));
    KillSelf();
    return 1;
  }
}

[System.Serializable]
[ObjectInfo("pollen", description: "Makes you sneeze.")]
class SnifflyStatus : StackingStatus, IFloorChangeHandler, IActionPerformedHandler {
  public override bool isDebuff => true;
  // public override StackingMode stackingMode => StackingMode.Max;

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (final.Type == ActionType.WAIT) {
      stacks--;
      actor.floor.Put(new Puffer(actor.pos));
    }
  }

  public void HandleFloorChanged(Floor newFloor, Floor oldFloor) {
    Remove();
  }

  public override string Info() {
    return "When you Wait, you will Sneeze out a Puffer!\nRemoved on Floor change.";
  }
}