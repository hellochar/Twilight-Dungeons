using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Bladegrass : Grass {
  public static bool CanOccupy(Tile tile) => tile is Ground;
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

  public void Sharpen() {
    if (!isSharp) {
      isSharp = true;
      OnSharpened?.Invoke();
      AddTimedEvent(10, () => Kill());
    }
  }

  private void HandleActorLeave(Actor obj) {
    Sharpen();
  }

  public void HandleActorEnter(Actor actor) {
    if (isSharp) {
      actor.TakeDamage(2);
      Kill();
    }
  }
}
