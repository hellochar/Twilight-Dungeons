using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// disallow camera from seeing "outside" the edge of the map
public class CenterCameraOnFloor : MonoBehaviour {
  public float paddingTop = 2, paddingRight = 2, paddingBottom = 2, paddingLeft = 2;
  public float minZoom = 0;
  new private Camera camera;

  // Start is called before the first frame update
  void Start() {
    this.camera = GetComponent<Camera>();
  }

  void LateUpdate() {
    Bounds bounds = new Bounds();
    bounds.min = Util.withZ(GameModel.main.currentFloor.boundsMin) + new Vector3(-paddingLeft, -paddingBottom, 0);
    bounds.max = Util.withZ(GameModel.main.currentFloor.boundsMax) + new Vector3(paddingRight, paddingTop, 0);

    this.transform.position = new Vector3(bounds.center.x, bounds.center.y - 0.5f, this.transform.position.z);
    camera.orthographicSize = Mathf.Max(minZoom, bounds.extents.y);
  }
}
