using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// some things to do:
// show current raycasted object in a textbox
public class DebugGame : MonoBehaviour {
  GameObject textBox;

  void Start() {
    textBox = new GameObject("DebugGame text box");
    textBox.transform.parent = GameObject.Find("Canvas").transform;
  }

  // Update is called once per frame
  void Update() {
    if (Input.GetMouseButtonDown(0)) {
      OnMousePressed();
    }
  }

  void OnMousePressed() {
    var eventSystem = EventSystem.current;
    var inputModule = eventSystem.currentInputModule;
    PointerEventData ped = new PointerEventData(eventSystem);
    var mousePosition = eventSystem.currentInputModule.input.mousePosition;
    Debug.Log(Input.mousePosition + " vs " + mousePosition);
    ped.position = mousePosition;
    var results = new List<RaycastResult>();
    eventSystem.RaycastAll(ped, results);

    var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    var intersections = Physics2D.GetRayIntersectionAll(ray);

    // Debug.DrawRay(ray.origin, ray.direction, Color.yellow, 1, true);
    Debug.Log(string.Join(", ", results) + " ------- " + string.Join(", ", intersections));
  }
}
