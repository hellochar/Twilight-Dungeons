using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZoom : MonoBehaviour {
  public static float lastZoomTime;
  public static bool IsZoomGuardActive => Time.time - lastZoomTime < 0.5f;
  public float wantedZoom = 5;
  public float minZoom = 3;
  public float maxZoom = 15;
  bool acceptUserInput = true;
  public float lerpSpeed = 4;

  // Start is called before the first frame update
  void Start() {
    wantedZoom = PlayerPrefs.GetFloat("zoom", this.wantedZoom);
  }

  public static void ZoomLerp(Camera camera, float wantedZoom, float lerpSpeed) {
    if (Mathf.Abs(camera.orthographicSize - wantedZoom) < 0.01) {
      camera.orthographicSize = wantedZoom;
    } else {
      camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, wantedZoom, lerpSpeed * Time.deltaTime);
    }
  }

  // Update is called once per frame
  void Update() {
    var camera = Camera.main;
    if (zoomAnimation == null) {
      ZoomLerp(camera, wantedZoom, lerpSpeed);
    }
    if (Input.touchSupported && acceptUserInput) {
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
        Zoom(-deltaDistance / Screen.height * 8.75f);
      }
    }

    var scroll = Input.mouseScrollDelta.y;
    if (scroll != 0 && acceptUserInput) {
      Zoom(scroll);
    }
  }

  void Zoom(float scroll) {
    lastZoomTime = Time.time;
    var camera = Camera.main;
    var scalar = Mathf.Pow(1.1f, -scroll);
    wantedZoom = Mathf.Clamp(wantedZoom * scalar, minZoom, maxZoom);
    camera.orthographicSize = wantedZoom;
    PlayerPrefs.SetFloat("zoom", wantedZoom);
  }

  Coroutine zoomAnimation = null;
  internal void PlayFloorZoomAnimation() {
    // disable for now
    // zoomAnimation = StartCoroutine(Transitions.Animate(1f, (t) => {
    //   Camera.main.orthographicSize = EasingFunctions.EaseOutSine(wantedZoom * 0.85f, wantedZoom, t);
    // }, () => zoomAnimation = null));
  }
}
