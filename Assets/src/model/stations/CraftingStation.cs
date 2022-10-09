using System;
using UnityEngine;

[Serializable]
[ObjectInfo("station", description: "Build more items here.")]
public class CraftingStation : Station, IDaySteppable {
  public override int maxDurability => 6;
  private Item crafting {
    get => inventory[0];
    set {
      inventory.RemoveItem(inventory[0]);
      if (value != null) {
        inventory.AddItem(value, 0);
      }
    }
  }

  public override string description {
    get {
      if (crafting != null) {
        return $"Will craft a {crafting.displayName} when you return!";
      } else {
        return "Select an item to craft.";
      }
    }
  }

  public override bool isActive => crafting != null;

  public CraftingStation(Vector2Int pos) : base(pos) {}

  [PlayerAction]
  public void Crafting() => Craft(new ItemPlaceableEntity(
    new GrowingEntity(new Vector2Int(),
      new CraftingStation(new Vector2Int())
    )
  ).RequireSpace());

  [PlayerAction]
  public void Shovel() => Craft(new ItemShovel());

  [PlayerAction]
  public void CreatureFood() => Craft(new ItemCreatureFood());

  [PlayerAction]
  public void Campfire() => Craft(new ItemPlaceableEntity(
    new GrowingEntity(new Vector2Int(),
      new Campfire(new Vector2Int())
    )
  ).RequireSpace());

  [PlayerAction]
  public void Desalinator() => Craft(new ItemPlaceableEntity(
    new GrowingEntity(new Vector2Int(),
      new Desalinator(new Vector2Int())
      )
  ).RequireSpace());

  [PlayerAction]
  public void Composter() => Craft(new ItemPlaceableEntity(
    new GrowingEntity(new Vector2Int(),
      new Composter(new Vector2Int())
      )
  ).RequireSpace());

  [PlayerAction]
  public void Cloner() => Craft(new ItemPlaceableEntity(
    new GrowingEntity(new Vector2Int(),
      new Cloner(new Vector2Int())
      )
  ).RequireSpace());

  [PlayerAction]
  public void Modder() => Craft(new ItemPlaceableEntity(
    new GrowingEntity(new Vector2Int(),
      new Modder(new Vector2Int())
    )
  ).RequireSpace());

  [PlayerAction]
  public void Driller() => Craft(new ItemPlaceableEntity(
    new GrowingEntity(new Vector2Int(),
      new Driller(new Vector2Int())
    )
  ));

  public void Craft(Item item) {
    // crafting = item;
    Player player = GameModel.main.player;
    player.UseActionPointOrThrow();
    bool success = player.inventory.AddItem(item, this);
    if (!success) {
      player.floor.Put(new ItemOnGround(player.pos, item, player.pos));
    }
    this.ReduceDurability();
  }

  public void StepDay() {
    // if (crafting != null) {
    //   // poop the item out
    //   var item = crafting;
    //   crafting.inventory.RemoveItem(crafting);
    //   floor.Put(new ItemOnGround(pos, item, pos));
    //   this.ReduceDurability();
    // }
    // crafting = null;
  }
}
