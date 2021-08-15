using System;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Tap to pick up this item and see what it does.")]
public class ItemOnGround : Entity, IActorEnterHandler {
  public static bool CanOccupy(Tile tile) => tile.CanBeOccupied() && tile.item == null;
  public static void PlacementBehavior(Floor floor, ItemOnGround i) {
    var newPosition = floor.BreadthFirstSearch(i.pos, (_) => true)
      .Where(ItemOnGround.CanOccupy)
      .First()
      .pos;
    i._pos = newPosition;
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
    Debug.AssertFormat(item.inventory == null, "Item's inventory should be null");
  }

  internal void PickUp() {
    var player = GameModel.main.player;
    if (IsNextTo(player) && player.inventory.AddItem(item, this)) {
      Kill(player);
    }
  }

  public void HandleActorEnter(Actor who) {
    if (who == GameModel.main.player) {
      PickUp();
    }
  }

  // BUGFIX - prior to 1.10.0 the _pos was erroneously marked nonserialized, meaning
  // players would lose all items on the ground when they saved and loaded.
  // Thankfully, the "start" position kind of keeps track of it, so we graft it onto
  // the pos and then re-register the entity
  public void FixPosZeroBug(int x, int y) {
    this._pos.Set(x, y);
  }
}