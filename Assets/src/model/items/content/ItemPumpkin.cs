using System;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Attack to uproot and pick up. You can eat a pumpkin to gain 4 stacks of strength.")]
public class Pumpkin : Destructible, IDeathHandler {
    public Pumpkin(Vector2Int pos) : base(pos) {
    }

    public void HandleDeath(Entity source) {
      var floor = this.floor;
      GameModel.main.EnqueueEvent(() => {
        floor.Put(new ItemOnGround(pos, new ItemPumpkin()));
      });
    }
}

[System.Serializable]
[ObjectInfo("pumpkin", "Round and heavy and filled with raw calories.")]
public class ItemPumpkin : Item, IEdible {
  public ItemPumpkin() {}

  public void Eat(Actor a) {
    a.statuses.Add(new StrengthStatus(4));
    a.floor.Put(new ItemOnGround(a.pos, new ItemPumpkinHelmet(), a.pos));
    Destroy();
  }

  internal override string GetStats() => "Gives 4 stacks of Strength.\nMakes a nice helmet after you eat it.";
}

[System.Serializable]
[ObjectInfo("strength", "You're feeling good!")]
class StrengthStatus : StackingStatus, IAttackDamageModifier {
  public StrengthStatus(int stacks) : base(stacks) {}

  public int Modify(int input) {
    stacks--;
    return input + 1;
  }

  public override string Info() => $"Your next {stacks} attacks deal +1 damage!";
}