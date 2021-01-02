using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Bladegrass : Grass {
  public bool isSharp = false;
  public event Action OnSharpened;
  public Bladegrass(Vector2Int pos) : base(pos) {
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
  }

  private void HandleEnterFloor() {
    tile.OnActorLeave += HandleActorLeave;
    tile.OnActorEnter += HandleActorEnter;
  }

  private void HandleLeaveFloor() {
    tile.OnActorLeave -= HandleActorLeave;
    tile.OnActorEnter -= HandleActorEnter;
  }

  private void HandleActorLeave(Actor obj) {
    if (!isSharp) {
      isSharp = true;
      OnSharpened?.Invoke();
      AddTimedEvent(10, () => Kill());
    }
  }

  private void HandleActorEnter(Actor actor) {
    if (isSharp) {
      actor.TakeDamage(2);
      Kill();
    }
  }
}
