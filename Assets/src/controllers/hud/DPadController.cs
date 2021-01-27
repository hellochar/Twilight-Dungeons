using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DPadController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler {

  void Start() {
    // this will work even while the gameobject is inactive.
    Settings.OnChanged += MatchSettings;
  }

  void MatchSettings() {
    gameObject.SetActive(Settings.main.moveMode.HasFlag(MoveMode.DPad));
  }

  public void MovePlayer(int dx, int dy) {
    var floorController = GameModelController.main.CurrentFloorController;
    var pos = GameModel.main.player.pos + new Vector2Int(dx, dy);
    floorController.UserInteractAt(pos, null);
  }

  public void UpLeft() => MovePlayer(-1, 1);
  public void Up() => MovePlayer(0, 1);
  public void UpRight() => MovePlayer(1, 1);

  public void Left() => MovePlayer(-1, 0);
  public void Stop() => MovePlayer(0, 0);
  public void Right() => MovePlayer(1, 0);

  public void DownLeft() => MovePlayer(-1, -1);
  public void Down() => MovePlayer(0, -1);
  public void DownRight() => MovePlayer(1, -1);

  bool isPressed = false;
  public void OnPointerDown(PointerEventData eventData) {
    isPressed = true;
    UpdatePressedButton();
  }

  public void OnPointerUp(PointerEventData eventData) {
    // do the press
    button.GetComponent<Button>().OnPointerClick(eventData);
    isPressed = false;
    UnsetButtonSprite();
  }

  /// if touch leaves the dpad area, cancel the touch.
  public void OnPointerExit(PointerEventData eventData) {
    isPressed = false;
    UnsetButtonSprite();
  }

  void Update() {
    if (isPressed) {
      UpdatePressedButton();
    }
  }

  GameObject button;
  Sprite buttonOriginalSprite;
  void UpdatePressedButton() {
    GameObject newButton = GetPressedButtonFromTouchPosition(Input.mousePosition);
    if (button != newButton) {
      UnsetButtonSprite();

      button = newButton;
      buttonOriginalSprite = button.GetComponent<Image>().sprite;
      button.GetComponent<Image>().sprite = button.GetComponent<Button>().spriteState.pressedSprite;
    }
  }

  void UnsetButtonSprite() {
    if (button != null) {
      button.GetComponent<Image>().sprite = buttonOriginalSprite;
    }
  }

  /// hard-coded
  private Transform stopButton => transform.GetChild(4);

  private float stopButtonRadius => stopButton.GetComponent<RectTransform>().sizeDelta.x / 2;

  private GameObject GetPressedButtonFromTouchPosition(Vector3 touchPosition3) {
    Vector2 touchPos = Util.getXY(touchPosition3);
    Vector2 dPadCenter = Util.getXY(transform.position);
    Vector2 offset = touchPos - dPadCenter;
    float magnitude = offset.magnitude;
    if (magnitude <= stopButtonRadius) {
      // we're clicking the center
      return stopButton.gameObject;
    } else {
      var directionButtons = new List<Transform>();
      foreach (Transform child in transform) {
        if (child != stopButton) {
          directionButtons.Add(child);
        }
      }

      // find the button with the closest angle
      var minAngleButton = directionButtons.OrderBy((child) => {
        var childOffset = Util.getXY(child.position) - dPadCenter;
        return Vector2.Angle(offset, childOffset);
      }).First();
      return minAngleButton.gameObject;
    }
  }
}
