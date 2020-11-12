using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchInventoryState : MonoBehaviour {
  private Inventory inventory;

  void Start() {
    inventory = GameModel.main.player.inventory;
  }

  void Update() {
    /// TODO add more or less slots as inventory changes
  }
}
