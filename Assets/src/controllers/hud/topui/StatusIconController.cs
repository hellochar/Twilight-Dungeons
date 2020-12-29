using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatusIconController : MonoBehaviour {
  public Status status;
  Image icon;
  TMPro.TMP_Text text;

  void Start() {
    status.OnRemoved += HandleRemoved;
    icon = transform.Find("Icon").GetComponent<Image>();
    icon.sprite = ObjectInfo.GetSpriteFor(status);
    text = transform.Find("Text (TMP)").GetComponent<TMPro.TMP_Text>();
    text.gameObject.SetActive(status is StackingStatus);
    Update();
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
      info: status.Info(),
      flavor: ObjectInfo.GetFlavorTextFor(status),
      sprite: transform.Find("Icon").gameObject
    );
  }
}
