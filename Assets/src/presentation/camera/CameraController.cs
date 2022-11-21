using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using Random = System.Random;

// Controls other Camera scripts.
// 
// Use CenterCameraOnFloor in cave levels, but move camera with player at home.
public class CameraController : MonoBehaviour {
  public static CameraController main;

  public float paddingLeft = 0;
  public float paddingRight = 0;
  public float paddingTop = 0;
  public float paddingBottom = 0;

  public float wantedZoom = 5;
  public float minZoom = 3;
  public float maxZoom = 15;
  public float zoomLerpSpeed = 10;

  public float followSpeed = 1f;
  public float jumpThreshold = 10f;

  public new Camera camera { get; private set; }

  private ICameraOverride cameraOverride;

  void Awake() {
    main = this;
    camera = GetComponent<Camera>();
  }

  void Start() {
    wantedZoom = PlayerPrefs.GetFloat("zoom", this.wantedZoom);
    Update();
  }

  void Update() {
    if (cameraOverride is Component c) {
      if (c == null || (c.gameObject == null)) {
        cameraOverride = null;
      }
    }
    if (cameraOverride != null) {
      var state = cameraOverride.overrideState;
      if (state != null) {
        // ZoomLerp(camera, state.targetZoom, zoomLerpSpeed);
        // camera.orthographicSize = state.targetZoom;

        var targetPos = state.targetPos;
        // var targetPos = new Vector2(GameModel.main.player.pos.x, GameModel.main.player.pos.y);
        if (state.lean == ViewportLean.Left) {
          // put the focus on the left half of the camera
          var cameraBounds = OrthographicBounds(camera);
          var cameraWidth = cameraBounds.size.x;
          var leanOffset = new Vector2(cameraWidth / 4, 0);
          targetPos += leanOffset;
        }
        camera.transform.position = LerpTowardsPosition(camera.transform.position, targetPos, followSpeed, jumpThreshold);
        return;
      }
    }
    var floor = GameModel.main.currentFloor;
    // after this the sprites look too small and misclicking is too easy
    var fitsOnOneScreen = floor.width <= 18 && floor.height <= 11;
    if (fitsOnOneScreen) {
      centerCameraOnFloor();
      // boundCameraToFloor();
      // cameraFollowEntity();
      // cameraZoom();
    } else {
#if experimental_survivalhomefloor
      // boundCameraToFloor();
      centerCameraOnActiveRoom();
      cameraFollowEntity();
      cameraZoom();
#elif experimental_chainfloors
      var isHome = floor == GameModel.main.home;
      if (isHome) {
        cameraFollowEntity();
        cameraZoom();
        // boundCameraToFloor();
      } else {
        centerCameraOnActiveRoom();
      }
#else
      cameraFollowEntity();
      cameraZoom();
      boundCameraToFloor();
#endif
    }
  }

  private Bounds bounds = new Bounds(new Vector3(-999, -999), new Vector3(999, 999));
  private void boundCameraToFloor() {
    /// bug - if the current floor is less than 1 bounds, the camera shakes

    var floor = GameModel.main.currentFloor;

    Vector2Int min = floor is HomeFloor ? floor.root.min : floor.boundsMin;
    Vector2Int max = floor is HomeFloor ? floor.root.max : floor.boundsMax;
    bounds.min = Util.withZ(min) + new Vector3(-paddingLeft, -paddingBottom, 0);
    bounds.max = Util.withZ(max) + new Vector3(paddingRight, paddingTop, 0);

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

  public static float lastZoomTime;
  public static bool IsZoomGuardActive => Time.time - lastZoomTime < 0.5f;
  bool acceptUserInput = true;
  private void cameraZoom() {
    if (zoomAnimation == null) {
      ZoomLerp(camera, wantedZoom, zoomLerpSpeed);
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

  private void cameraFollowEntity() {
    var target = GameObject.Find("Player");
    if (target != null) {
      transform.position = LerpTowardsPosition(transform.position, target.transform.position, followSpeed, jumpThreshold);
    }
  }

  private readonly static Vector2 half = new Vector2(0.5f, 0.5f);
  private void centerCameraOnActiveRoom() {
    var activeRoom = GameModel.main.player.room;

    if (activeRoom != null) {
      Bounds bounds = new Bounds();
      bounds.min = Util.withZ(activeRoom.min - half) + new Vector3(-paddingLeft, -paddingBottom, 0);
      bounds.max = Util.withZ(activeRoom.max + half) + new Vector3(paddingRight, paddingTop, 0);

      // this.transform.position = new Vector3(bounds.center.x, bounds.center.y - 0.5f, this.transform.position.z);
      transform.position = LerpTowardsPosition(
        transform.position,
        new Vector3(bounds.center.x, bounds.center.y - 0.5f, this.transform.position.z),
        followSpeed,
        jumpThreshold
      );
      var zoom = Mathf.Max(minZoom, bounds.extents.y);
      ZoomLerp(camera, zoom, zoomLerpSpeed);
    }
  }

  private void centerCameraOnFloor() {
    Bounds bounds = new Bounds();
    bounds.min = Util.withZ(GameModel.main.currentFloor.boundsMin) + new Vector3(-paddingLeft, -paddingBottom, 0);
    bounds.max = Util.withZ(GameModel.main.currentFloor.boundsMax) + new Vector3(paddingRight, paddingTop, 0);

    this.transform.position = new Vector3(bounds.center.x, bounds.center.y - 0.5f, this.transform.position.z);
    camera.orthographicSize = Mathf.Max(minZoom, bounds.extents.y);
  }

  public void SetCameraOverride(ICameraOverride overrider) {
    this.cameraOverride = overrider;
  }

  public static void ZoomLerp(Camera camera, float wantedZoom, float lerpSpeed) {
    if (Mathf.Abs(camera.orthographicSize - wantedZoom) < 0.01) {
      camera.orthographicSize = wantedZoom;
    } else {
      camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, wantedZoom, lerpSpeed * Time.deltaTime);
    }
  }

  public static Vector3 LerpTowardsPosition(Vector3 position, Vector3 target, float followSpeed, float jumpThreshold) {
    // jump immediately if too far away
    if (Vector2.Distance(Util.getXY(position), Util.getXY(target)) > jumpThreshold) {
      return Util.withZ(Util.getXY(target), position.z);
    } else {
      // lerp towards target
      return Util.withZ(
        Vector2.Lerp(
          Util.getXY(position),
          Util.getXY(target),
          // followSpeed / 60f
          followSpeed * Time.deltaTime
        ),
      position.z);
    }
  }
}

public enum ViewportLean { None, Left, Right }

public class CameraState {
  public Entity target;
  public ViewportLean lean = ViewportLean.None;
  public float targetZoom => Mathf.Max((extents.height + 1) / 2.0f, 2);
  public Vector2 targetPos => extents.centerFloat + target.pos;

  private Room m_extents;
  private Room extents {
    get {
      if (m_extents == null) {
        m_extents = new Room(Vector2Int.zero, Vector2Int.zero);
        foreach(var offset in target.shape) {
          m_extents.ExtendToEncompass(new Room(offset, offset));
        }
      }
      return m_extents;
    }
  }
}
