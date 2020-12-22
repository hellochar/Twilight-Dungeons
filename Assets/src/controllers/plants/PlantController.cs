using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

/// expects this GameObject to have one child for each of this plant's state with matching names.
public class PlantController : ActorController {
  public Plant plant => (Plant) actor;
  private Dictionary<string, GameObject> plantStageObjects = new Dictionary<string, GameObject>();
  private GameObject activePlantStageObject;
  private GameObject ui = null;
  public bool popupOpen {
    get => ui != null;
    set {
      if (value) {
        // opening popup
        ui = PrefabCache.UI.Instantiate("Plant UI", transform);
      } else {
        // closing popup
        Destroy(ui);
        ui = null;
      }
    }
  }

  // Start is called before the first frame update
  public override void Start() {
    foreach (Transform t in transform) {
      plantStageObjects.Add(t.gameObject.name, t.gameObject);
      t.gameObject.SetActive(false);
      t.localPosition = new Vector3(0, 0, t.localPosition.z);
    }
    activePlantStageObject = plantStageObjects[plant.stage.name];
    activePlantStageObject.SetActive(true);
    base.Start();
  }

  public override void Update() {
    base.Update();
    if (activePlantStageObject.name != plant.stage.name) {
      activePlantStageObject.SetActive(false);
      activePlantStageObject = plantStageObjects[plant.stage.name];
      activePlantStageObject.SetActive(true);
    }
  }

  public override void PointerClick(PointerEventData pointerEventData) {
    if (!GameModel.main.player.IsNextTo(plant)) {
      MoveNextToTargetTask task = new MoveNextToTargetTask(GameModel.main.player, plant.pos);
      GameModel.main.player.task = task;
      GameModel.main.turnManager.OnPlayersChoice += HandlePlayersChoice;
      return;
    }
    // Clicking inside the popup will trigger this method; account for that by checking if the clicked location is in the tile.
    Tile t = Util.GetVisibleTileAt(pointerEventData.position);
    if (t != null && t == plant.tile && t.visibility == TileVisiblity.Visible) {
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

  public void Harvest() {
    popupOpen = false;
    plant.Harvest();
  }

  public void Cull() {
    popupOpen = false;
    plant.Cull();
  }

  internal void Water(ItemWaterPail pail) {
    popupOpen = false;
    pail.Water(plant);
  }
}
