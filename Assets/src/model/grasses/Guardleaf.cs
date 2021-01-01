using UnityEngine;

public class Guardleaf : Grass {
  public int guardLeft;
  public Guardleaf(Vector2Int pos) : base(pos) {
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
    guardLeft = 5;
  }

  private void HandleEnterFloor() {
    tile.OnActorEnter += HandleActorEnter;
    actor?.statuses.Add(new GuardStatus());
  }

  private void HandleLeaveFloor() {
    tile.OnActorEnter -= HandleActorEnter;
    actor?.statuses.RemoveOfType<GuardStatus>();
  }

  void HandleActorEnter(Actor who) {
    who.statuses.Add(new GuardStatus());
    TriggerNoteworthyAction();
  }

  internal void removeGuard(int reduction) {
    guardLeft -= reduction;
    if (guardLeft <= 0) {
      GameModel.main.EnqueueEvent(Kill);
    }
  }
}

[ObjectInfo("guardroot", "Big leaves")]
public class GuardStatus : StackingStatus, IDamageTakenModifier {
  public override int stacks {
    get => leaf.guardLeft;
    set { }
  }

  public GuardStatus() {}

  private Guardleaf leaf => actor.grass as Guardleaf;
  public override string Info() => "Blocks 1 damage.";

  public int Modify(int input) {
    var reduction = Mathf.Min(input, leaf.guardLeft);
    leaf.removeGuard(reduction);
    return input - reduction;
  }

  public override void Step() {
    if (leaf == null || stacks <= 0) {
      Remove();
    }
  }

  public override void Stack(Status other) {}
}