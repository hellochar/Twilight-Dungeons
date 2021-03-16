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
  public GameObject tutorialExtras;

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

    var mature = plantController.activePlantStageObject.GetComponent<SpriteRenderer>();
    var image = transform.Find("Frame/Image").GetComponent<Image>();
    image.sprite = mature.sprite;
    image.color = mature.color;
    if (plant.percentGrown < 1) {
      var p = image.rectTransform.anchoredPosition;
      p.x = 0;
      image.rectTransform.anchoredPosition = p;
      uiInfo.rectTransform.anchoredPosition = new Vector2();
    }

    if (plant.floor is TutorialFloor && plant.stage.NextStage == null) {
      tutorialExtras.SetActive(true);
    }

    Update();
  }

  private void SetupHarvestOption(Transform transform, Inventory inventory, int index) {
    transform.Find("Inventory").GetComponent<InventoryController>().inventory = inventory;
    Button button = transform.Find("HarvestButton").GetComponent<Button>();
    if (plant.floor is TutorialFloor && index > 0) {
      // disable past index 0
      button.interactable = false;
    } else {
      button.onClick.AddListener(() => {
        plantController.Harvest(index);
      });
    }
  }

  void Update() {
    uiName.text = plant.displayName;
    // uiInfo.text = plant.GetUIText();
    uiInfo.text = plant.percentGrown == 1 ?
      "Choose one Harvest! Tap items to learn about them." :
      $"{plant.percentGrown.ToString("0.0%")} grown. Come back later.";
    if (plant.percentGrown < 1) {
      // uiInfo.GetComponent<RectTransform>().anchoredPosition = new Vector2();
    }
  }

  public void OnPointerClick(PointerEventData eventData) {
    // Clicking the overlay will trigger this method since the overlay is a child
    if (eventData == null || eventData.pointerEnter.name == "Overlay") {
      plantController.popupOpen = false;
      return;
    }
  }

  void OnDestroy() {
    AudioClipStore.main.popupClose.Play(0.2f);
  }
}
