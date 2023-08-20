using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class UpstairsController : TileController, IOnTopActionHandler {
  public Upstairs upstairs => (Upstairs) tile;
  public GameObject shiny;

  public string OnTopActionName => ((IOnTopActionHandler)upstairs).OnTopActionName;
  public void HandleOnTopAction() {
      ((IOnTopActionHandler)upstairs).HandleOnTopAction();
  }

  void Update() {
    bool bHasMaturePlantAtHome = GameModel.main.home.bodies.OfType<Plant>().Any(p => p.IsMature);
    if (bHasMaturePlantAtHome && !shiny.activeSelf) {
      shiny.SetActive(true);
    }
  }
}
