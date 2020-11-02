﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartsUpdate : MonoBehaviour {
  public Sprite[] sprites;
  public GameObject heartPrefab;

  // Start is called before the first frame update
  void Start() {
    this.heartPrefab = Resources.Load<GameObject>("UI/Heart");
  }

  // Update is called once per frame
  void Update() {
    int hp = GameModel.main.player.hp;
    int hpMax = GameModel.main.player.hpMax;

    int numHearts = hpMax / 4;
    MatchNumberOfHearts(numHearts);

    for (int i = 0; i < numHearts; i++) {
      GameObject heart = transform.GetChild(i).gameObject;
      int hpForThisHeart = Mathf.Clamp(hp - i * 4, 0, 4);
      Sprite wantedSprite = this.sprites[hpForThisHeart];
      heart.GetComponent<Image>().sprite = wantedSprite;
    }
  }

  void MatchNumberOfHearts(int numHearts) {
    int padding = 5;
    for (int i = transform.childCount; i < numHearts; i++) {
      GameObject heart = Instantiate(heartPrefab, this.transform.position, Quaternion.identity, this.transform);
      RectTransform rt = heart.GetComponent<RectTransform>();
      float width = rt.rect.width;
      rt.anchoredPosition = new Vector2((width + padding) * i, 0);
    }
    // TODO delete extra hearts
  }
}
