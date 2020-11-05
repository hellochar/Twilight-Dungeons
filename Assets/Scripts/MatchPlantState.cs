using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// expects this GameObject to have one child for each of this plant's state with matching names.
public class MatchPlantState : MatchActorState, IPointerClickHandler {
  public BerryBush plant => (BerryBush)actor;
  private Dictionary<string, GameObject> plantStageObjects = new Dictionary<string, GameObject>();
  private GameObject activePlantStageObject;
  private GameObject uiChild;
  private bool popupOpen = false;

  // Start is called before the first frame update
  public override void Start() {
    base.Start();
    uiChild = transform.Find("Canvas").gameObject;
    uiChild.SetActive(false);
    foreach (Transform t in GetComponentInChildren<Transform>(true)) {
      plantStageObjects.Add(t.gameObject.name, t.gameObject);
    }
    activePlantStageObject = plantStageObjects[plant.currentStage.name];
    activePlantStageObject.SetActive(true);
  }

  public override void Update() {
    base.Update();
    if (activePlantStageObject.name != plant.currentStage.name) {
      activePlantStageObject.SetActive(false);
      activePlantStageObject = plantStageObjects[plant.currentStage.name];
      activePlantStageObject.SetActive(true);
    }
    if (Input.GetMouseButtonDown(0)) {
      Tile t = Util.GetVisibleTileAt(Input.mousePosition);
      if (t.pos == plant.pos && t.visiblity == TileVisiblity.Visible) {
        popupOpen = !popupOpen;
      }
    }
    UpdatePopup();
  }

  public void OnPointerClick(PointerEventData pointerEventData) {
    Debug.Log("point click happened");
  }

  void UpdatePopup() {
    uiChild.SetActive(popupOpen);
  }

  public void Harvest() {
    Debug.Log("Harvesting" + plant);
    plant.Harvest();
  }
}
