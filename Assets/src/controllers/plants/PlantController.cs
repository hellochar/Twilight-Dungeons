using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

/// expects this GameObject to have one child for each of this plant's state with matching names.
public class PlantController : BodyController, IPopupOverride {
  public Plant plant => (Plant) body;
  public GameObject particles;
  public GameObject seed;
  public GameObject young;
  public GameObject mature;
  [NonSerialized]
  public GameObject activePlantStageObject;
  private PopupController popup = null;
  public bool popupOpen {
    get => popup != null;
    set {
      if (value) {
        // opening popup
        popup = Popups.CreateEmpty(TextAnchor.MiddleRight);
        var plantUI = UnityEngine.Object.Instantiate(PrefabCache.UI.GetPrefabFor("Plant UI"), popup.container);
        var plantUIController = plantUI.GetComponentInChildren<PlantUIController>();
        plantUIController.plantController = this;
        CameraController.main.SetCameraOverride(plantUIController);
      } else if (popup != null) {
        // closing popup
        Destroy(popup.gameObject);
        popup = null;
      }
    }
  }

  // Start is called before the first frame update
  public override void Start() {
    seed.SetActive(false);
    young.SetActive(false);
    mature.SetActive(false);
    seed.transform.localPosition = new Vector3(0, seed.transform.localPosition.y, seed.transform.localPosition.z);
    young.transform.localPosition = new Vector3(0, young.transform.localPosition.y, young.transform.localPosition.z);
    mature.transform.localPosition = new Vector3(0, mature.transform.localPosition.y, mature.transform.localPosition.z);

    activePlantStageObject = plant.stage.name == "Seed" ?
      (plant.percentGrown == 0 ? seed : young) :
      mature;
    activePlantStageObject.SetActive(true);

    if (plant is SingleItemPlant.SingleItemPlant sip) {
      var sr = mature.GetComponent<SpriteRenderer>();
      sr.sprite = ObjectInfo.GetSpriteFor(sip.ItemType);
    }

    particles.SetActive(plant.stage.name == "Seed");
    plant.OnHarvested += HandleHarvested;
    base.Start();
  }

  void OnDestroy() {
    plant.OnHarvested -= HandleHarvested;
  }

  private void HandleHarvested() {
    AudioClipStore.main.plantHarvest.Play(0.1f);
    popupOpen = false;

    var particles = PrefabCache.Effects.Instantiate("Harvest Particles", transform.parent);
    particles.transform.localPosition = transform.localPosition;
    var ps = particles.GetComponent<ParticleSystem>();
    var shape = ps.shape;
    shape.spriteRenderer = activePlantStageObject.GetComponent<SpriteRenderer>();
    shape.texture = shape.spriteRenderer.sprite.texture;
  }

  public void Update() {
    var desiredStageObject = plant.stage.name == "Seed" ?
      (plant.percentGrown == 0 ? seed : young) :
      mature;
    if (desiredStageObject != activePlantStageObject) {
      activePlantStageObject.SetActive(false);
      desiredStageObject.SetActive(true);
      activePlantStageObject = desiredStageObject;
      particles.SetActive(plant.stage.name == "Seed");
    }
  }

  public void HandleShowPopup() {
    GetPlayerInteraction(null).Perform();
  }

  public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    // Clicking inside the popup will trigger this method; account for that by checking if the clicked location is in the tile.
    Tile t = pointerEventData == null ? plant.tile : Util.GetVisibleTileAt(pointerEventData.position);
    if (t != null && t == plant.tile && t.visibility == TileVisiblity.Visible && popupOpen) {
      return new ArbitraryPlayerInteraction(TogglePopup);
    }

    return new SetTasksPlayerInteraction(
      new MoveNextToTargetTask(GameModel.main.player, plant.pos),
      new GenericPlayerTask(GameModel.main.player, TogglePopup)
    );
  }

  public void TogglePopup() {
    popupOpen = !popupOpen;
  }

  public void Harvest(int choiceIndex) {
    try {
      plant.Harvest(choiceIndex);
    } catch (CannotPerformActionException e) {
      popupOpen = false;
      GameModel.main.turnManager.OnPlayerCannotPerform(e);
    }
  }
}
