using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ReticleInputController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
  public ReticleJoystickController reticleJoystick;
  void Start() {
    // Settings.OnChanged += MatchSettings;
    // MatchSettings();
  }

  void OnDestroy() {
    // Settings.OnChanged -= MatchSettings;
  }

  private void MatchSettings() {
    // gameObject?.SetActive(Settings.main.swipeToMove);
  }

  public void OnPointerDown(PointerEventData eventData) {
    reticleJoystick.BeginPress(eventData.position);
  }

  public void OnPointerUp(PointerEventData eventData) {
    reticleJoystick.LetGo();
  }
}
