using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

/// expects this GameObject to have one child for each of this plant's state with matching names.
public class MatchPlantState : MatchActorState, IPointerClickHandler {
  public BerryBush plant => (BerryBush)actor;
  private Dictionary<string, GameObject> plantStageObjects = new Dictionary<string, GameObject>();
  private GameObject activePlantStageObject;
  private GameObject uiChild;
  private bool popupOpen = false;
  private float originalCameraOrthographicSize { get; set; }

  // Start is called before the first frame update
  public override void Start() {
    originalCameraOrthographicSize = Camera.main.orthographicSize;
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
    if (t != null && t == plant.currentTile && t.visiblity == TileVisiblity.Visible) {
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
    // float targetOrthographicSize = popupOpen ? originalCameraOrthographicSize * 0.5f : originalCameraOrthographicSize;
    // Camera.main.orthographicSize = Mathf.Lerp(Camera.main.orthographicSize, targetOrthographicSize, 2f * Time.deltaTime);
    uiChild.SetActive(popupOpen);
  }

  public void Harvest() {
    Debug.Log("Harvesting" + plant);
    plant.Harvest();
  }
}
