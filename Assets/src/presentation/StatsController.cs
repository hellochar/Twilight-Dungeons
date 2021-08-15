using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatsController : MonoBehaviour {
  void Start() {
    var stats = GameModel.main.stats;
    var tmp = GetComponent<TMPro.TMP_Text>();
    tmp.text = $@"
Turns Taken: {stats.timeTaken}
Water Collected: {stats.waterCollected}
Floors Cleared: {stats.floorsCleared}
Damage Dealt: {stats.damageDealt}
Damage Taken: {stats.damageTaken}
Enemies Defeated: {stats.enemiesDefeated}
Plants Planted: {stats.plantsPlanted}
    ".Trim();
  }
}
