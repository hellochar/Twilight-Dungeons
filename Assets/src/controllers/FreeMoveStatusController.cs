using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeMoveStatusController : StatusController {
  public GameObject particles;

  public Vector2Int startPosition;
  public float motionTime = 0.116f;

  public override void Start() {
    base.Start();
    if (status.list != null) {
      startPosition = status.actor.pos;
      particles.SetActive(false);
    }
  }

  void Update() {
    if (!particles.activeSelf) {
      // wait until we're at the target location to start
      var distanceToStart = Vector2.Distance(
        Util.getXY(gameObject.transform.position),
        startPosition
      );
      if (distanceToStart < 0.1f) {
        particles.SetActive(true);
      }
    }
  }

  private IEnumerator WaitForMotionAndFinish() {
    yield return new WaitForSeconds(motionTime);
    // finish the particle system, whose DestroyAtAnimationEnd will Destroy this
    var particleSystem = particles.GetComponent<ParticleSystem>();
    var main = particleSystem.main;
    main.loop = false;
  }

  protected override void HandleRemoved() {
    // HandleRemoved gets called the moment the GameObject *starts* its free move.
    // We must wait for a few frames, and *then* finish.
    StartCoroutine(WaitForMotionAndFinish());
  }
}
