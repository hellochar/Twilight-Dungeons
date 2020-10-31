using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// expects this GameObject to have one child for each of this plant's state with matching names.
public class MatchPlantState : MonoBehaviour {
  public BerryBush plant;
  private Dictionary<string, GameObject> plantStageObjects = new Dictionary<string, GameObject>();
  private GameObject activePlantStageObject;

  // Start is called before the first frame update
  void Start() {
    foreach (Transform t in GetComponentInChildren<Transform>(true)) {
      plantStageObjects.Add(t.gameObject.name, t.gameObject);
    }
    activePlantStageObject = plantStageObjects[plant.currentStage.name];
    activePlantStageObject.SetActive(true);
  }

  // Update is called once per frame
  void Update() {
    if (activePlantStageObject.name != plant.currentStage.name) {
      activePlantStageObject.SetActive(false);
      activePlantStageObject = plantStageObjects[plant.currentStage.name];
      activePlantStageObject.SetActive(true);
    }
  }
}
