using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlantUIController : MonoBehaviour, IPointerClickHandler {
  private TMP_Text uiName;
  private TMP_Text uiInfo;
  public PlantController plantController;
  private Plant plant => plantController.plant;
  private Transform waterIndicator;
  private Sprite waterCircleEmpty;
  private Sprite waterCircleFilled;

  void Start() {
    uiName = transform.Find("Frame/Name").GetComponent<TMP_Text>();
    uiInfo = transform.Find("Frame/Info").GetComponent<TMP_Text>();

    // GetComponentInChildren<InventoryController>().inventory = plant.inventory;

    waterIndicator = transform.Find("Frame/WaterInfo/Water Indicator");
    var empty = waterIndicator.Find("Water Circle Empty").gameObject;
    var filled = waterIndicator.Find("Water Circle Filled").gameObject;
    waterCircleEmpty = empty.GetComponent<Image>().sprite;
    waterCircleFilled = filled.GetComponent<Image>().sprite;

    // we already have 2 water circles; add the rest
    for (var i = 2; i < plant.maxWater; i++) {
      Instantiate(empty, waterIndicator, false);
    }

    var player = GameModel.main.player;
    var waterButton = transform.Find("Frame/WaterInfo/WaterButton").GetComponent<Button>();
    waterButton.interactable = player.water > 0;
    waterButton.onClick.AddListener(() => plantController.Water(player));

    var options = plant.stage.harvestOptions;
    var harvests = transform.Find("Frame/Harvests");
    if (options.Count > 0) {
      for (var i = 0; i < options.Count; i++) {
        SetupHarvestOption(harvests.GetChild(i), options[i], i);
      }
      if (options.Count < harvests.childCount) {
        for (var i = options.Count; i < harvests.childCount; i++) {
          Destroy(harvests.GetChild(i).gameObject);
        }
      }
    } else {
      Destroy(harvests.gameObject);
    }

    var mature = plantController.transform.GetComponentsInChildren<SpriteRenderer>(true).Last();
    var image = transform.Find("Frame/Image").GetComponent<Image>();
    image.sprite = mature.sprite;
    image.color = mature.color;

    Update();
  }

  private void SetupHarvestOption(Transform transform, Inventory inventory, int index) {
    transform.Find("Inventory").GetComponent<InventoryController>().inventory = inventory;
    transform.Find("HarvestButton").GetComponent<Button>().onClick.AddListener(() => {
      plantController.Harvest(index);
    });
  }

  void Update() {
    uiName.text = plant.displayName;
    // uiInfo.text = plant.GetUIText();
    uiInfo.text = plant.percentGrown == 1 ? "Ready to harvest!" : $"{plant.percentGrown.ToString("P")} grown.";
    UpdateWater();
  }

  void UpdateWater() {
    for (var i = 0; i < plant.maxWater; i++) {
      if (i < plant.water) {
        waterIndicator.GetChild(i).GetComponent<Image>().sprite = waterCircleFilled;
      } else {
        waterIndicator.GetChild(i).GetComponent<Image>().sprite = waterCircleEmpty;
      }
    }
  }

  public void OnPointerClick(PointerEventData eventData) {
    // Clicking the overlay will trigger this method since the overlay is a child
    if (eventData.pointerEnter.name == "Overlay") {
      plantController.popupOpen = false;
      return;
    }
  }
}
