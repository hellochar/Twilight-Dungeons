using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// disallow camera from seeing "outside" the edge of the map
public class BoundCameraToFloor : MonoBehaviour {
  public Bounds bounds = new Bounds(new Vector3(Floor.WIDTH / 2 - 0.5f, Floor.HEIGHT / 2 - 0.5f, 0), new Vector3(Floor.WIDTH, Floor.HEIGHT, 1));
  new public Camera camera;

  // Start is called before the first frame update
  void Start() {
    this.camera = GetComponent<Camera>();
  }

  // Update is called once per frame
  void Update() {
    Bounds cameraBounds = OrthographicBounds(this.camera);
    float newX = this.transform.position.x;
    float newY = this.transform.position.y;
    if (cameraBounds.min.x < bounds.min.x) {
      newX += bounds.min.x - cameraBounds.min.x;
    } else if (cameraBounds.max.x > bounds.max.x) {
      newX += bounds.max.x - cameraBounds.max.x;
    }
    if (cameraBounds.min.y < bounds.min.y) {
      newY += bounds.min.y - cameraBounds.min.y;
    } else if (cameraBounds.max.y > bounds.max.y) {
      newY += bounds.max.y - cameraBounds.max.y;
    }
    this.transform.position = new Vector3(newX, newY, this.transform.position.z);
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
