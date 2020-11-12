using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupMatchItem : MonoBehaviour {
  public Item item;
  public GameObject spriteBase;

  GameObject itemActionButtonPrefab;

  TMPro.TMP_Text title;
  GameObject actionsContainer;
  GameObject spriteContainer;
  TMPro.TMP_Text stats;
  TMPro.TMP_Text flavorText;
  void Start() {
    itemActionButtonPrefab = Resources.Load<GameObject>("UI/Item Action Button");
    title = transform.Find("Frame/Title").GetComponent<TMPro.TMP_Text>();
    actionsContainer = transform.Find("Frame/Actions").gameObject;
    spriteContainer = transform.Find("Frame/Sprite Container").gameObject;
    stats = transform.Find("Frame/Stats").GetComponent<TMPro.TMP_Text>();
    flavorText = transform.Find("Frame/Flavor Text").GetComponent<TMPro.TMP_Text>();

    title.text = item.displayName;

    Instantiate(spriteBase, spriteContainer.GetComponent<RectTransform>().position, Quaternion.identity, spriteContainer.transform);

    stats.text = item.GetStatsString();

    flavorText.text = GetFlavorTextFor(item);

  }

  private string GetFlavorTextFor(Item item) {
    return ItemInfo.FlavorText[item.GetType()];
  }

  // Update is called once per frame
  void Update() {
    stats.text = item.GetStatsString();
    // if it's been removed
    if (item.inventory == null) {
      Debug.LogWarning("Item Details popup is being run on an item that's been removed from the inventory!");
      Destroy(this);
      return;
    }
  }

  /// call to close the popup
  public void Close() {
    Destroy(gameObject);
  }
}
