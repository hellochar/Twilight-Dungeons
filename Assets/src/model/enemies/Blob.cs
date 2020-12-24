using System;
using System.Collections.Generic;
using UnityEngine;

public class Blob : AIActor {
  public Blob(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 8;
    faction = Faction.Enemy;
    ai = AIs.BlobAI(this).GetEnumerator();
    inventory.AddItem(new ItemGoop(1));
  }

  internal override int BaseAttackDamage() {
    return UnityEngine.Random.Range(2, 4);
  }
}

[ObjectInfo(spriteName: "goop", flavorText: "Weirdly healing")]
public class ItemGoop : EquippableItem, IStackable, IBaseActionModifier {
  public override EquipmentSlot slot => EquipmentSlot.Feet;
  private int turnsLeft = 50;

  public ItemGoop(int stacks) {
    this.stacks = stacks;
  }

  public int stacksMax => 100;

  private int _stacks;
  public int stacks {
    get => _stacks;
    set {
      if (value < 0) {
        throw new ArgumentException("Setting negative stack!" + this + " to " + value);
      }
      _stacks = value;
      if (_stacks == 0) {
        Destroy();
      }
    }
  }

  public BaseAction Modify(BaseAction input) {
    if (input.Type == ActionType.MOVE) {
      if (turnsLeft <= 0) {
        GameModel.main.player.Heal(1);
        turnsLeft = 50;
        stacks--;
      }
      turnsLeft--;
    }
    return input;
  }

  internal override string GetStats() => $"While equipped, every 50 turns you heal 1 hp and use 1 stack.\nNeed {turnsLeft} turns.";
}