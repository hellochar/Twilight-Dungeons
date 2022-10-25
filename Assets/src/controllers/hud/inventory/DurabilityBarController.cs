using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DurabilityBarController : MonoBehaviour {
  public EquipmentSlot slot;
  public Equipment equipment => GameModel.main.player.equipment;
  public Item item => equipment[slot];
  public Image bar;
  public TMPro.TMP_Text text;
  public Color colorGood, colorUsed;
  private RectTransform rectTransform;
  private int lastDurability;

  public void Start() {
    rectTransform = GetComponent<RectTransform>();
    equipment.OnItemAdded += HandleItemAdded;
    equipment.OnItemRemoved += HandleItemRemoved;
    UpdateActive();
  }

  public void OnDestroy() {
    equipment.OnItemAdded -= HandleItemAdded;
    equipment.OnItemRemoved -= HandleItemRemoved;
  }

  private void HandleItemRemoved(Item obj) {
    UpdateActive();
  }

  private void HandleItemAdded(Item item, Entity arg2) {
    UpdateActive();
  }

  public void UpdateActive() {
    // var isActive = gameObject.activeSelf;
    var shouldBeActive = item != null && item.stacksMax > 1;
    gameObject.SetActive(shouldBeActive);
    if (shouldBeActive) {
      lastDurability = GetItemDurability();
      Update();
    }
  }

  public void Update() {
    UpdatePulse();
    UpdateTextAndBarFillAmount();
    UpdateWidth();
    UpdateFillOrigin();
  }

  void UpdatePulse() {
    // do a pulse
    var durability = GetItemDurability();
    if (lastDurability != durability) {
      gameObject.AddComponent<PulseAnimation>()?.Scale(1.1f);
      lastDurability = durability;
    }
  }

  void UpdateTextAndBarFillAmount() {
    if (item.disjoint) {
      // durable case
      text.text = $"{item.stacks}/{item.stacksMax}";
      bar.fillAmount = (float) item.stacks / item.stacksMax;
      bar.color = Color.Lerp(colorUsed, colorGood, bar.fillAmount);
    } else {
      // stackable case
      text.text = $"{item.stacks}";
      bar.fillAmount = 1;
      bar.color = Color.clear;
    }
  }

  void UpdateWidth() {
    var wantedWidth = item.disjoint ? 80 : 40;
    if (rectTransform.sizeDelta.x != wantedWidth) {
      var sizeDelta = rectTransform.sizeDelta;
      sizeDelta.x = wantedWidth;
      rectTransform.sizeDelta = sizeDelta;
    }
  }

  void UpdateFillOrigin() {
    // right-hand mode
    var wantedFillMode = Settings.main.rightHanded ? (int) Image.OriginHorizontal.Left : (int) Image.OriginHorizontal.Right;
    bar.fillOrigin = wantedFillMode;
  }

  private int GetItemDurability() {
    return item.stacks;
  }
}
