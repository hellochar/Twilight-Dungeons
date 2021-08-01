using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Walk over it to harvest.")]
public class Agave : Grass, IActorEnterHandler {
  public static bool CanOccupy(Tile tile) => Mushroom.CanOccupy(tile);
  public Agave(Vector2Int pos) : base(pos) {}

  public void HandleActorEnter(Actor actor) {
    if (actor == GameModel.main.player) {
      GameModel.main.player.inventory.AddItem(new ItemAgave(1), this);
      Kill(actor);
    }
  }
}

[Serializable]
[ObjectInfo("agave", flavorText: "An unassuming and earthy succulent with thick, nutrient bearing leaves.")]
class ItemAgave : Item, IStackable {
  public ItemAgave(int stacks) {
    this.stacks = stacks;
  }

  public int stacksMax => 4;
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

  public void Refine(Player player) {
    player.SetTasks(new GenericPlayerTask(player, () => RefineImpl(player)));
  }

  private void RefineImpl(Player player) {
    if (stacks < stacksMax) {
      throw new CannotPerformActionException("Gather 4 stacks of Agave first.");
    }
    if (player.water < 25) {
      throw new CannotPerformActionException("Need <color=lightblue>25</color> water.");
    }
    player.water -= 25;
    player.floor.Put(new ItemOnGround(player.pos, new ItemAgaveHoney(), player.pos));
    Destroy();
  }

  public override List<MethodInfo> GetAvailableMethods(Player player) {
    var methods = base.GetAvailableMethods(player);
    methods.Add(GetType().GetMethod("Refine"));
    return methods;
  }

  internal override string GetStats() => $"Gather {stacksMax} to Refine into Honey - costs <color=lightblue>25</color> water.";
}

[Serializable]
[ObjectInfo("roguelikeSheet_transparent_647", flavorText: "A restorative and tasty treat!")]
class ItemAgaveHoney : Item, IEdible {
  public void Eat(Actor a) {
    a.Heal(1);
    var debuffs = a.statuses.list.Where((s) => s.isDebuff);
    if (debuffs.Count() > 0) {
      foreach (var debuff in debuffs) {
        debuff.Remove();
      }
      GameModel.main.DrainEventQueue();
    }
    Destroy();
  }

  internal override string GetStats() => "Heals 1 HP. Removes all debuffs (red outlined Statuses).";
}