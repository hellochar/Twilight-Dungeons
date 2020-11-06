using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class PointerManager : MonoBehaviour {
  void Update() {
    if (Input.GetMouseButtonUp(0)) {
      // set target
      HandleClick(Input.mousePosition);
    }
    // mobile touch
    if (Input.touchCount > 0) {
      Touch t = Input.GetTouch(0);
      if (t.phase == TouchPhase.Ended) {
        HandleClick(t.position);
      }
    }
  }

  void HandleClick(Vector3 screenPoint) {
    var pointerEventData = new PointerEventData(EventSystem.current) { position = screenPoint };
    var raycastResults = new List<RaycastResult>();
    EventSystem.current.RaycastAll(pointerEventData, raycastResults);

    IClickHandler topmostClickHandler = raycastResults.Select(x => x.gameObject.GetComponent<IClickHandler>()).Last();
    if (topmostClickHandler != null) {
      topmostClickHandler.OnClick(pointerEventData);
    }

    // Tile tile = Util.GetVisibleTileAt(screenPoint);
    // actor.action = new MoveToTargetAction(actor, tile.pos);
  }
}
