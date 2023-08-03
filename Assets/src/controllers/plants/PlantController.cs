using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

/// expects this GameObject to have one child for each of this plant's state with matching names.
public class PlantController : BodyController, ILongTapHandler {
  public Plant plant => (Plant) body;
  public GameObject particles;
  public GameObject seed;
  public GameObject mature;
  [NonSerialized]
  public GameObject activePlantStageObject;
  private GameObject ui = null;
  public bool popupOpen {
    get => ui != null;
    set {
      if (value) {
        // opening popup
        var parent = GameObject.Find("Canvas").transform;
        ui = UnityEngine.Object.Instantiate(PrefabCache.UI.GetPrefabFor("Plant UI"), parent.position, Quaternion.identity, parent);
        ui.GetComponentInChildren<PlantUIController>().plantController = this;
      } else {
        // closing popup
        Destroy(ui);
        ui = null;
      }
    }
  }

  // Start is called before the first frame update
  public override void Start() {
    seed.SetActive(false);
    mature.SetActive(false);
    seed.transform.localPosition = new Vector3(0, seed.transform.localPosition.y, seed.transform.localPosition.z);
    mature.transform.localPosition = new Vector3(0, mature.transform.localPosition.y, mature.transform.localPosition.z);

    activePlantStageObject = plant.stage.name == "Seed" ? seed : mature;
    activePlantStageObject.SetActive(true);

    particles.SetActive(plant.stage.name == "Seed");
    plant.OnHarvested += HandleHarvested;
    base.Start();
  }

  void OnDestroy() {
    plant.OnHarvested -= HandleHarvested;
  }

  private void HandleHarvested() {
    var particles = PrefabCache.Effects.Instantiate("Harvest Particles", transform.parent);
    particles.transform.localPosition = transform.localPosition;
    var ps = particles.GetComponent<ParticleSystem>();
    var shape = ps.shape;
    shape.spriteRenderer = activePlantStageObject.GetComponent<SpriteRenderer>();
    shape.texture = shape.spriteRenderer.sprite.texture;
  }

  public void Update() {
    if (activePlantStageObject.name != plant.stage.name) {
      activePlantStageObject.SetActive(false);
      activePlantStageObject = plant.stage.name == "Seed" ? seed : mature;
      activePlantStageObject.SetActive(true);
      particles.SetActive(plant.stage.name == "Seed");
    }
  }

  public void HandleLongTap() {
    GetPlayerInteraction(null);
  }

  public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    if (!GameModel.main.player.IsNextTo(plant)) {
      return new SetTasksPlayerInteraction(
        new MoveNextToTargetTask(GameModel.main.player, plant.pos),
        new GenericPlayerTask(GameModel.main.player, TogglePopup)
      );
    }
    // Clicking inside the popup will trigger this method; account for that by checking if the clicked location is in the tile.
    Tile t = pointerEventData == null ? plant.tile : Util.GetVisibleTileAt(pointerEventData.position);
    if (t != null && t == plant.tile && t.visibility == TileVisiblity.Visible) {
      return new ArbitraryPlayerInteraction(TogglePopup);
    }
    return null;
  }

  public void TogglePopup() {
    popupOpen = !popupOpen;
  }

  public void Harvest(int choiceIndex) {
    AudioClipStore.main.plantHarvest.Play(0.1f);
    popupOpen = false;
    plant.Harvest(choiceIndex);
  }

  public GameObject GetUI() {
    return ui;
  }
}
