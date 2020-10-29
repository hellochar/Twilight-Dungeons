﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchPlayerState : MonoBehaviour {
  public Player player;

  // Start is called before the first frame update
  void Start() {
    this.player = GameModel.main.player;
    this.transform.position = Util.withZ(this.player.pos);
  }

  // Update is called once per frame
  void Update() {
    // sync positions
    this.transform.position = Util.withZ(Vector2.Lerp(Util.getXY(this.transform.position), player.pos, 10f * Time.deltaTime));
  }
}
