using System;
using System.Collections.Generic;
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

  [NonSerialized]
  public OnNoteworthyAction OnNoteworthyAction = delegate {};

  public Grass(Vector2Int pos) : base() {
    this._pos = pos;
  }
}
