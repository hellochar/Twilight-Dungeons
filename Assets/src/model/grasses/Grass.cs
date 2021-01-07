using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Grass : SteppableEntity {
  private Vector2Int _pos;
  public override Vector2Int pos {
    get => _pos;
    /// do not allow moving grass
    set { }
  }

  public event Action OnNoteworthyAction;

  public Grass(Vector2Int pos) : base() {
    this._pos = pos;
    timeNextAction = this.timeCreated + 99999;
  }

  protected override float Step() {
    /// TODO make SteppableEntity an Interface
    return 99999;
  }

  /// The UI will do *something* in response to this
  public void TriggerNoteworthyAction() {
    OnNoteworthyAction?.Invoke();
  }
}
