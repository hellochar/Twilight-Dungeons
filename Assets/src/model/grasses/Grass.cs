using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

public delegate void OnNoteworthyAction();

[Serializable]
public class Grass : Entity, IDaySteppable {
  private Vector2Int _pos;
  public override Vector2Int pos {
    get => _pos;
    /// do not allow moving grass
    set { }
  }
  public virtual object BodyModifier { get; protected set; }

  [NonSerialized] /// controller only
  public OnNoteworthyAction OnNoteworthyAction = delegate {};
  [OnDeserialized]
  void HandleDeserialized() {
    OnNoteworthyAction = delegate {};
  }

  public Grass(Vector2Int pos) : base() {
    this._pos = pos;
  }

  public void BecomeItemInInventory(Item innerItem, Player player) {
    var floor = this.floor;
    var item = new ItemVisibleBox(innerItem);
    Kill(actor);
    if (!player.inventory.AddItem(item, this)) {
      floor.Put(new ItemOnGround(pos, item, pos));
    }
  }

  public virtual void StepDay() {
    // spread to a nearby tile
    if (tile is Soil) {
    }
  }

  public void SpreadToOneNearbySoil() {
      var canOccupyMethod = GetType().GetMethod("CanOccupy", System.Reflection.BindingFlags.Static);
      bool canOccupy(Tile t) {
        if (canOccupyMethod != null) {
          return (bool) canOccupyMethod.Invoke(null, new object[] { t });
        }
        return t is Ground;
      }
      var openSpot = Util.RandomPick(floor.GetAdjacentTiles(pos).Where(t => t is Soil && canOccupy(t) && t.grass == null && t.CanBeOccupied()));
      if (openSpot != null) {
        var constructor = GetType().GetConstructor(new Type[1] { typeof(Vector2Int) });
        var newGrass = (Grass)constructor.Invoke(new object[] { openSpot.pos });
        grass.floor.Put(newGrass);
      }
  }
}
