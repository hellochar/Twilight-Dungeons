using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatusIconController : MonoBehaviour {
  [NonSerialized]
  public Status status;
  Image icon;
  public GameObject redOutline;
  TMPro.TMP_Text text;

  void Start() {
    redOutline.SetActive(status.isDebuff);
    icon = transform.Find("Icon").GetComponent<Image>();
    icon.sprite = ObjectInfo.GetSpriteFor(status);
    text = transform.Find("Text (TMP)").GetComponent<TMPro.TMP_Text>();
    text.gameObject.SetActive(status is StackingStatus);
    Update();
    /// in cases where statuses get added and removed within the same render loop,
    /// this status might be removed already. Handle that
    if (status.list == null) {
      HandleRemoved();
    } else {
      status.OnRemoved += HandleRemoved;
    }
  }

  void Update() {
    if (status is StackingStatus stacking) {
      text.text = stacking.stacks.ToString();
    }
  }

  private void HandleRemoved() {
    this.gameObject.AddComponent<FadeThenDestroy>();
  }

  public void OpenPopup() {
    Popups.Create(
      title: status.displayName,
      category: status.isDebuff ? "Debuff" : "Status",
      info: status.Info(),
      flavor: ObjectInfo.GetFlavorTextFor(status),
      sprite: transform.Find("Icon").gameObject
    );
  }
}
