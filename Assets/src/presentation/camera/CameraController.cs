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
	public CenterCameraOnFloor centerCameraOnFloor;
  public CenterCameraOnActiveRoom centerCameraOnActiveRoom;
  public BoundCameraToFloor boundCameraToFloor;
  public CameraFollowEntity cameraFollowEntity;
  public CameraZoom cameraZoom;

  void Start() {
    Update();
  }

  void Update() {
    var floor = GameModel.main.currentFloor;
    // after this the sprites look too small and misclicking is too easy
    var fitsOnOneScreen = floor.width <= 18 && floor.height <= 11;
    if (fitsOnOneScreen) {
      centerCameraOnFloor.enabled = true;
      boundCameraToFloor.enabled = false;
      cameraFollowEntity.enabled = false;
      cameraZoom.enabled = false;
    } else {
      centerCameraOnFloor.enabled = false;
#if experimental_chainfloors
      var isHome = floor == GameModel.main.home;
      boundCameraToFloor.enabled = isHome;
      centerCameraOnActiveRoom.enabled = !isHome;
#else
      boundCameraToFloor.enabled = true;
      centerCameraOnActiveRoom.enabled = false;
#endif
      cameraFollowEntity.enabled = true;
      cameraZoom.enabled = true;
    }
  }
}