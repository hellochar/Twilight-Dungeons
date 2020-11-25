using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

/// expects this GameObject to have one child for each of this plant's state with matching names.
public class MatchPlantState : MatchActorState, IPointerClickHandler {
  public Plant plant => (Plant) actor;
  private Dictionary<string, GameObject> plantStageObjects = new Dictionary<string, GameObject>();
  private GameObject activePlantStageObject;
  private GameObject uiChild;
  private bool popupOpen = false;

  // Start is called before the first frame update
  public override void Start() {
    base.Start();
    uiChild = transform.Find("Canvas").gameObject;
    uiChild.SetActive(false);
    foreach (Transform t in transform) {
      // don't include the built-in canvas
      if (t.GetComponent<Canvas>() == null) {
        plantStageObjects.Add(t.gameObject.name, t.gameObject);
        t.gameObject.SetActive(false);
        t.localPosition = new Vector3(0, 0, t.localPosition.z);
      }
    }
    activePlantStageObject = plantStageObjects[plant.stage.name];
    activePlantStageObject.SetActive(true);
  }

  public override void Update() {
    base.Update();
    if (activePlantStageObject.name != plant.stage.name) {
      activePlantStageObject.SetActive(false);
      activePlantStageObject = plantStageObjects[plant.stage.name];
      activePlantStageObject.SetActive(true);
    }
    UpdatePopup();
  }

  public override void OnPointerClick(PointerEventData pointerEventData) {
    // Clicking the overlay will trigger this method since the overlay is a child
    if (pointerEventData.pointerEnter.name == "Overlay") {
      popupOpen = false;
      return;
    }

    if (!GameModel.main.player.IsNextTo(plant)) {
      MoveNextToTargetAction action = new MoveNextToTargetAction(GameModel.main.player, plant.pos);
      GameModel.main.player.action = action;
      GameModel.main.turnManager.OnPlayersChoice += HandlePlayersChoice;
      return;
    }
    // Clicking inside the popup will trigger this method; account for that by checking if the clicked location is in the tile.
    Tile t = Util.GetVisibleTileAt(pointerEventData.position);
    if (t != null && t == plant.currentTile && t.visibility == TileVisiblity.Visible) {
      TogglePopup();
    }
  }

  public async void HandlePlayersChoice() {
      GameModel.main.turnManager.OnPlayersChoice -= HandlePlayersChoice;
      await Task.Delay(100);
      TogglePopup();
  }

  public void TogglePopup() {
    popupOpen = !popupOpen;
  }

  void UpdatePopup() {
    uiChild.SetActive(popupOpen);
  }

  public void Harvest() {
    plant.Harvest();
  }

  public void Cull() {
    plant.Cull();
  }
}
