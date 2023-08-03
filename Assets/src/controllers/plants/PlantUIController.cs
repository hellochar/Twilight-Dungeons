using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlantUIController : MonoBehaviour, IPointerClickHandler, ICameraOverride {
  public TMP_Text uiName;
  public TMP_Text uiInfo;
  public GameObject harvests;
  public Image image;
  /// Set by the PlantController creating this one
  [System.NonSerialized]
  public PlantController plantController;
  private Plant plant => plantController.plant;
  public CameraState overrideState => new CameraState {
    target = plant,
    lean = ViewportLean.Left
  };

  public GameObject tutorialExtras;

  void Start() {
    AudioClipStore.main.popupOpen.Play(0.2f);

    var options = plant.stage.harvestOptions;
    var harvestTransform = harvests.transform;
    if (options.Count > 0) {
      harvests.SetActive(true);
      for (var i = 0; i < options.Count; i++) {
        SetupHarvestOption(harvestTransform.GetChild(i), options[i], i);
      }
      if (options.Count < harvestTransform.childCount) {
        for (var i = options.Count; i < harvestTransform.childCount; i++) {
          Destroy(harvestTransform.GetChild(i).gameObject);
        }
      }
      if (options.Count == 3) {
        var gridLayoutGroup = harvestTransform.GetComponent<GridLayoutGroup>();
        var cellSize = gridLayoutGroup.cellSize;
        var newCellSize = new Vector2(cellSize.x, 80);
        gridLayoutGroup.cellSize = newCellSize;
      }
    }

    var mature = plantController.activePlantStageObject.GetComponent<SpriteRenderer>();
    image.sprite = mature.sprite;
    image.color = mature.color;
    if (plant.percentGrown < 1) {
      var p = image.rectTransform.anchoredPosition;
      p.x = 0;
      image.rectTransform.anchoredPosition = p;
      // uiInfo.rectTransform.anchoredPosition -= new Vector2(0, 50);
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
