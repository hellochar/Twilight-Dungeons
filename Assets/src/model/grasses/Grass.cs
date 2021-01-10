using System;
using System.Collections.Generic;
using UnityEngine;

public delegate void OnNoteworthyAction();
  public float timeNextAction { get; set; }
  public float turnPriority => 50;

  private Vector2Int _pos;
  public override Vector2Int pos {
    get => _pos;
    /// do not allow moving grass
    set { }
  }

  public OnNoteworthyAction OnNoteworthyAction = delegate {};

  public Grass(Vector2Int pos) : base() {
    this._pos = pos;
    timeNextAction = this.timeCreated + 99999;
  }

  public virtual float Step() {
    /// TODO make SteppableEntity an Interface
    return 99999;
  }
}
