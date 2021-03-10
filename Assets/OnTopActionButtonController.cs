using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OnTopActionButtonController : MonoBehaviour {
  public GameObject button;
  public Image image;
  public TMPro.TMP_Text text;

  // Start is called before the first frame update
  void Start() {
    button.SetActive(false);
  }

  IOnTopActionHandler current;

  // Update is called once per frame
  void Update() {
    var controller = FloorController.current;
    var entities = controller.GetVisibleEntitiesInLayerOrder(GameModel.main.player.pos);
    controller.TryGetFirstControllerComponent<IOnTopActionHandler>(entities, out current, out var e);

    if (current == null) {
      // hide
      HideButton();
    } else {
      ShowButton(e);
    }
  }
  void ShowButton(Entity e) {
    button.SetActive(true);
    image.sprite = ObjectInfo.GetSpriteFor(e);
    text.text = current.OnTopActionName;
  }

  void HideButton() {
    button.SetActive(false);
  }

  public void Pressed() {
    current?.HandleOnTopAction();
  }
}
