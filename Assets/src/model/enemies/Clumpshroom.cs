using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "After 5-7 turns, turns into two or three Clumpshrooms in adjacent tiles.\nDoes not attack or move\n.On death, it applies Clumped Lung to the killer.\nIf you have 20 Clumped Lung, you die.")]
public class Clumpshroom : AIActor, IBaseActionModifier, INoTurnDelay {
  bool hasDuplicated = false;
  public Clumpshroom(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 1;
    SetTasks(new WaitTask(this, MyRandom.Range(5, 8)));
  }

  protected override ActorTask GetNextTask() {
    if (hasDuplicated) {
      return new WaitTask(this, 999);
    }
    return new TelegraphedTask(this, 1, new GenericBaseAction(this, Duplicate));
  }

  public void Duplicate() {
    var iter = floor
      .GetAdjacentTiles(pos)
      .Where(t => t.CanBeOccupied() && t.pos != pos).ToList();
    iter.Shuffle();
    floor.PutAll(iter.Take(MyRandom.Range(2, 4)).Select(t => new Clumpshroom(t.pos)));
    KillSelf();

    // floor.PutAll(
    //   floor
    //     .GetCardinalNeighbors(pos)
    //     .Where(t => t.CanBeOccupied())
    //     .Select(t => new Clumpshroom(t.pos))
    // );
    // hasDuplicated = true;
  }

  public override void HandleDeath(Entity source) {
    if (source is Actor a) {
      a.statuses.Add(new ClumpedLungStatus());
    }
    base.HandleDeath(source);
  }

  public BaseAction Modify(BaseAction input) {
    // cannot move
    if (input.Type == ActionType.MOVE || input.Type == ActionType.ATTACK) {
      return new WaitBaseAction(this);
    }
    return input;
  }
}

[System.Serializable]
[ObjectInfo("clumped-lung", description: "At 20 stacks, you die.")]
public class ClumpedLungStatus : StackingStatus, IFloorChangeHandler {
  public override bool isDebuff => true;
  public override void HandleFloorChanged(Floor newFloor, Floor oldFloor) {
    Remove();
  }

  public int numToKill = 
#if experimental_chainfloors
    50;
#else
    20;
#endif

  public override bool Consume(Status otherParam) {
    var baseRetVal = base.Consume(otherParam);
    if (stacks >= numToKill) {
      actor.KillSelf();
    }
    return baseRetVal;
  }

  public override string Info() {
    return $"At {numToKill} stacks, you die.";
  }
}
