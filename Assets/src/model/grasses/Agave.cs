using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Walk over it to harvest.")]
public class Agave : Grass, IActorEnterHandler {
  public static Item HomeItem => new ItemAgave();
  public static bool CanOccupy(Tile tile) => Mushroom.CanOccupy(tile);
  public Agave(Vector2Int pos) : base(pos) {}

  public void HandleActorEnter(Actor actor) {
    var player = GameModel.main.player;
    if (actor == player) {
      BecomeItemInInventory(new ItemAgave(), player);
    }
  }
}

[Serializable]
[ObjectInfo("agave", flavorText: "An unassuming and earthy succulent with thick, nutrient bearing leaves.")]
class ItemAgave : Item {
  public override int stacksMax => 4;
  public void Refine(Player player) {
    player.SetTasks(new GenericPlayerTask(player, () => RefineImpl(player)));
  }

  private void RefineImpl(Player player) {
    if (stacks < stacksMax) {
      throw new CannotPerformActionException("Gather 4 stacks of Agave first.");
    }
    player.floor.Put(new ItemOnGround(player.pos, new ItemAgaveHoney(), player.pos));
    Destroy();
  }

  public override List<MethodInfo> GetAvailableMethods(Player player) {
    var methods = base.GetAvailableMethods(player);
    methods.Add(GetType().GetMethod("Refine"));
    return methods;
  }

  internal override string GetStats() => $"Gather 4 to Refine into Honey.\nHoney heals 1 HP and removes all Debuffs (red outlined Statuses).";
}

[Serializable]
[ObjectInfo("agave-honey", flavorText: "A restorative and tasty treat!")]
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

  internal override string GetStats() => "Heals 1 HP and removes all Debuffs (red outlined Statuses).";
}