using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SwipeInputController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
  public JoystickController joystick;
  public void OnPointerDown(PointerEventData eventData) {
    joystick.gameObject.SetActive(true);
    joystick.transform.position = Util.withZ(eventData.position);
  }

  public void OnPointerUp(PointerEventData eventData) {
    joystick.gameObject.SetActive(false);
    joystick.Reset();
  }
}
