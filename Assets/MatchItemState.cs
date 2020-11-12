using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class MatchItemState : MonoBehaviour {
  public Item item;
  private Button button;
  private Image image;
  private TMPro.TMP_Text stacksText;
  public SpriteAtlas masterSpriteAtlas;

  void Start() {
    /// on click - toggle the popup for this item
    button = GetComponent<Button>();
    image = GetComponentInChildren<Image>(true);
    image.sprite = GetSpriteForItem(item);
    image.rectTransform.sizeDelta = image.sprite.rect.size * 3;
    stacksText = GetComponentInChildren<TMPro.TMP_Text>(true);
    stacksText.gameObject.SetActive(item is IStackable);
  }

  private Sprite GetSpriteForItem(Item item) {
    switch(item) {
      case ItemBerries _:
        return masterSpriteAtlas.GetSprite("berry-red-1");
      case ItemBarkShield _:
        return masterSpriteAtlas.GetSprite("colored_transparent_packed_134");
      case ItemSeed _:
        return masterSpriteAtlas.GetSprite("roguelikeSheet_transparent_532");
      default:
        return masterSpriteAtlas.GetSprite("colored_transparent_packed_1046");
    }
  }

  // Update is called once per frame
  void Update() { }
}
