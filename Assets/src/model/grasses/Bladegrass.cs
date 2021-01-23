using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ObjectInfo(description: "Walk over to sharpen.\nOnce sharpened, any creature walking into this Bladegrass takes 2 damage and kills it.")]
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
      Kill();
      actor.TakeDamage(2);
    }
  }
}
