using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TapToPulse : MonoBehaviour, IPointerClickHandler {
  public void OnPointerClick(PointerEventData eventData) {
      var pulse = gameObject.AddComponent<PulseAnimation>();
      pulse.pulseScale = 0.9f;
  }
}
