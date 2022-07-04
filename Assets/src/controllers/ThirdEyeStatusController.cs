using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ThirdEyeStatusController : StatusController {
  public Dictionary<Actor, GameObject> mapping = new Dictionary<Actor, GameObject>();
  public override void Start() {
    base.Start();
    LateUpdate();
  }

  void OnDestroy() {
    // remove mapping
    foreach (var worldText in mapping.Values) {
      Destroy(worldText);
    }
  }

  void LateUpdate() {
    var player = GameModel.main.player;
    if (player.IsDead) {
      return;
    }
    var actorsInSight = new HashSet<Actor>(player.ActorsInSight(Faction.Ally | Faction.Enemy));
    actorsInSight.Remove(player);
    // create
    foreach (var actor in actorsInSight) {
      if (!mapping.ContainsKey(actor)) {
        var worldText = PrefabCache.UI.Instantiate("Persistent WorldText");
        mapping.Add(actor, worldText);
      }
    }
    // delete
    var outdatedActors = mapping.Keys.Except(actorsInSight).ToList();
    foreach (var outdatedActor in outdatedActors) {
      var worldText = mapping[outdatedActor];
      Destroy(worldText);
      mapping.Remove(outdatedActor);
    }
    // update
    foreach (var actor in mapping.Keys) {
      var worldText = mapping[actor];
      var text = worldText.GetComponentInChildren<TMPro.TMP_Text>();
      text.text = $"{actor.hp}/{actor.maxHp}";

      var actorController = GameModelController.main.CurrentFloorController.GameObjectFor(actor);
      if (actorController != null) {
        worldText.transform.position = actorController.transform.position + new Vector3(0, -0.30f, 0);
      }
    }
  }
}
