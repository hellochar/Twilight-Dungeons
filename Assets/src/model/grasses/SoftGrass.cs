using System;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Player has double movespeed on Soft Grass.", flavorText: "Feels nice on your feet.")]
public class SoftGrass : Grass, IActorEnterHandler {
  public static Item HomeItem => new ItemGrassSlippers(4);
  public SoftGrass(Vector2Int pos) : base(pos) {
  }

  public void HandleActorEnter(Actor who) {
    if (who is Player player) {
      player.statuses.Add(new SoftGrassStatus(1));
      OnNoteworthyAction();
    }
  }
}

[Serializable]
[ObjectInfo("grass-slippers", description: "Double movespeed walking over any Grass other than Soft Grass.")]
internal class ItemGrassSlippers : EquippableItem, IActionCostModifier, IBodyMoveHandler {
  public ItemGrassSlippers(int stacks) : base(stacks) { }

  public override EquipmentSlot slot => EquipmentSlot.Footwear;
  public override int stacksMax => int.MaxValue;

  public void HandleMove(Vector2Int newPos, Vector2Int oldPos) {
    var oldGrass = player.floor.grasses[oldPos];
    if (oldGrass != null && !(oldGrass is SoftGrass)) {
      stacks--;
    }
  }

  public ActionCosts Modify(ActionCosts input) {
    if (player.grass != null && !(player.grass is SoftGrass)) {
      input[ActionType.MOVE] *= 0.5f;
    }
    return input;
  }
}

[System.Serializable]
[ObjectInfo(description: "All Player movement is a Free Move over Gold Grass.", flavorText: "Feels extremely nice on your feet.")]
public class GoldGrass : Grass, IActorEnterHandler {
  public GoldGrass(Vector2Int pos) : base(pos) { }

  public void HandleActorEnter(Actor who) {
    if (who is Player player) {
      player.statuses.Add(new GoldGrassStatus());
      OnNoteworthyAction();
    }
  }
}

[Serializable]
[ObjectInfo("goldgrass", "Feels extremely nice on your feet.")]
public class GoldGrassStatus : Status, IActionCostModifier {
  public override bool Consume(Status other) => true;

  public override string Info() => "Movement over Gold Grass is free.";

  public override void Step() {
    if (!(actor.grass is GoldGrass)) {
      Remove();
    }
  }

  public ActionCosts Modify(ActionCosts input) {
    input[ActionType.MOVE] = 0f;
    return input;
  }
}

[System.Serializable]
[ObjectInfo(spriteName: "colored_transparent_packed_95", flavorText: "Feels nice on your feet.")]
public class SoftGrassStatus : StackingStatus, IActionCostModifier {
  public override StackingMode stackingMode => StackingMode.Ignore;
  public SoftGrassStatus(int stacks) : base(stacks) {}

  public override void Step() {
    if (!(actor.grass is SoftGrass)) {
      Remove();
    }
  }

  public override string Info() => "Move twice as fast on Soft Grass.";

  public ActionCosts Modify(ActionCosts input) {
    input[ActionType.MOVE] *= 0.5f;
    return input;
  }
}

[System.Serializable]
[ObjectInfo("free-move", "")]
public class FreeMoveStatus : StackingStatus, IActionCostModifier, IActionPerformedHandler {
  public FreeMoveStatus(int stacks) : base(stacks) {}
  public FreeMoveStatus() : this(1) {}

  public override string Info() => "Get another turn immediately after you move!";

  public ActionCosts Modify(ActionCosts costs) {
    costs[ActionType.MOVE] = 0f;
    return costs;
  }

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (initial.Type == ActionType.MOVE) {
      stacks--;
    }
  }
}