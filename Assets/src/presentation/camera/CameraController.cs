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
  public BoundCameraToFloor boundCameraToFloor;
  public CameraFollowEntity cameraFollowEntity;
  public CameraZoom cameraZoom;

  void Start() {
    Update();
  }

  void Update() {
    if (GameModel.main.currentFloor == GameModel.main.home) {
      centerCameraOnFloor.enabled = false;
      boundCameraToFloor.enabled = true;
      cameraFollowEntity.enabled = true;
      cameraZoom.enabled = true;
    } else {
      centerCameraOnFloor.enabled = true;
      boundCameraToFloor.enabled = false;
      cameraFollowEntity.enabled = false;
      cameraZoom.enabled = false;
    }
  }
}