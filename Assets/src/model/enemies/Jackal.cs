using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
// run fast, fear other jackals nearby when they die
public class Jackal : AIActor {
  public static new ActionCosts StaticActionCosts = new ActionCosts(Actor.StaticActionCosts) {
    [ActionType.MOVE] = 0.67f,
  };

  protected override ActionCosts actionCosts => Jackal.StaticActionCosts;
  public Jackal(Vector2Int pos) : base(pos) {
    faction = Faction.Enemy;
    hp = baseMaxHp = 2;
    ai = AIs.JackalAI(this).GetEnumerator();
    OnDeath += HandleDeath;
    inventory.AddItem(new ItemJackalFur(1));
  }

  private void HandleDeath() {
    foreach (var jackal in floor.ActorsInCircle(pos, 7).Where((actor) => actor is Jackal)) {
      jackal.SetTasks(new RunAwayTask(jackal, pos, 6));
    }
  }

  internal override int BaseAttackDamage() {
    return UnityEngine.Random.Range(1, 3);
  }
}

[ObjectInfo(spriteName: "jackal-fur", flavorText: "Patches of matted fur strewn together.")]
internal class ItemJackalFur : EquippableItem, IStackable, IMaxHPModifier {
  public override EquipmentSlot slot => EquipmentSlot.Body;

  public ItemJackalFur(int stacks) {
    this.stacks = stacks;
  }

  public int stacksMax => 10;

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

  public int Modify(int input) {
    if (stacks == stacksMax) {
      return input + 4;
    } else {
      return input;
    }
  }

  internal override string GetStats() => "At 10 stacks, you get +4 max HP.";
}