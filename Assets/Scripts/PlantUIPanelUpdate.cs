using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlantUIPanelUpdate : MonoBehaviour {
  private Plant plant;
  private TMP_Text uiName;
  private TMP_Text uiInfo;
  void Start() {
    plant = GetComponentInParent<MatchPlantState>().plant;
    uiName = transform.Find("Name").GetComponent<TMP_Text>();
    uiInfo = transform.Find("Info").GetComponent<TMP_Text>();
  }

  void Update() {
    uiName.text = plant.displayName;
    uiInfo.text = plant.stage.getUIText();
  }
}
