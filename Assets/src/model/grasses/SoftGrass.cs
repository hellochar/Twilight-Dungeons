using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Moving five times on Soft Grass gives the Player one Free Move.", flavorText: "Feels nice on your feet.")]
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

[System.Serializable]
[ObjectInfo(spriteName: "colored_transparent_packed_95", flavorText: "Feels nice on your feet.")]
public class SoftGrassStatus : StackingStatus, IBodyMoveHandler {
  public override StackingMode stackingMode => StackingMode.Ignore;
  public SoftGrassStatus(int stacks) : base(stacks) {}

  public void HandleMove(Vector2Int newPos, Vector2Int oldPos) {
    if (newPos != oldPos && actor.floor.grasses[newPos] is SoftGrass) {
      stacks++;
      if (stacks == 5) {
        GameModel.main.EnqueueEvent(() => actor.statuses.Add(new FreeMoveStatus()));
      } else if (stacks > 5) {
        stacks = 1;
      }
    }
  }

  public override void Step() {
    if (!(actor.grass is SoftGrass)) {
      Remove();
    }
  }

  public override string Info() => "Moving five times on Soft Grass gives the Player one Free Move.";
}

[System.Serializable]
[ObjectInfo("colored_transparent_packed_850", "")]
public class FreeMoveStatus : StackingStatus, IActionCostModifier, IBaseActionModifier {
  public FreeMoveStatus(int stacks) : base(stacks) {}
  public FreeMoveStatus() : this(1) {}

  public override string Info() => "Your next move is free!";

  public ActionCosts Modify(ActionCosts costs) {
    costs[ActionType.MOVE] = 0f;
    return costs;
  }

  public BaseAction Modify(BaseAction input) {
    if (input.Type == ActionType.MOVE) {
      stacks--;
    }
    return input;
  }

  public override bool Consume(Status other) => true;
}