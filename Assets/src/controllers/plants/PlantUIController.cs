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
  /// Set by the PlantController creating this one
  public PlantController plantController;
  private Plant plant => plantController.plant;

  void Start() {
    AudioClipStore.main.popupOpen.Play(0.2f);
    uiName = transform.Find("Frame/Name").GetComponent<TMP_Text>();
    uiInfo = transform.Find("Frame/Info").GetComponent<TMP_Text>();

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
  }

  public void OnPointerClick(PointerEventData eventData) {
    // Clicking the overlay will trigger this method since the overlay is a child
    if (eventData.pointerEnter.name == "Overlay") {
      plantController.popupOpen = false;
      return;
    }
  }

  void OnDestroy() {
    AudioClipStore.main.popupClose.Play(0.2f);
  }
}
