using System;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Tap to pick up this item and see what it does.")]
public class ItemOnGround : Entity {
  public static bool CanOccupy(Tile tile) => tile is Ground && tile.item == null && (tile.CanBeOccupied() || tile.body is Player);
  public static void PlacementBehavior(Floor floor, ItemOnGround i) {
    var newPosition = floor.BreadthFirstSearch(i.pos, (_) => true)
      .Where(ItemOnGround.CanOccupy)
      .FirstOrDefault()
      ?.pos ?? null;
    if (newPosition.HasValue) {
      i._pos = newPosition.Value;
    }
  }

  private Vector2Int _pos;
  public override Vector2Int pos {
    get => _pos;
    set { }
  }

  public readonly new Item item;
  public Vector2Int? start;

  public ItemOnGround(Vector2Int pos, Item item, Vector2Int? start = null) : base() {
    this.start = start;
    this._pos = pos;
    this.item = item;
    item.OnDestroyed += HandleItemDestroyed;
    Debug.AssertFormat(item.inventory == null, "Item's inventory should be null");
  }

  [OnDeserialized]
  void HandleDeserialized() {
    item.OnDestroyed += HandleItemDestroyed;
  }

  private void HandleItemDestroyed() {
    KillSelf();
  }

  public virtual void PickUp() {
    var player = GameModel.main.player;
    if (!IsNextTo(player)) {
      return;
    }

    Inventory inventory = player.inventory;
#if experimental_autoequip
    if (item is EquippableItem) {
      inventory = player.equipment;
    }
#endif
    if (inventory.AddItem(item, this)) {
      Kill(player);
    }
  }

  public virtual void StepDay() {
    if (age > 0) {
      var floor = this.floor;
      KillSelf();
      int numOrganicMatters = YieldContributionUtils.GetCost(item) / 2;
      for (int i = 0; i < numOrganicMatters; i++) {
        floor.Put(new OrganicMatterOnGround(pos));
      }
    }
  }

  // public void HandleActorEnter(Actor who) {
  //   if (who == GameModel.main.player) {
  //     PickUp();
  //   }
  // }

  // BUGFIX - prior to 1.10.0 the _pos was erroneously marked nonserialized, meaning
  // players would lose all items on the ground when they saved and loaded.
  // Thankfully, the "start" position kind of keeps track of it, so we graft it onto
  // the pos and then re-register the entity
  public void FixPosZeroBug(int x, int y) {
    this._pos.Set(x, y);
  }
}