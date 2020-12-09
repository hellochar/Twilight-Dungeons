using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatchStatusTopUIState : MonoBehaviour {
  public Status status;
  Image icon;

  void Start() {
    status.OnRemoved += HandleRemoved;
    icon = transform.Find("Icon").GetComponent<Image>();
    icon.sprite = ObjectInfo.GetSpriteFor(status);
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
