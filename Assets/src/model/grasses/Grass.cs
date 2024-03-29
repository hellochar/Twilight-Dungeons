using System;
using System.Collections.Generic;
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
  public override void HandleDeserialized(StreamingContext context) {
    base.HandleDeserialized(context);
    OnNoteworthyAction = delegate {};
  }

  public Grass(Vector2Int pos) : base() {
    this._pos = pos;
  }
}
