using System;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Tap to pick up this item and see what it does.")]
public class ItemOnGround : Entity {
  public static bool CanOccupy(Tile tile) => tile.CanBeOccupied() && tile.item == null;

  private Vector2Int _pos;
  public override Vector2Int pos {
    get => _pos;
    set => _pos = value;
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
}