using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlantUIController : MonoBehaviour, IPointerClickHandler {
  private Plant plant;
  private TMP_Text uiName;
  private TMP_Text uiInfo;

  void Start() {
    plant = GetComponentInParent<PlantController>().plant;
    uiName = transform.Find("Name").GetComponent<TMP_Text>();
    uiInfo = transform.Find("Info").GetComponent<TMP_Text>();
  }

  void Update() {
    uiName.text = plant.displayName;
    uiInfo.text = plant.stage.getUIText();
  }

  public void OnPointerClick(PointerEventData eventData) {
    // Clicking the overlay will trigger this method since the overlay is a child
    if (eventData.pointerEnter.name == "Overlay") {
      var plantController = GetComponentInParent<PlantController>();
      plantController.popupOpen = false;
      return;
    }
  }

}
