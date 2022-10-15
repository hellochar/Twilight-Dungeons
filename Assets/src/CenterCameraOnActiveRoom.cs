using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CenterCameraOnActiveRoom : MonoBehaviour {
  private Bounds bounds = new Bounds(new Vector3(-999, -999), new Vector3(999, 999));
  public float paddingTop = 2, paddingRight = 2, paddingBottom = 2, paddingLeft = 2;
  public float minZoom = 0;
  public float followSpeed = 1f;
  public float jumpThreshold = 1f;
  public float zoomSpeed = 1f;
  new private Camera camera;

  // Start is called before the first frame update
  void Start() {
    this.camera = GetComponent<Camera>();
  }

  static Vector2 half = new Vector2(0.5f, 0.5f);

#if experimental_chainfloors
  void LateUpdate() {
    var activeRoom = GameModel.main.player.room;

    if (activeRoom != null) {
      Bounds bounds = new Bounds();
      bounds.min = Util.withZ(activeRoom.min - half) + new Vector3(-paddingLeft, -paddingBottom, 0);
      bounds.max = Util.withZ(activeRoom.max + half) + new Vector3(paddingRight, paddingTop, 0);

      // this.transform.position = new Vector3(bounds.center.x, bounds.center.y - 0.5f, this.transform.position.z);
      transform.position = CameraFollowEntity.LerpTowardsPosition(
        transform.position,
        new Vector3(bounds.center.x, bounds.center.y - 0.5f, this.transform.position.z),
        followSpeed,
        jumpThreshold
      );
      var zoom = Mathf.Max(minZoom, bounds.extents.y);
      CameraZoom.ZoomLerp(camera, zoom, zoomSpeed);
    }
  }
#endif
}
