using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZoom : MonoBehaviour {
  public static float lastZoomTime;
  public static bool IsZoomGuardActive => Time.time - lastZoomTime < 0.5f;
  public float minZoom = 3;
  public float maxZoom = 15;
  // Start is called before the first frame update
  void Start() {

  }

  // Update is called once per frame
  void Update() {
    if (Input.touchSupported) {
      // Pinch to zoom
      if (Input.touchCount == 2) {

        // get current touch positions
        Touch tZero = Input.GetTouch(0);
        Touch tOne = Input.GetTouch(1);
        // get touch position from the previous frame
        Vector2 tZeroPrevious = tZero.position - tZero.deltaPosition;
        Vector2 tOnePrevious = tOne.position - tOne.deltaPosition;

        float oldTouchDistance = Vector2.Distance(tZeroPrevious, tOnePrevious);
        float currentTouchDistance = Vector2.Distance(tZero.position, tOne.position);

        // get offset value
        float deltaDistance = oldTouchDistance - currentTouchDistance;
        Zoom(-deltaDistance / Screen.height * 5f);
      }
    }

    var scroll = Input.mouseScrollDelta.y;
    if (scroll != 0) {
      Zoom(scroll);
    }
  }

  void Zoom(float scroll) {
    lastZoomTime = Time.time;
    var camera = Camera.main;
    var scalar = Mathf.Pow(1.1f, -scroll);
    var newSize = camera.orthographicSize * scalar;
    camera.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
  }
}
