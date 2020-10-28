using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TouchToMove : MonoBehaviour {
  public Vector2Int? target;
  public GameObject reticle;
  public GameObject pathDotPrefab;

  // Get the entity represented by this GameObject
  public Entity entity;

  public List<Vector2Int> currentPath;
  public List<GameObject> currentPathSprites;

  // Start is called before the first frame update
  void Start() {
    this.pathDotPrefab = Resources.Load<GameObject>("PathDotSprite");
    reticle.SetActive(false);
    // hard code for now
    this.entity = GameModel.model.player;
  }

  // Update is called once per frame
  void Update() {
    UpdateSetTarget();
    UpdateRemoveTarget();
    UpdateMove();
    UpdateReticle();
  }

  void UpdateMove() {
    if (this.target == null) {
      return;
    }
    if (Time.frameCount % 50 == 0) {
      Vector2Int target = this.target.Value;
      Floor floor = GameModel.model.floors[GameModel.model.activeFloorIndex];
      if (this.currentPath == null) {
        this.currentPath = floor.FindPath(this.entity.pos, target);
        this.currentPathSprites = currentPath.Select(pos => Instantiate(pathDotPrefab, Util.withZ(pos, 0), Quaternion.identity)).ToList();
      }
      if (this.currentPath.Count > 0) {
        // take the next direction off the path and move in that direction
        Vector2Int nextPosition = currentPath[0];
        currentPath.RemoveAt(0);
        Destroy(this.currentPathSprites[0]);
        this.currentPathSprites.RemoveAt(0);
        this.entity.pos = nextPosition;
      }
      if (currentPath.Count == 0) {
        currentPath = null;
        this.target = null;
      }
    }
  }

  // Set target on mouse click or mobile touch
  void UpdateSetTarget() {
    // mouse click
    if (Input.GetMouseButtonDown(0)) {
      // set target
      setTarget(Input.mousePosition);
    }
    // mobile touch
    if (Input.touchCount > 0) {
      Touch t = Input.GetTouch(0);
      if (t.phase == TouchPhase.Began) {
        setTarget(t.position);
      }
    }
  }

  private void setTarget(Vector3 screenPoint) {
    Vector3 worldTarget = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    target = new Vector2Int(Mathf.RoundToInt(worldTarget.x), Mathf.RoundToInt(worldTarget.y));
    // clear existing path
    this.currentPath = null;
    if (this.currentPathSprites != null) {
      this.currentPathSprites.ForEach(sprite => Destroy(sprite));
      this.currentPathSprites = null;
    }
  }

  // remove target if we're close enough
  void UpdateRemoveTarget() {
    if (Util.getXY(this.transform.position).Equals(target)) {
      target = null;
    }
  }

  void UpdateReticle() {
    if (target != null) {
      reticle.SetActive(true);
      reticle.transform.position = Util.withZ(target.Value, reticle.transform.position.z);
    } else {
      reticle.SetActive(false);
    }
  }
}
