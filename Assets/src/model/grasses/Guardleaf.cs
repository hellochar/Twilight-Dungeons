using UnityEngine;

[ObjectInfo(description: "Blocks up to 5 attack damage dealt to any creature standing on the Guardleaf.")]
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
    actor.statuses.Add(new GuardStatus());
    OnNoteworthyAction();
  }

  internal void removeGuard(int reduction) {
    guardLeft -= reduction;
    if (guardLeft <= 0) {
      GameModel.main.EnqueueEvent(Kill);
    }
  }
}

[ObjectInfo("guardroot", "Big leaves")]
public class GuardStatus : StackingStatus, IAttackDamageTakenModifier {
  public override int stacks {
    get => leaf?.guardLeft ?? 0;
    set { }
  }

  public GuardStatus() {}

  private Guardleaf leaf => actor?.grass as Guardleaf;
  public override string Info() => $"The Guardleaf will block {stacks} more attack damage.";

  public int Modify(int input) {
    var reduction = Mathf.Min(input, leaf.guardLeft);
    leaf.removeGuard(reduction);
    leaf.OnNoteworthyAction();
    return input - reduction;
  }

  public override void Step() {
    if (leaf == null || stacks <= 0) {
      Remove();
    }
  }

  public override bool Consume(Status other) => true;
}
