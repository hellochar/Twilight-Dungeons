using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class ItemPlaceableTile : Item, ITargetedAction<Tile> {
  public override int stacksMax => int.MaxValue;
  public string TargettedActionName => "Place";
  public string TargettedActionDescription => $"Place the {Util.WithSpaces(tileType.Name)}.";
  public Type tileType;
  public ItemPlaceableTile(Type tileType, int stacks) : base(stacks) {
    this.tileType = tileType;
  }

  public ItemPlaceableTile(Type tileType) : this(tileType, 1) { }
  public override string displayName => Util.WithSpaces(tileType.Name);
  protected override bool StackingPredicate(Item other) {
    return (other as ItemPlaceableTile).tileType == tileType;
  }

  public void PerformTargettedAction(Player player, Entity target) {
    var tile = (Tile) tileType
      .GetConstructor(new Type[] { typeof(Vector2Int) })
      .Invoke(new object[] { target.pos });
    target.floor.Put(tile);
    stacks--;
  }

  public IEnumerable<Tile> Targets(Player player) {
    return player.GetVisibleTiles().Where(t => t.GetType() != tileType);
  }
}