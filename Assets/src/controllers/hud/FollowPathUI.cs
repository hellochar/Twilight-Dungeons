﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FollowPathUI : MonoBehaviour {
  private GameObject reticle;
  private GameObject pathDotPrefab;
  private Player player;
  public List<GameObject> pathDots;

  // Start is called before the first frame update
  void Start() {
    pathDotPrefab = Resources.Load<GameObject>("UI/PathDot");
    reticle = Instantiate(Resources.Load<GameObject>("UI/Reticle"), new Vector3(), Quaternion.identity, transform);
    reticle.SetActive(false);
    player = GameModel.main.player;
    player.OnSetTask += HandleSetPlayerTask;
  }

  void HandleSetPlayerTask(ActorTask action) {
    if (action is FollowPathTask) {
      this.ResetPathDots();
    }
  }

  // Update is called once per frame
  void Update() {
    // MaybeSetTarget();
    UpdatePathSprites();
    UpdateReticle();
  }

  void UpdatePathSprites() {
    if (!(player.task is FollowPathTask)) {
      if (pathDots != null) {
        this.ResetPathDots();
      }
      return;
    }
    FollowPathTask action = (FollowPathTask) player.task;
    if (pathDots == null) {
      this.pathDots = action.path.Select(pos => Instantiate(pathDotPrefab, Util.withZ(pos, 0), Quaternion.identity, transform)).ToList();
    }
    // remove paths as they're tackled
    while(pathDots.Count > action.path.Count) {
      Destroy(pathDots[0]);
      pathDots.RemoveAt(0);
    }
  }

  private void ResetPathDots() {
    if (this.pathDots != null) {
      this.pathDots.ForEach(sprite => Destroy(sprite));
      this.pathDots = null;
    }
  }

  void UpdateReticle() {
    if (player.task is FollowPathTask) {
      reticle.SetActive(true);
      reticle.transform.position = Util.withZ(( (FollowPathTask) player.task).target, reticle.transform.position.z);
    } else {
      reticle.SetActive(false);
    }
  }
}