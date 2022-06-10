using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwipeJoystickController : MonoBehaviour {
  public GameObject stick;
  public float threshold = 40;
  void Update() {
    var offset = Util.getXY(Input.mousePosition) - Util.getXY(transform.position);
    if (offset.magnitude >= threshold) {
      offset = offset.normalized * threshold;
      TryPress();
    }
    stick.transform.position = transform.position + Util.withZ(offset);
  }

  public void Reset() {
    lastTapTime = -1;
  }

  private float lastTapTime = -1;
  void TryPress() {
    if (Time.time - lastTapTime < 0.5f) {
      return;
    }
    lastTapTime = Time.time;
    // var offset = Util.getXY(stick.transform.position) - Util.getXY(transform.position);
    var angle = Mathf.Atan2(stick.transform.localPosition.y, stick.transform.localPosition.x) / Mathf.PI * 180;
    if (angle < 0) {
      angle = 360 + angle;
    }
    // var angle = Vector2.SignedAngle(Vector2.right, Util.getXY(stick.transform.localPosition).normalized);
    Debug.Log(angle);
    switch (angle) {
        // right
      case var x when x > slice(7) || x < slice(0):
        MovePlayer(1, 0); break;
      case var x when x < slice(1):
        MovePlayer(1, 1); break;
      case var x when x < slice(2):
        MovePlayer(0, 1); break;
      case var x when x < slice(3):
        MovePlayer(-1, 1); break;
      case var x when x < slice(4):
        MovePlayer(-1, 0); break;
      case var x when x < slice(5):
        MovePlayer(-1, -1); break;
      case var x when x < slice(6):
        MovePlayer(0, -1); break;
      case var x when x < slice(7):
        MovePlayer(1, -1); break;
      default: break;
    }
  }

  private float slice(int index) {
    return 22.5f + 45f * index;
  }

  public void MovePlayer(int dx, int dy) {
    var interactionController = GameModelController.main.CurrentFloorController.GetComponent<InteractionController>();
    var pos = GameModel.main.player.pos + new Vector2Int(dx, dy);
    /// this potentially does *anything* - set player action, open a popup, or be a no-op.
    interactionController.Interact(pos, null);
  }

}
