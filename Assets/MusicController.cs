using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// following situations:
// not in boss floor - normal music
// in boss floor, haven't seen boss - no music
// in boss floor, seen boss - boss music
// in boss floor, boss is dead - no music
public class MusicController : MonoBehaviour {
  public AudioSource normal;
  public AudioSource boss;
  // Start is called before the first frame update
  void Start() {
    // GameModel.main.OnPlayerChangeFloor += HandleChangeFloor;
    // GameModel.main.player.OnBossNewlySeen += HandleBossNewlySeen;
    // UpdateMusic();
  }

  void OnDestroy() {
    // GameModel.main.OnPlayerChangeFloor -= HandleChangeFloor;
    // GameModel.main.player.OnBossNewlySeen -= UpdateMusic;
  }

  public void NoMusic() {
    if (normal.isPlaying) {
      normal.Pause();
    }
    if (boss.isPlaying) {
      boss.Pause();
    }
  }

  public void NormalMusic() {
    if (!normal.isPlaying) {
      normal.Play();
    }
    if (boss.isPlaying) {
      boss.Pause();
    }
  }

  public void BossMusic() {
    if (normal.isPlaying) {
      normal.Pause();
    }
    if (!boss.isPlaying) {
      boss.Play();
    }
  }

  public void Update() {
    var floor = GameModel.main.currentFloor;
    if (floor is BossFloor bossFloor) {
      var seesBoss = bossFloor.seenBosses.Any();
      if (seesBoss) {
        BossMusic();
      } else {
        NoMusic();
      }
    } else {
      NormalMusic();
    }
  }
}
