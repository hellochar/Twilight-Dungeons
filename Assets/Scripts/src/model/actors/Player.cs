using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


public class Player : Actor {
  public static readonly int MAX_FULLNESS = 1000;
  public Inventory inventory { get; }
  public Equipment equipment { get; }
  /// 1000 is max fullness
  public int fullness = MAX_FULLNESS;

  internal override float queueOrderOffset => 0f;

  public Player(Vector2Int pos) : base(pos) {
    faction = Faction.Ally;
    inventory = new Inventory(this, 12);
    inventory.AddItem(new ItemBarkShield());
    inventory.AddItem(new ItemBerries(3));
    inventory.AddItem(new ItemSeed());
    equipment = new Equipment(this);
    hp = hpMax = 12;
    OnStepped += HandleStepped;
  }

  void HandleStepped(ActorAction action, int timeCost) {
    fullness = Math.Max(fullness - timeCost, 0);
    // you are now starving
    if (fullness <= 0) {
      this.TakeDamage(1, this);
    }
  }

  public override Vector2Int pos {
    get {
      return base.pos;
    }

    set {
      GameModel model = GameModel.main;
      if (floor != null) {
        floor.RemoveVisibility(this);
      }
      base.pos = value;
      if (floor != null) {
        floor.AddVisibility(this);
        Tile t = floor.tiles[value.x, value.y];
        model.EnqueueEvent(() => t.OnPlayerEnter());
      }
    }
  }

  internal void IncreaseFullness(float v) {
    int amount = (int) (v * MAX_FULLNESS);
    fullness = Mathf.Clamp(fullness + amount, 0, MAX_FULLNESS);
  }

  protected override int ModifyDamage(int damage) {
    foreach (var item in equipment.ItemsNonNull()) {
      if (item is IDamageModifier damageModifier && damage > 0) {
        damage = damageModifier.ModifyDamage(damage);
      }
    }
    return damage;
  }
}
