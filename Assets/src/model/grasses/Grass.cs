using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

public delegate void OnNoteworthyAction();

[Serializable]
public class Grass : Entity {
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

  public void BecomeItemInInventory(Item item, Player player) {
    var floor = this.floor;
    Kill(actor);
    if (!player.inventory.AddItem(item, this)) {
      floor.Put(new ItemOnGround(pos, item, pos));
    }
  }

  public void Uproot() {
    if (floor.EnemiesLeft() == 0 && floor.availableToPickGrass) {
      floor.availableToPickGrass = false;
      var whichGrasses = floor.BreadthFirstSearch(pos, t => t.grass?.GetType() == GetType()).Select(t => t.grass).ToList();
      var item = new ItemGrass(GetType(), whichGrasses.Count);
      GameModel.main.player.inventory.AddItem(item, this);
      floor.RemoveAll(whichGrasses);
    }
  }
}
