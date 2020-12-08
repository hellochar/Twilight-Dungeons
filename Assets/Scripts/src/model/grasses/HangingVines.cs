using System;
using System.Collections.Generic;
using UnityEngine;

public class HangingVines : Grass {
  public HangingVines(Vector2Int pos) : base(pos) {
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
  }

  public Tile tileBelow => floor.tiles[pos + new Vector2Int(0, -1)];

  private void HandleEnterFloor() {
    tileBelow.OnActorEnter += HandleActorEnter;
  }

  private void HandleLeaveFloor() {
    tileBelow.OnActorEnter -= HandleActorEnter;
  }

  private void HandleActorEnter(Actor who) {
    var stuckStatus = new BoundStatus();
    who.statuses.Add(stuckStatus);
    stuckStatus.OnRemoved += HandleStatusRemoved;
  }

  private void HandleStatusRemoved() {
    // when someone is able to break free; remove these vines
    Kill();
  }
}
