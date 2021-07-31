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
    var shouldBeActive = item is IDurable d || item is IStackable s;
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
      var pulse = gameObject.AddComponent<PulseAnimation>();
      if (pulse != null) {
        pulse.pulseScale = 1.1f;
      }
      lastDurability = durability;
    }
  }

  void UpdateTextAndBarFillAmount() {
    if (item is IDurable durable) {
      // durable case
      text.text = $"{durable.durability}/{durable.maxDurability}";
      bar.fillAmount = (float) durable.durability / durable.maxDurability;
      bar.color = Color.Lerp(colorUsed, colorGood, bar.fillAmount);
    } else if (item is IStackable stackable) {
      // stackable case
      text.text = $"{stackable.stacks}";
      bar.fillAmount = 1;
      bar.color = Color.clear;
    }
  }

  void UpdateWidth() {
    var wantedWidth = item is IDurable ? 80 : 40;
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
    if (item is IDurable durable) {
      return durable.durability;
    } else if (item is IStackable stackable) {
      return stackable.stacks;
    } else {
      return -1;
    }
  }
}
