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
    UpdatePopup();
  }

  public void OnPointerClick(PointerEventData pointerEventData) {
    // Both the popup and plant stage child components will trigger this since they're children.
    // We *do* want them to capture pointer events (to hide tiles underneath), so raycasting must be
    // enabled for them. Instead we check if the clicked location is in the tile.
    Tile t = Util.GetVisibleTileAt(pointerEventData.position);
    if (t != null && t == plant.currentTile && t.visiblity == TileVisiblity.Visible) {
      popupOpen = !popupOpen;
    }
  }

  void UpdatePopup() {
    uiChild.SetActive(popupOpen);
  }

  public void Harvest() {
    Debug.Log("Harvesting" + plant);
    plant.Harvest();
  }
}
