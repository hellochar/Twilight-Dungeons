using System;
using UnityEngine;

[Serializable]
[ObjectInfo("station", description: "Build more items here.")]
public class CraftingStation : Station, IDaySteppable {
  public override int maxDurability => 7;
  private Item crafting;

  public override string description {
    get {
      if (crafting != null) {
        return $"Will craft a {crafting.displayName} when you return!";
      } else {
        return "Select an item to craft.";
      }
    }
  } 

  public CraftingStation(Vector2Int pos) : base(pos) {}

  [PlayerAction]
  public void CraftCraftingStation() => Craft(new ItemPlaceableEntity(
    new GrowingEntity(new Vector2Int(),
      new CraftingStation(new Vector2Int())
    )
  ).RequireSpace());

  [PlayerAction]
  public void CraftShovel() => Craft(new ItemShovel());

  [PlayerAction]
  public void CraftCampfire() => Craft(new ItemPlaceableEntity(
    new GrowingEntity(new Vector2Int(),
      new Campfire(new Vector2Int())
    )
  ).RequireSpace());

  [PlayerAction]
  public void CraftDesalinator() => Craft(new ItemPlaceableEntity(
    new GrowingEntity(new Vector2Int(),
      new Desalinator(new Vector2Int())
      )
  ).RequireSpace());

  [PlayerAction]
  public void CraftComposter() => Craft(new ItemPlaceableEntity(
    new GrowingEntity(new Vector2Int(),
      new Composter(new Vector2Int())
      )
  ).RequireSpace());

  [PlayerAction]
  public void Driller() => Craft(new ItemPlaceableEntity(
    new GrowingEntity(new Vector2Int(),
      new Driller(new Vector2Int())
    )
  ));

  public void Craft(Item item) {
    crafting = item;
  }

  public void StepDay() {
    if (crafting != null) {
      // poop the item out
      floor.Put(new ItemOnGround(pos, crafting, pos));
      this.ReduceDurability();
    }
    crafting = null;
    // Player player = GameModel.main.player;
    // // player.UseActionPointOrThrow();
    // bool success = player.inventory.AddItem(item, this);
    // if (!success) {
    //   player.floor.Put(new ItemOnGround(player.pos, item, player.pos));
    // }
  }
}
