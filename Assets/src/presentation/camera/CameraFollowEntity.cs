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

    // jump immediately if too far away
    if (Vector2.Distance(Util.getXY(this.transform.position), Util.getXY(this.target.transform.position)) > jumpThreshold) {
      this.transform.position = Util.withZ(Util.getXY(this.target.transform.position), this.transform.position.z);
    } else {
      // lerp towards target
      this.transform.position = Util.withZ(
        Vector2.Lerp(
          Util.getXY(this.transform.position),
          Util.getXY(this.target.transform.position),
          // followSpeed / 60f
          this.followSpeed * Time.deltaTime
        ),
      this.transform.position.z);
    }
  }
}
