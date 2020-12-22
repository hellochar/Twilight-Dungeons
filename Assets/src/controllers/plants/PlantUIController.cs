﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlantUIController : MonoBehaviour, IPointerClickHandler {
  private TMP_Text uiName;
  private TMP_Text uiInfo;
  private PlantController plantController;
  private Plant plant => plantController.plant;
  private Transform waterIndicator;
  private Sprite waterCircleEmpty;
  private Sprite waterCircleFilled;

  void Start() {
    plantController = GetComponentInParent<PlantController>();

    uiName = transform.Find("Frame/Name").GetComponent<TMP_Text>();
    uiInfo = transform.Find("Frame/Info").GetComponent<TMP_Text>();
    waterIndicator = transform.Find("Frame/Water Indicator");
    var empty = waterIndicator.Find("Water Circle Empty").gameObject;
    var filled = waterIndicator.Find("Water Circle Filled").gameObject;
    waterCircleEmpty = empty.GetComponent<Image>().sprite;
    waterCircleFilled = filled.GetComponent<Image>().sprite;

    // we already have 2 water circles; add the rest
    for (var i = 2; i < plant.maxWater; i++) {
      Instantiate(empty, waterIndicator, false);
    }

    var waterPail = GameModel.main.player.inventory.First((item) => item is ItemWaterPail) as ItemWaterPail;
    var waterButton = transform.Find("Frame/Buttons/Water").GetComponent<Button>();
    waterButton.interactable = waterPail.stacks > 0;
    waterButton.onClick.AddListener(() => plantController.Water(waterPail));

    transform.Find("Frame/Buttons/Harvest").GetComponent<Button>().onClick.AddListener(plantController.Harvest);
    transform.Find("Frame/Buttons/Cull").GetComponent<Button>().onClick.AddListener(plantController.Cull);

    Update();
  }

  void Update() {
    uiName.text = plant.displayName;
    uiInfo.text = plant.GetUIText();
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
