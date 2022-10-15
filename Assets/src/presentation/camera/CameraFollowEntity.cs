using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowEntity : MonoBehaviour {

  public GameObject target;
  public float followSpeed = 1f;
  public float jumpThreshold = 10f;

  // Update is called once per frame
  void Update() {
    // GameObject thisTarget = target;
    // var activeEntity = GameModel.main.turnManager.activeEntity;
    // if (activeEntity is Entity e) {
    //   var gameObject = FloorController.current.GameObjectFor(e);
    //   if (gameObject) {
    //     thisTarget = gameObject;
    //   }
    // }

    transform.position = LerpTowardsPosition(transform.position, target.transform.position, followSpeed, jumpThreshold);
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
