using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DPadController : MonoBehaviour {

  void Start() {
    // this will work even while the gameobject is inactive.
    Settings.OnChanged += HandleSettingsChanged;
    MatchSettings();
  }

  private void HandleSettingsChanged() {
    MatchSettings();
  }

  void MatchSettings() {
    gameObject.SetActive(Settings.main.moveMode.HasFlag(MoveMode.DPad));
  }

  public void MovePlayer(int dx, int dy) {
    var floorController = GameModelController.main.CurrentFloorController;
    var pos = GameModel.main.player.pos + new Vector2Int(dx, dy);
    floorController.UserInteractAt(pos, null);
  }

  public void UpLeft() => MovePlayer(-1, 1);
  public void Up() => MovePlayer(0, 1);
  public void UpRight() => MovePlayer(1, 1);

  public void Left() => MovePlayer(-1, 0);
  public void Stop() => MovePlayer(0, 0);
  public void Right() => MovePlayer(1, 0);

  public void DownLeft() => MovePlayer(-1, -1);
  public void Down() => MovePlayer(0, -1);
  public void DownRight() => MovePlayer(1, -1);
}
