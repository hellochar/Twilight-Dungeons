using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TouchToMove : MonoBehaviour {
  public GameObject reticle;
  public GameObject pathDotPrefab;

  // Get the entity represented by this GameObject
  public Actor actor;

  public List<GameObject> currentPathSprites;

  // Start is called before the first frame update
  void Start() {
    this.pathDotPrefab = Resources.Load<GameObject>("PathDotSprite");
    reticle.SetActive(false);
    // hard code for now
    this.actor = GameModel.main.player;
  }

  // Update is called once per frame
  void Update() {
    MaybeSetTarget();
    UpdatePathSprites();
    UpdateReticle();
  }

  void UpdatePathSprites() {
    if (!(actor.action is MoveToTargetAction)) {
      if (currentPathSprites != null) {
        this.ResetPathSprites();
      }
      return;
    }
    MoveToTargetAction action = (MoveToTargetAction) actor.action;
    if (currentPathSprites == null) {
      this.currentPathSprites = action.path.Select(pos => Instantiate(pathDotPrefab, Util.withZ(pos, 0), Quaternion.identity)).ToList();
    }
    // remove paths as they're tackled
    while(currentPathSprites.Count > action.path.Count) {
      Destroy(currentPathSprites[0]);
      currentPathSprites.RemoveAt(0);
    }
  }

  // Set target on mouse click or mobile touch
  void MaybeSetTarget() {
    // mouse click
    if (Input.GetMouseButtonUp(0)) {
      // set target
      setTarget(Input.mousePosition);
    }
    // mobile touch
    if (Input.touchCount > 0) {
      Touch t = Input.GetTouch(0);
      if (t.phase == TouchPhase.Ended) {
        setTarget(t.position);
      }
    }
  }

  private void setTarget(Vector3 screenPoint) {
    Tile tile = Util.GetVisibleTileAt(screenPoint);
    actor.action = new MoveToTargetAction(actor, tile.pos);
    // clear existing path
    if (this.currentPathSprites != null) {
      ResetPathSprites();
    }
  }

  private void ResetPathSprites() {
    this.currentPathSprites.ForEach(sprite => Destroy(sprite));
    this.currentPathSprites = null;
  }

  void UpdateReticle() {
    if (actor.action is MoveToTargetAction) {
      reticle.SetActive(true);
      reticle.transform.position = Util.withZ(( (MoveToTargetAction) actor.action).target, reticle.transform.position.z);
    } else {
      reticle.SetActive(false);
    }
  }
}
