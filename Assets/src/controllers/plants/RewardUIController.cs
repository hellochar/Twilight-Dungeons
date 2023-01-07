using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RewardUIController : MonoBehaviour, IPointerClickHandler {
  public Rewards rewards;
  public GameObject rewardContainer;

  void Start() {
    AudioClipStore.main.popupOpen.Play(0.2f);

    for (var i = 0; i < rewards.inventories.Count; i++) {
      SetupRewardOption(rewardContainer.transform.GetChild(i), rewards.inventories[i], i);
    }
    if (rewards.inventories.Count < rewardContainer.transform.childCount) {
      for (var i = rewards.inventories.Count; i < rewardContainer.transform.childCount; i++) {
        Destroy(rewardContainer.transform.GetChild(i).gameObject);
      }
    }
  }

  private void SetupRewardOption(Transform transform, Inventory inventory, int index) {
    transform.Find("Inventory").GetComponent<InventoryController>().inventory = inventory;
    Button button = transform.Find("ChooseButton").GetComponent<Button>();
    button.onClick.AddListener(() => {
      rewards.Choose(inventory);
      Destroy(gameObject, 0);
    });
  }

  void OnDestroy() {
    AudioClipStore.main.popupClose.Play(0.2f);
  }

  public void OnPointerClick(PointerEventData eventData) {
  }
}
