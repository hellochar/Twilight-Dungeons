using System;
using UnityEngine;

// A piece on the home garden.
[Serializable]
public class Piece : Entity {
  // private Vector2Int _pos;
  public override Vector2Int pos {
    get; set;
    // get => _pos;
    /// do not allow moving Pieces
    // set { }
  }
  public Soil soil => floor.soils[pos];
  public int dayCreated { get; }
  public int dayAge => GameModel.main.day - dayCreated;
  public Piece(Vector2Int pos) : base() {
    this.pos = pos;
    dayCreated = GameModel.main.day;
  }

  public ItemOfPiece BecomeItem() {
    if (floor != null) {
      floor.Remove(this);
    }
    return new ItemOfPiece(this);
  }
}

public interface IDaySteppable {
  void StepDay();
}

// A Piece that represents an Entity in the caves such as a 
// Grass or Creature
[Serializable]
public class CavePiece<T> : Piece where T : Entity {
  public CavePiece(Vector2Int pos, T type) : base(pos) {
  }
}

[Serializable]
public class HomeInventory : Inventory {
  public override bool AddItem(Item item, Entity source = null, bool expandToFit = false) {
    if (item is ItemOfPiece) {
      return base.AddItem(item, source, expandToFit);
    }
    return false;
  }
}

[Serializable]
public class ItemOfPiece : Item {
  Piece piece;

  public ItemOfPiece(Piece p) {
    this.piece = p;
  }
}