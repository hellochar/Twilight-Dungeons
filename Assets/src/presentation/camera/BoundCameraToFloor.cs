using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// disallow camera from seeing "outside" the edge of the map
public class BoundCameraToFloor : MonoBehaviour {
  private Bounds bounds = new Bounds(new Vector3(-999, -999), new Vector3(999, 999));
  /// Allow camera bounds to go beyond the map bounds.
  /// Set to 2 to allow the bottom-left corner (where the backpack is) to not be blocked
  public float paddingTop = 2, paddingRight = 2, paddingBottom = 2, paddingLeft = 2;
  new private Camera camera;

  // Start is called before the first frame update
  void Start() {
    this.camera = GetComponent<Camera>();
  }

  // Update is called once per frame
  void LateUpdate() {
    /// bug - if the current floor is less than 1 bounds, the camera shakes

    bounds.min = Util.withZ(GameModel.main.home.root.min) + new Vector3(-paddingLeft, -paddingBottom, 0);
    bounds.max = Util.withZ(GameModel.main.home.root.max) + new Vector3(paddingRight, paddingTop, 0);

    Bounds cameraBounds = OrthographicBounds(this.camera);
    bool constrainX = bounds.extents.x > cameraBounds.extents.x;
    bool constrainY = bounds.extents.y > cameraBounds.extents.y;
    float newX = this.transform.position.x;
    float newY = this.transform.position.y;
    if (constrainX) {
      if (cameraBounds.min.x < bounds.min.x) {
        newX += bounds.min.x - cameraBounds.min.x;
      } else if (cameraBounds.max.x > bounds.max.x) {
        newX += bounds.max.x - cameraBounds.max.x;
      }
    } else {
      // otherwise, set to center
      newX = bounds.center.x;
    }
    if (constrainY) {
      if (cameraBounds.min.y < bounds.min.y) {
        newY += bounds.min.y - cameraBounds.min.y;
      } else if (cameraBounds.max.y > bounds.max.y) {
        newY += bounds.max.y - cameraBounds.max.y;
      }
    } else {
      newY = bounds.center.y;
    }
    this.transform.position = new Vector3(newX, newY, this.transform.position.z);
    /// TODO do a lerp once we do a force/velocity based approach
    // var newPosition = new Vector3(newX, newY, this.transform.position.z);
    // this.transform.position = Vector3.Lerp(transform.position, newPosition, 10f * Time.deltaTime);
  }

  public static Bounds OrthographicBounds(Camera camera) {
    float screenAspect = (float)Screen.width / (float)Screen.height;
    float cameraHeight = camera.orthographicSize * 2;
    Bounds bounds = new Bounds(
        new Vector3(camera.transform.position.x, camera.transform.position.y, 0),
        new Vector3(cameraHeight * screenAspect, cameraHeight, 0));
    return bounds;
  }
}
