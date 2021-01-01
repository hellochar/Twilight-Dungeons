using UnityEngine;

public class SoftGrass : Grass {
  public SoftGrass(Vector2Int pos) : base(pos) {
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
  }

  void HandleEnterFloor() {
    /// TODO make this declarative instead of manually registering events
    tile.OnActorEnter += HandleActorEnter;
  }

  private void HandleLeaveFloor() {
    tile.OnActorEnter -= HandleActorEnter;
  }

  void HandleActorEnter(Actor who) {
    if (who is Player player) {
      player.statuses.Add(new SoftGrassStatus(1));
      TriggerNoteworthyAction();
    }
  }
}

public class SoftGrassStatus : StackingStatus, IActionCostModifier, IBaseActionModifier {
  public override StackingMode stackingMode => StackingMode.Ignore;
  public SoftGrassStatus(int stacks) : base(stacks) {}
  private bool nextMoveFree = false;

  public ActionCosts Modify(ActionCosts costs) {
    if (nextMoveFree) {
      nextMoveFree = false;
      costs[ActionType.MOVE] = 0f;
    }
    return costs;
  }

  public BaseAction Modify(BaseAction input) {
    if (input.Type == ActionType.MOVE) {
      if (stacks >= 5) {
        nextMoveFree = true;
        stacks = 1;
      } else {
        stacks++;
      }
    }
    return input;
  }

  public override void Step() {
    if (!(actor.grass is SoftGrass)) {
      Remove();
    }
  }

  public override string Info() => "Every fifth move is free!";
}