using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BossHPController : MonoBehaviour {
  public Image barFilled;
  public TMPro.TMP_Text hpText;
  public TMPro.TMP_Text bossName;

  void Start() {
    GameModel.main.OnPlayerChangeFloor += HandleChangeFloor;
    GameModel.main.player.OnBossNewlySeen += UpdateActive;
    UpdateActive();
  }

  void UpdateActive() {
    var isActive = GameModel.main.currentFloor.seenBosses.Any();
    gameObject.SetActive(isActive);
  }

  private void HandleChangeFloor(Floor newFloor, Floor oldFloor) {
    UpdateActive();
  }

  void OnDestroy() {
    GameModel.main.OnPlayerChangeFloor -= HandleChangeFloor;
    GameModel.main.player.OnBossNewlySeen -= UpdateActive;
  }

  void Update() {
    var boss = GameModel.main.currentFloor.seenBosses.FirstOrDefault();
    if (boss != null) {
      var hp = boss.hp;
      var maxHp = boss.maxHp;
      barFilled.fillAmount = (float)hp / maxHp;
      hpText.text = $"{hp}/{maxHp}";
      bossName.text = boss.displayName;
    } else {
      // boss died
      UpdateActive();
    }
  }
}
