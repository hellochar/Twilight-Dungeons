using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DPadController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler {
  bool isPressed = false;
  private bool queueButtonPress = false;
  GameObject activeButton;
  Sprite buttonOriginalSprite;
  private PointerEventData lastEventData;
  private float lastTapTime = -1;

  /// hard-coded
  private Transform stopButton => transform.GetChild(4);
  private float stopButtonRadius => stopButton.GetComponent<RectTransform>().sizeDelta.x * 0.60f;

  void Start() {
    GameModel.main.turnManager.OnPlayersChoice += HandlePlayersChoice;
    Settings.OnChanged += MatchSettings;
    MatchSettings();
  }

  void OnDestroy() {
    GameModel.main.turnManager.OnPlayersChoice -= HandlePlayersChoice;
    Settings.OnChanged -= MatchSettings;
  }

  private void MatchSettings() {
    gameObject?.SetActive(Settings.main.useDPad);
  }

  void HandlePlayersChoice() {
    // we must queue the button press because at this point in the callback, the GameModel's other handler
    // might not have run yet, which we rely on to set the player's action
    if (isPressed) {
      queueButtonPress = true;
    }
  }

  public void MovePlayer(int dx, int dy) {
    var interactionController = GameModelController.main.CurrentFloorController.GetComponent<InteractionController>();
    var pos = GameModel.main.player.pos + new Vector2Int(dx, dy);
    /// this potentially does *anything* - set player action, open a popup, or be a no-op.
    interactionController.Interact(pos, null);
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

  public void OnPointerDown(PointerEventData eventData) {
    lastEventData = eventData;
    lastTapTime = Time.time;
    isPressed = true;
    UpdateActiveButton();
    /// do the initial press
    activeButton.GetComponent<Button>().OnPointerClick(eventData);
  }

  public void OnPointerUp(PointerEventData eventData) => Unpress();

  /// if touch leaves the dpad area, cancel the touch.
  public void OnPointerExit(PointerEventData eventData) => Unpress();

  void Unpress() {
    isPressed = false;
    UnsetButtonSprite();
  }

  void Update() {
    if (isPressed) {
      UpdateActiveButton();
      if (ShouldCancelPress()) {
        Unpress();
      } else {
        /// only do queued button presses after .5 seconds of holding
        /// this prevents an undesirable "double move" if you get free moves
        if (queueButtonPress && (Time.time - lastTapTime > 0.5f)) {
          activeButton.GetComponent<Button>().OnPointerClick(lastEventData);
          queueButtonPress = false;
        }
      }
    }
  }

  /// cancel the press if there's a popup or plant ui
  /// 
  /// for press and hold, the question is *when* to re-trigger a press?
  /// if we're moving on normal ground, we should retrigger at a regular interval
  /// (when it's the player's choice).
  /// if we hit a popup of some sort, then stop the press.

  private bool ShouldCancelPress() {
    return GameObject.FindGameObjectWithTag("Popup") != null;
  }

  void UpdateActiveButton() {
    GameObject newButton = GetPressedButtonFromTouchPosition(Input.mousePosition);
    if (activeButton != newButton) {
      UnsetButtonSprite();

      activeButton = newButton;
      buttonOriginalSprite = activeButton.GetComponent<Image>().sprite;
      activeButton.GetComponent<Image>().sprite = activeButton.GetComponent<Button>().spriteState.pressedSprite;
    }
  }

  void UnsetButtonSprite() {
    if (activeButton != null) {
      activeButton.GetComponent<Image>().sprite = buttonOriginalSprite;
      activeButton = null;
      buttonOriginalSprite = null;
    }
  }

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
