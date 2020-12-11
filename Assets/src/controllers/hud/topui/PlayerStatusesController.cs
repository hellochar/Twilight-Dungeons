using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatusesController : MonoBehaviour {
  GameObject statusIconPrefab;
  Player player;
  // Start is called before the first frame update
  void Start() {
    player = GameModel.main.player;
    statusIconPrefab = Resources.Load<GameObject>("UI/Status Icon");
    player.statuses.OnAdded += HandleStatusAdded;
  }

  private void HandleStatusAdded(Status status) {
    var statusIndicator = Instantiate(statusIconPrefab, transform.position, Quaternion.identity, transform);
    statusIndicator.GetComponent<StatusIconController>().status = status;
  }
}
