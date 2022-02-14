using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ReticleInputController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
  public ReticleJoystickController reticleJoystick;
  public void OnPointerDown(PointerEventData eventData) {
    reticleJoystick.BeginPress(eventData.position);
  }

  public void OnPointerUp(PointerEventData eventData) {
    reticleJoystick.LetGo();
  }
}
