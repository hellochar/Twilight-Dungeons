using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Agave : Grass {
  public static bool CanOccupy(Tile tile) => Mushroom.CanOccupy(tile);
  public Agave(Vector2Int pos) : base(pos) {
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
  }

  void HandleEnterFloor() {
    tile.OnActorEnter += HandleActorEnter;
  }

  void HandleLeaveFloor() {
    tile.OnActorEnter -= HandleActorEnter;
  }

  private void HandleActorEnter(Actor actor) {
    if (actor == GameModel.main.player) {
      GameModel.main.player.inventory.AddItem(new ItemAgave(1), this);
      Kill();
    }
  }
}

[ObjectInfo("agave", "")]
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
    player.SetTasks(new GenericTask(player, (_) => RefineImpl(player)));
  }

  private void RefineImpl(Player player) {
    player.floor.Put(new ItemOnGround(player.pos, new ItemAgaveHoney(), player.pos));
    Destroy();
  }

  public override List<MethodInfo> GetAvailableMethods(Player player) {
    var methods = base.GetAvailableMethods(player);
    if (stacks == stacksMax) {
      methods.Add(GetType().GetMethod("Refine"));
    }
    return methods;
  }

  internal override string GetStats() => $"Gather {stacksMax} stacks to Refine into Honey.";
}

[ObjectInfo("roguelikeSheet_transparent_647", "")]
class ItemAgaveHoney : Item, IEdible {
  public void Eat(Actor a) {
    a.Heal(1);
    // a.statuses.Add(new WellFedStatus(10));
    Destroy();
  }

  internal override string GetStats() => "Perhaps the tastiest thing you've ever tried!";
}