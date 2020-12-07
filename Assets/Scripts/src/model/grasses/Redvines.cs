using System;
using System.Collections.Generic;
using UnityEngine;

public class Redvines : Grass {
  public Redvines(Vector2Int pos) : base(pos) {
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
    var stuckStatus = new StuckStatus();
    who.statuses.Add(stuckStatus);
    stuckStatus.OnRemoved += HandleStatusRemoved;
    // grapple them for a few turns
    // GrappledTask action = new GrappledTask(who, 3, this);
    // action.OnDone += HandleActionDone;
    // who.InsertTasks(action);
  }

  private void HandleStatusRemoved() {
    // when someone is able to break free; remove these vines
    Kill();
  }
}
