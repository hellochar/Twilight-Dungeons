using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatusesTopUIController : MonoBehaviour {
  GameObject statusIndicatorPrefab;
  Player player;
  // Start is called before the first frame update
  void Start() {
    player = GameModel.main.player;
    statusIndicatorPrefab = Resources.Load<GameObject>("UI/Status Indicator");
    player.statuses.OnAdded += HandleStatusAdded;
  }

  private void HandleStatusAdded(Status status) {
    var statusIndicator = Instantiate(statusIndicatorPrefab, transform.position, Quaternion.identity, transform);
    statusIndicator.GetComponent<StatusIconController>().status = status;
  }
}
