using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatusesController : MonoBehaviour, IStatusAddedHandler {
  GameObject statusIconPrefab;
  [NonSerialized]
  Player player;
  // Start is called before the first frame update
  void Start() {
    player = GameModel.main.player;
    player.nonserializedModifiers.Add(this);
    statusIconPrefab = Resources.Load<GameObject>("UI/Status Icon");

    /// match statuses that already exist
    foreach (var s in player.statuses.list) {
      HandleStatusAdded(s);
    }
  }

  public void HandleStatusAdded(Status status) {
    var statusIndicator = Instantiate(statusIconPrefab, transform);
    statusIndicator.GetComponent<StatusIconController>().status = status;
  }
}
