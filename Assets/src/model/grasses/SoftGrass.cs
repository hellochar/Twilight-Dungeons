using UnityEngine;

[ObjectInfo(description: "Moving five times on Soft Grass gives you one Free Move.")]
public class SoftGrass : Grass, IActorEnterHandler{
  public SoftGrass(Vector2Int pos) : base(pos) {
  }

  public void HandleActorEnter(Actor who) {
    if (who is Player player) {
      player.statuses.Add(new SoftGrassStatus(1));
      OnNoteworthyAction();
    }
  }
}
[ObjectInfo(spriteName: "colored_transparent_packed_95", flavorText: "Feels nice on your feet.")]
public class SoftGrassStatus : StackingStatus, IBaseActionModifier {
  public override StackingMode stackingMode => StackingMode.Ignore;
  public SoftGrassStatus(int stacks) : base(stacks) {}

  public BaseAction Modify(BaseAction input) {
    if (input.Type == ActionType.MOVE) {
      stacks++;
      if (stacks == 5) {
        GameModel.main.EnqueueEvent(() => input.actor.statuses.Add(new FreeMoveStatus()));
      } else if (stacks > 5) {
        stacks = 1;
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

[ObjectInfo("colored_transparent_packed_850", "")]
public class FreeMoveStatus : Status, IActionCostModifier, IBaseActionModifier {
  public FreeMoveStatus() {}

  public override string Info() => "Your next move is free!";

  public ActionCosts Modify(ActionCosts costs) {
    costs[ActionType.MOVE] = 0f;
    return costs;
  }

  public BaseAction Modify(BaseAction input) {
    if (input.Type == ActionType.MOVE) {
      Remove();
    }
    return input;
  }

  public override bool Consume(Status other) => true;
}